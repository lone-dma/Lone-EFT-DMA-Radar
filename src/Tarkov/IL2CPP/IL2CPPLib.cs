using VmmSharpEx;
using VmmSharpEx.Extensions;

namespace LoneEftDmaRadar.Tarkov.IL2CPP
{
    internal static class IL2CPPLib
    {
        #region Public API

        /// <summary>
        /// True if IL2CPPLib was successfully initialized, otherwise False.
        /// </summary>
        public static bool Initialized { get; private set; }

        /// <summary>
        /// Lookup GameWorld using IL2CPP Interop.
        /// </summary>
        /// <param name="gameWorld"></param>
        /// <param name="map"></param>
        /// <returns>True if success, otherwise False.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static bool TryGetGameWorld(out ulong gameWorld, out string map)
        {
            gameWorld = default;
            map = default;
            try
            {
                if (!Initialized)
                    return false;
                var gamePlayerOwner = Memory.ReadValue<Class>(class_gamePlayerOwner);
                var myPlayer = Memory.ReadPtr(gamePlayerOwner.static_fields + Offsets.GamePlayerOwner._myPlayer);
                gameWorld = Memory.ReadPtr(myPlayer + Offsets.Player.GameWorld);
                /// Get Selected Map
                var mapPtr = Memory.ReadValue<ulong>(gameWorld + Offsets.GameWorld.LocationId);
                if (mapPtr == 0x0) // Offline Mode
                {
                    var localPlayer = Memory.ReadPtr(gameWorld + Offsets.GameWorld.MainPlayer);
                    mapPtr = Memory.ReadPtr(localPlayer + Offsets.Player.Location);
                }

                map = Memory.ReadUnityString(mapPtr, 128);
                Logging.WriteLine("Detected Map " + map);
                if (!TarkovDataManager.MapData.ContainsKey(map)) // Also makes sure we're not in the hideout
                    throw new ArgumentException("Invalid Map ID!");
                return true;
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"Failed to get GameWorld via IL2CPP: {ex}");
                return false;
            }
        }

        #endregion

        private static Vmm _vmm = null!;
        private static uint _pid;
        private static int gTypeCount;
        private static ulong gAssembliesStart;
        private static ulong gAssembliesEnd;
        private static ulong gTypeInfoDefinitionTable;
        private static ulong gMetadataGlobalHeader;
        private static ulong gGlobalMetadata;
        private static ulong class_gamePlayerOwner;

        static IL2CPPLib()
        {
            Memory.ProcessStopped += Memory_ProcessStopped;
        }

        private static void Memory_ProcessStopped(object sender, EventArgs e) => Reset();
        private static void Reset()
        {
            _vmm = default;
            _pid = default;
            class_gamePlayerOwner = default;
            Initialized = default;
        }

        public static void Init(Vmm vmm, uint pid)
        {
            try
            {
                Logging.WriteLine("Initializing IL2CPP SDK...");
                _vmm = vmm;
                _pid = pid;
                if (!vmm.Map_GetModuleFromName(pid, "GameAssembly.dll", out var module))
                    throw new InvalidOperationException("Could not find GameAssembly.dll module in target process.");
                if (vmm.Map_GetEAT(pid, "GameAssembly.dll", out _) is not Vmm.EATEntry[] eat || eat.Length == 0)
                    throw new InvalidOperationException("Could not get GameAssembly.dll export table.");

                Resolve_TypeInfoDefinitionTable(ref module, eat);
                class_gamePlayerOwner = Class.FindClass("EFT.GamePlayerOwner");

                Initialized = true;
                Logging.WriteLine("IL2CPP SDK Initialized.");
                return;
            }
            catch
            {
                Reset();
                throw;
            }
        }

        private static void Resolve_TypeInfoDefinitionTable(ref Vmm.ModuleEntry module, Vmm.EATEntry[] eat)
        {
            try
            {
                // mov rax, cs:qword_59759D8
                // lea     r14, [rax+rsi * 8]
                // mov rdi, [r14]
                const string pattern = "48 8B 05 ? ? ? ? 4C 8D 34 F0 49 8B 3E";

                ulong sig = _vmm.FindSignature(_pid, pattern, module.vaBase, module.vaBase + module.cbImageSize);
                sig.ThrowIfInvalidUserVA(nameof(sig));

                int disp32 = Memory.ReadValue<int>(sig + 3);
                ulong typeDefPtrAddr = sig.AddRVA(3 + 4, disp32);
                gTypeInfoDefinitionTable = Memory.ReadValue<ulong>(typeDefPtrAddr);
                gTypeInfoDefinitionTable.ThrowIfInvalidUserVA(nameof(gTypeInfoDefinitionTable));
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"Signature scan failed for TypeInfoDefinitionTable: {ex}. Falling back to static offsets.");
                ulong staticOffset = module.vaBase + 0x598BAD8;
                gTypeInfoDefinitionTable = Memory.ReadValue<ulong>(staticOffset);
                gTypeInfoDefinitionTable.ThrowIfInvalidUserVA(nameof(gTypeInfoDefinitionTable));
            }
            gTypeCount = Memory.ReadValue<int>(gTypeInfoDefinitionTable - 0x10) / 8;
            ArgumentOutOfRangeException.ThrowIfLessThan(gTypeCount, 1, nameof(gTypeCount));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(gTypeCount, 100000, nameof(gTypeCount));
            Logging.WriteLine($"{nameof(gTypeInfoDefinitionTable)} @ 0x{gTypeInfoDefinitionTable:X} (Type Count: {gTypeCount})");
        }

        [Flags]
        public enum FieldAttributes : ushort
        {
            FIELD_ATTRIBUTE_FIELD_ACCESS_MASK = 0x0007,
            FIELD_ATTRIBUTE_PRIVATE_SCOPE = 0x0000,
            FIELD_ATTRIBUTE_PRIVATE = 0x0001,
            FIELD_ATTRIBUTE_FAM_AND_ASSEM = 0x0002,
            FIELD_ATTRIBUTE_ASSEMBLY = 0x0003,
            FIELD_ATTRIBUTE_FAMILY = 0x0004,
            FIELD_ATTRIBUTE_FAM_OR_ASSEM = 0x0005,
            FIELD_ATTRIBUTE_PUBLIC = 0x0006,
            FIELD_ATTRIBUTE_STATIC = 0x0010,
            FIELD_ATTRIBUTE_INIT_ONLY = 0x0020,
            FIELD_ATTRIBUTE_LITERAL = 0x0040,
            FIELD_ATTRIBUTE_NOT_SERIALIZED = 0x0080,
            FIELD_ATTRIBUTE_SPECIAL_NAME = 0x0200,
            FIELD_ATTRIBUTE_PINVOKE_IMPL = 0x2000,
        }

        [Flags]
        public enum MethodAttributes : ushort
        {
            METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK = 0x0007,
            METHOD_ATTRIBUTE_PRIVATE_SCOPE = 0x0000,
            METHOD_ATTRIBUTE_PRIVATE = 0x0001,
            METHOD_ATTRIBUTE_FAM_AND_ASSEM = 0x0002,
            METHOD_ATTRIBUTE_ASSEM = 0x0003,
            METHOD_ATTRIBUTE_FAMILY = 0x0004,
            METHOD_ATTRIBUTE_FAM_OR_ASSEM = 0x0005,
            METHOD_ATTRIBUTE_PUBLIC = 0x0006,
            METHOD_ATTRIBUTE_STATIC = 0x0010,
            METHOD_ATTRIBUTE_FINAL = 0x0020,
            METHOD_ATTRIBUTE_VIRTUAL = 0x0040,
            METHOD_ATTRIBUTE_HIDE_BY_SIG = 0x0080,
            METHOD_ATTRIBUTE_VTABLE_LAYOUT_MASK = 0x0100,
            METHOD_ATTRIBUTE_REUSE_SLOT = 0x0000,
            METHOD_ATTRIBUTE_NEW_SLOT = 0x0100,
            METHOD_ATTRIBUTE_STRICT = 0x0200,
            METHOD_ATTRIBUTE_ABSTRACT = 0x0400,
            METHOD_ATTRIBUTE_SPECIAL_NAME = 0x0800,
        }

        [Flags]
        public enum TypeAttributes : uint
        {
            TYPE_ATTRIBUTE_VISIBILITY_MASK = 0x00000007,
            TYPE_ATTRIBUTE_NOT_PUBLIC = 0x00000000,
            TYPE_ATTRIBUTE_PUBLIC = 0x00000001,
            TYPE_ATTRIBUTE_NESTED_PUBLIC = 0x00000002,
            TYPE_ATTRIBUTE_NESTED_PRIVATE = 0x00000003,
            TYPE_ATTRIBUTE_NESTED_FAMILY = 0x00000004,
            TYPE_ATTRIBUTE_NESTED_ASSEMBLY = 0x00000005,
            TYPE_ATTRIBUTE_NESTED_FAM_AND_ASSEM = 0x00000006,
            TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM = 0x00000007,
            TYPE_ATTRIBUTE_LAYOUT_MASK = 0x00000018,
            TYPE_ATTRIBUTE_AUTO_LAYOUT = 0x00000000,
            TYPE_ATTRIBUTE_SEQUENTIAL_LAYOUT = 0x00000008,
            TYPE_ATTRIBUTE_EXPLICIT_LAYOUT = 0x00000010,
            TYPE_ATTRIBUTE_CLASS_SEMANTIC_MASK = 0x00000020,
            TYPE_ATTRIBUTE_CLASS = 0x00000000,
            TYPE_ATTRIBUTE_INTERFACE = 0x00000020,
            TYPE_ATTRIBUTE_ABSTRACT = 0x00000080,
            TYPE_ATTRIBUTE_SEALED = 0x00000100,
            TYPE_ATTRIBUTE_SPECIAL_NAME = 0x00000400,
        }

        public enum TypeEnum : byte
        {
            IL2CPP_TYPE_END = 0x00,
            IL2CPP_TYPE_VOID = 0x01,
            IL2CPP_TYPE_BOOLEAN = 0x02,
            IL2CPP_TYPE_CHAR = 0x03,
            IL2CPP_TYPE_I1 = 0x04,
            IL2CPP_TYPE_U1 = 0x05,
            IL2CPP_TYPE_I2 = 0x06,
            IL2CPP_TYPE_U2 = 0x07,
            IL2CPP_TYPE_I4 = 0x08,
            IL2CPP_TYPE_U4 = 0x09,
            IL2CPP_TYPE_I8 = 0x0a,
            IL2CPP_TYPE_U8 = 0x0b,
            IL2CPP_TYPE_R4 = 0x0c,
            IL2CPP_TYPE_R8 = 0x0d,
            IL2CPP_TYPE_STRING = 0x0e,
            IL2CPP_TYPE_PTR = 0x0f,
            IL2CPP_TYPE_BYREF = 0x10,
            IL2CPP_TYPE_VALUETYPE = 0x11,
            IL2CPP_TYPE_CLASS = 0x12,
            IL2CPP_TYPE_VAR = 0x13,
            IL2CPP_TYPE_ARRAY = 0x14,
            IL2CPP_TYPE_GENERICINST = 0x15,
            IL2CPP_TYPE_TYPEDBYREF = 0x16,
            IL2CPP_TYPE_I = 0x18,
            IL2CPP_TYPE_U = 0x19,
            IL2CPP_TYPE_FNPTR = 0x1b,
            IL2CPP_TYPE_OBJECT = 0x1c,
            IL2CPP_TYPE_SZARRAY = 0x1d,
            IL2CPP_TYPE_MVAR = 0x1e,
            IL2CPP_TYPE_CMOD_REQD = 0x1f,
            IL2CPP_TYPE_CMOD_OPT = 0x20,
            IL2CPP_TYPE_INTERNAL = 0x21,
            IL2CPP_TYPE_MODIFIER = 0x40,
            IL2CPP_TYPE_SENTINEL = 0x41,
            IL2CPP_TYPE_PINNED = 0x45,
            IL2CPP_TYPE_ENUM = 0x55,
            IL2CPP_TYPE_IL2CPP_TYPE_INDEX = 0xff
        }

        [StructLayout(LayoutKind.Explicit)]
        public readonly struct GlobalMetadataHeader
        {
            [FieldOffset(0x00)] public readonly int sanity;
            [FieldOffset(0x04)] public readonly int version;
            [FieldOffset(0x08)] public readonly int stringLiteralOffset;
            [FieldOffset(0x0C)] public readonly int stringLiteralSize;
            [FieldOffset(0x10)] public readonly int stringLiteralDataOffset;
            [FieldOffset(0x14)] public readonly int stringLiteralDataSize;
            [FieldOffset(0x18)] public readonly int stringOffset;
            [FieldOffset(0x1C)] public readonly int stringSize;
            [FieldOffset(0x20)] public readonly int eventsOffset;
            [FieldOffset(0x24)] public readonly int eventsSize;
            [FieldOffset(0x28)] public readonly int propertiesOffset;
            [FieldOffset(0x2C)] public readonly int propertiesSize;
            [FieldOffset(0x30)] public readonly int methodsOffset;
            [FieldOffset(0x34)] public readonly int methodsSize;
            [FieldOffset(0x38)] public readonly int parameterDefaultValuesOffset;
            [FieldOffset(0x3C)] public readonly int parameterDefaultValuesSize;
            [FieldOffset(0x40)] public readonly int fieldDefaultValuesOffset;
            [FieldOffset(0x44)] public readonly int fieldDefaultValuesSize;
            [FieldOffset(0x48)] public readonly int fieldAndParameterDefaultValueDataOffset;
            [FieldOffset(0x4C)] public readonly int fieldAndParameterDefaultValueDataSize;
            [FieldOffset(0x50)] public readonly int fieldMarshaledSizesOffset;
            [FieldOffset(0x54)] public readonly int fieldMarshaledSizesSize;
            [FieldOffset(0x58)] public readonly int parametersOffset;
            [FieldOffset(0x5C)] public readonly int parametersSize;
            [FieldOffset(0x60)] public readonly int fieldsOffset;
            [FieldOffset(0x64)] public readonly int fieldsSize;
            [FieldOffset(0x68)] public readonly int genericParametersOffset;
            [FieldOffset(0x6C)] public readonly int genericParametersSize;
            [FieldOffset(0x70)] public readonly int genericParameterConstraintsOffset;
            [FieldOffset(0x74)] public readonly int genericParameterConstraintsSize;
            [FieldOffset(0x78)] public readonly int genericContainersOffset;
            [FieldOffset(0x7C)] public readonly int genericContainersSize;
            [FieldOffset(0x80)] public readonly int nestedTypesOffset;
            [FieldOffset(0x84)] public readonly int nestedTypesSize;
            [FieldOffset(0x88)] public readonly int interfacesOffset;
            [FieldOffset(0x8C)] public readonly int interfacesSize;
            [FieldOffset(0x90)] public readonly int vtableMethodsOffset;
            [FieldOffset(0x94)] public readonly int vtableMethodsSize;
            [FieldOffset(0x98)] public readonly int interfaceOffsetsOffset;
            [FieldOffset(0x9C)] public readonly int interfaceOffsetsSize;
            [FieldOffset(0xA0)] public readonly int typeDefinitionsOffset;
            [FieldOffset(0xA4)] public readonly int typeDefinitionsSize;
            [FieldOffset(0xA8)] public readonly int imagesOffset;
            [FieldOffset(0xAC)] public readonly int imagesSize;
            [FieldOffset(0xB0)] public readonly int assembliesOffset;
            [FieldOffset(0xB4)] public readonly int assembliesSize;
            [FieldOffset(0xB8)] public readonly int fieldRefsOffset;
            [FieldOffset(0xBC)] public readonly int fieldRefsSize;
            [FieldOffset(0xC0)] public readonly int referencedAssembliesOffset;
            [FieldOffset(0xC4)] public readonly int referencedAssembliesSize;
            [FieldOffset(0xC8)] public readonly int attributeDataOffset;
            [FieldOffset(0xCC)] public readonly int attributeDataSize;
            [FieldOffset(0xD0)] public readonly int attributeDataRangeOffset;
            [FieldOffset(0xD4)] public readonly int attributeDataRangeSize;
            [FieldOffset(0xD8)] public readonly int unresolvedVirtualCallParameterTypesOffset;
            [FieldOffset(0xDC)] public readonly int unresolvedVirtualCallParameterTypesSize;
            [FieldOffset(0xE0)] public readonly int unresolvedVirtualCallParameterRangesOffset;
            [FieldOffset(0xE4)] public readonly int unresolvedVirtualCallParameterRangesSize;
            [FieldOffset(0xE8)] public readonly int windowsRuntimeTypeNamesOffset;
            [FieldOffset(0xEC)] public readonly int windowsRuntimeTypeNamesSize;
            [FieldOffset(0xF0)] public readonly int windowsRuntimeStringsOffset;
            [FieldOffset(0xF4)] public readonly int windowsRuntimeStringsSize;
            [FieldOffset(0xF8)] public readonly int exportedTypeDefinitionsOffset;
            [FieldOffset(0xFC)] public readonly int exportedTypeDefinitionsSize;
        }

        [StructLayout(LayoutKind.Explicit)]
        public readonly struct TypeDefinition
        {
            [FieldOffset(0x00)] public readonly int nameIndex;
            [FieldOffset(0x04)] public readonly int namespaceIndex;
            [FieldOffset(0x08)] public readonly int byvalTypeIn;
            [FieldOffset(0x0C)] public readonly int declaringTypeIndex;
            [FieldOffset(0x10)] public readonly int parentIndex;
            [FieldOffset(0x14)] public readonly int elementTypeIndex;
            [FieldOffset(0x18)] public readonly int genericContainerIndex;
            [FieldOffset(0x1C)] public readonly uint flags;
            [FieldOffset(0x20)] public readonly int fieldStart;
            [FieldOffset(0x24)] public readonly int methodStart;
            [FieldOffset(0x28)] public readonly int eventStart;
            [FieldOffset(0x2C)] public readonly int propertyStart;
            [FieldOffset(0x30)] public readonly int nestedTypesStart;
            [FieldOffset(0x34)] public readonly int interfacesStart;
            [FieldOffset(0x38)] public readonly int vtableStart;
            [FieldOffset(0x3C)] public readonly int interfaceOffsetsStart;
            [FieldOffset(0x40)] public readonly ushort method_count;
            [FieldOffset(0x42)] public readonly ushort property_count;
            [FieldOffset(0x44)] public readonly ushort field_count;
            [FieldOffset(0x46)] public readonly ushort event_count;
            [FieldOffset(0x48)] public readonly ushort nested_type_count;
            [FieldOffset(0x4A)] public readonly ushort vtable_count;
            [FieldOffset(0x4C)] public readonly ushort interfaces_count;
            [FieldOffset(0x4E)] public readonly ushort interface_offsets_count;
            [FieldOffset(0x50)] public readonly uint bitfield;
            [FieldOffset(0x54)] public readonly uint token;
        }

        [StructLayout(LayoutKind.Explicit)]
        public readonly struct DynamicArray64
        {
            [FieldOffset(0x00)] public readonly ulong m_data;
            [FieldOffset(0x08)] public readonly ulong m_label;
            [FieldOffset(0x10)] public readonly ulong m_size;
            [FieldOffset(0x18)] public readonly ulong m_capacity;
        }

        [StructLayout(LayoutKind.Explicit)]
        public readonly struct GenericClass
        {
            [FieldOffset(0x00)] public readonly ulong type;
            [FieldOffset(0x08)] public readonly ulong context;
            [FieldOffset(0x10)] public readonly ulong cached_class;
        }

        [StructLayout(LayoutKind.Explicit)]
        public readonly struct Object
        {
            [FieldOffset(0x00)] public readonly ulong klass;
            [FieldOffset(0x08)] public readonly ulong monitor;
        }

        [StructLayout(LayoutKind.Explicit)]
        public readonly struct ScriptingObjectPtr
        {
            [FieldOffset(0x00)] public readonly ulong target;
        }

        [StructLayout(LayoutKind.Explicit)]
        public readonly struct ScriptingGCHandle
        {
            [FieldOffset(0x00)] public readonly ulong handle;
            [FieldOffset(0x08)] public readonly ulong weakness;
            [FieldOffset(0x10)] public readonly ScriptingObjectPtr @object;
        }

        // struct ScriptingObject { uint8_t pad0000[0x10]; ScriptingGCHandle monoreference; };
        [StructLayout(LayoutKind.Explicit)]
        public readonly struct ScriptingObject
        {
            [FieldOffset(0x10)] public readonly ScriptingGCHandle monoreference;
        }

        // struct ComponentPair { uint8_t pad0000[0x8]; Component* component; };
        [StructLayout(LayoutKind.Explicit)]
        public readonly struct ComponentPair
        {
            [FieldOffset(0x08)] public readonly ulong component;
        }

        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct ComponentPairContainer
        {
            [FieldOffset(0x00)] public fixed byte arr[65565 * 16];
        }

        // struct Component : ScriptingObject { uint64_t gameobject; uint8_t pad0038[0x38]; DynamicArray<Component*> childern; };
        [StructLayout(LayoutKind.Explicit)]
        public readonly struct Component
        {
            [FieldOffset(0x00)] public readonly ScriptingObject obj;
            [FieldOffset(0x30)] public readonly ulong gameobject;
            [FieldOffset(0x70)] public readonly DynamicArray64 childern;
        }

        // struct Renderer : Component { uint8_t pad0090[0xB8]; DynamicArray<uint64_t> materials; // 0x148 };
        [StructLayout(LayoutKind.Explicit)]
        public readonly struct Renderer
        {
            [FieldOffset(0x00)] public readonly Component component;
            [FieldOffset(0x148)] public readonly DynamicArray64 materials;
        }

        // struct GameObject : ScriptingObject { DynamicArray<uint64_t> components; uint8_t pad00[0x10]; uint64_t name; };
        [StructLayout(LayoutKind.Explicit)]
        public readonly struct GameObject
        {
            [FieldOffset(0x00)] public readonly ScriptingObject obj;
            [FieldOffset(0x28)] public readonly DynamicArray64 components;
            [FieldOffset(0x58)] public readonly ulong name;

            public readonly string GetName() => Memory.ReadAsciiString(name);

            public Component GetComponentByName(string klass_name)
            {
                throw new NotImplementedException();
            }

            public List<Component> GetComponentsInChildren(string klass_name)
            {
                throw new NotImplementedException();
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct Type
        {
            [FieldOffset(0x00)] public readonly ulong data;
            [FieldOffset(0x08)] public readonly ushort attrs;
            [FieldOffset(0x0A)] public readonly byte type;
            [FieldOffset(0x0B)] private byte _bitfield;

            public byte Num_mods
            {
                readonly get => (byte)(_bitfield & 0x1F);
                set => _bitfield = (byte)((_bitfield & ~0x1F) | (value & 0x1F));
            }

            public bool Byref
            {
                readonly get => (_bitfield & 0x20) != 0;
                set => _bitfield = value ? (byte)(_bitfield | 0x20) : (byte)(_bitfield & ~0x20);
            }

            public bool Pinned
            {
                readonly get => (_bitfield & 0x40) != 0;
                set => _bitfield = value ? (byte)(_bitfield | 0x40) : (byte)(_bitfield & ~0x40);
            }

            public bool ValueType
            {
                readonly get => (_bitfield & 0x80) != 0;
                set => _bitfield = value ? (byte)(_bitfield | 0x80) : (byte)(_bitfield & ~0x80);
            }

            public readonly string GetName()
            {
                var typeEnum = (TypeEnum)type;

                if (typeEnum == TypeEnum.IL2CPP_TYPE_END)
                    return "void";

                switch (typeEnum)
                {
                    case TypeEnum.IL2CPP_TYPE_VOID: return "void";
                    case TypeEnum.IL2CPP_TYPE_BOOLEAN: return "bool";
                    case TypeEnum.IL2CPP_TYPE_CHAR: return "char";
                    case TypeEnum.IL2CPP_TYPE_I1: return "int8_t";
                    case TypeEnum.IL2CPP_TYPE_U1: return "uint8_t";
                    case TypeEnum.IL2CPP_TYPE_I2: return "int16_t";
                    case TypeEnum.IL2CPP_TYPE_U2: return "uint16_t";
                    case TypeEnum.IL2CPP_TYPE_I4: return "int32_t";
                    case TypeEnum.IL2CPP_TYPE_U4: return "uint32_t";
                    case TypeEnum.IL2CPP_TYPE_I8: return "int64_t";
                    case TypeEnum.IL2CPP_TYPE_U8: return "uint64_t";
                    case TypeEnum.IL2CPP_TYPE_R4: return "float";
                    case TypeEnum.IL2CPP_TYPE_R8: return "double";
                    case TypeEnum.IL2CPP_TYPE_STRING: return "string";
                    case TypeEnum.IL2CPP_TYPE_OBJECT: return "object";
                    default: return "object"; // TODO: Expand type handling
                }

                if (typeEnum == TypeEnum.IL2CPP_TYPE_PTR ||
                    typeEnum == TypeEnum.IL2CPP_TYPE_BYREF ||
                    typeEnum == TypeEnum.IL2CPP_TYPE_SZARRAY)
                {
                    Type nested = Memory.ReadValue<Type>(data);
                    string inner = nested.GetName();
                    if (string.IsNullOrEmpty(inner) || inner.Length > 100)
                        return string.Empty;

                    if (typeEnum == TypeEnum.IL2CPP_TYPE_PTR) return inner + "*";
                    if (typeEnum == TypeEnum.IL2CPP_TYPE_BYREF) return inner + "&";
                    return inner + "[]";
                }

                if (typeEnum == TypeEnum.IL2CPP_TYPE_VALUETYPE ||
                    typeEnum == TypeEnum.IL2CPP_TYPE_CLASS)
                {
                    if (data < 0x10000 || data > 0x7FFFFFFFFFFF)
                        return string.Empty;

                    var header = Memory.ReadValue<GlobalMetadataHeader>(gMetadataGlobalHeader);
                    var type_def = Memory.ReadValue<TypeDefinition>(data);

                    ulong name_offset = gGlobalMetadata + (ulong)header.stringOffset + (ulong)type_def.nameIndex;
                    if (name_offset < 0x10000 || name_offset > 0x7FFFFFFFFFFF)
                        return string.Empty;

                    string name = Memory.ReadAsciiString(name_offset);
                    if (string.IsNullOrEmpty(name) || name.Length > 100)
                        return string.Empty;

                    return name;
                }

                if (typeEnum == TypeEnum.IL2CPP_TYPE_GENERICINST)
                {
                    if (data < 0x10000 || data > 0x7FFFFFFFFFFF)
                        return string.Empty;

                    var gclass = Memory.ReadValue<GenericClass>(data);
                    if (gclass.context == 0 || gclass.type == 0)
                        return string.Empty;

                    Type baseType = Memory.ReadValue<Type>(gclass.type);
                    string base_name = baseType.GetName();
                    if (string.IsNullOrEmpty(base_name) || base_name.Length > 100)
                        return string.Empty;

                    ulong contextListPtr = Memory.ReadValue<ulong>(gclass.context + 0x8);
                    if (contextListPtr == 0)
                        return base_name;

                    ulong il2cpp_type_ptr = Memory.ReadValue<ulong>(contextListPtr);
                    if (il2cpp_type_ptr == 0)
                        return base_name;

                    Type argType = Memory.ReadValue<Type>(il2cpp_type_ptr);
                    string arg_type = argType.GetName();
                    if (string.IsNullOrEmpty(arg_type) || arg_type.Length > 100)
                        return base_name;

                    return base_name + "<" + arg_type + ">";
                }

                return string.Empty;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public readonly struct MethodInfo
        {
            [FieldOffset(0x00)] public readonly ulong method_ptr;
            [FieldOffset(0x08)] public readonly ulong virt_method_invoker;
            [FieldOffset(0x10)] public readonly ulong invoker_method;
            [FieldOffset(0x18)] public readonly ulong name;
            [FieldOffset(0x20)] public readonly ulong klass;
            [FieldOffset(0x28)] public readonly ulong returntype;
            [FieldOffset(0x30)] public readonly ulong parameters;
            [FieldOffset(0x38)] public readonly ulong parameter_info;
            [FieldOffset(0x40)] public readonly uint token;
            [FieldOffset(0x44)] public readonly ushort flags;
            [FieldOffset(0x46)] public readonly ushort iflags;
            [FieldOffset(0x48)] public readonly ushort slot;
            [FieldOffset(0x4A)] public readonly byte param_count;
            [FieldOffset(0x4B)] public readonly byte bitflags;
            [FieldOffset(0x4C)] public readonly ushort mod_flags;

            public readonly string GetName() => Memory.ReadAsciiString(name);

            public Type GetReturnType()
            {
                return Memory.ReadValue<Type>(returntype);
            }

            public readonly string GetParameters()
            {
                string result = "(";

                int stringOffset = Memory.ReadValue<int>(IL2CPPLib.gMetadataGlobalHeader + 0x18);
                int parameterDefTableOffset = Memory.ReadValue<int>(IL2CPPLib.gMetadataGlobalHeader + 0x58);
                int parameterStart = Memory.ReadValue<int>(parameter_info + 0x10);

                bool hasParameters = false;

                for (uint i = 0; i < param_count; i++)
                {
                    ulong paramDefAddr = IL2CPPLib.gGlobalMetadata + (ulong)parameterDefTableOffset + 0xC * (i + (uint)parameterStart);
                    int nameIndex = Memory.ReadValue<int>(paramDefAddr);
                    if (nameIndex <= 0 || nameIndex > 0x1000000)
                        break;

                    ulong paramNamePtr = IL2CPPLib.gGlobalMetadata + (ulong)stringOffset + (ulong)nameIndex;

                    string paramName = Memory.ReadAsciiString(paramNamePtr);
                    if (string.IsNullOrEmpty(paramName) || paramName.Length > 100)
                        break;

                    ulong paramTypePtr = Memory.ReadValue<ulong>(parameters + i * (ulong)IntPtr.Size);
                    if (paramTypePtr < 0x10000 || paramTypePtr > 0x7FFFFFFFFFFF)
                        break;

                    Type type = Memory.ReadValue<Type>(paramTypePtr);
                    string typeName = type.GetName();
                    if (string.IsNullOrEmpty(typeName) || typeName.Length > 100)
                        break;

                    if (hasParameters) result += ", ";
                    result += typeName + " " + paramName;
                    hasParameters = true;
                }

                result += ")";
                return result;
            }

            public string GetModifier()
            {
                string result;

                var access = (MethodAttributes)(mod_flags & (ushort)MethodAttributes.METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK);

                switch (access)
                {
                    case MethodAttributes.METHOD_ATTRIBUTE_PRIVATE:
                        result = "private";
                        break;
                    case MethodAttributes.METHOD_ATTRIBUTE_FAM_AND_ASSEM:
                        result = "protected internal";
                        break;
                    case MethodAttributes.METHOD_ATTRIBUTE_ASSEM:
                        result = "internal";
                        break;
                    case MethodAttributes.METHOD_ATTRIBUTE_FAMILY:
                        result = "protected";
                        break;
                    case MethodAttributes.METHOD_ATTRIBUTE_FAM_OR_ASSEM:
                        result = "private protected";
                        break;
                    case MethodAttributes.METHOD_ATTRIBUTE_PUBLIC:
                        result = "public";
                        break;
                    default:
                        result = "private";
                        break;
                }

                if ((mod_flags & (ushort)MethodAttributes.METHOD_ATTRIBUTE_STATIC) != 0)
                    result += " static";
                if ((mod_flags & (ushort)MethodAttributes.METHOD_ATTRIBUTE_ABSTRACT) != 0)
                    result += " abstract";
                if ((mod_flags & (ushort)MethodAttributes.METHOD_ATTRIBUTE_FINAL) != 0)
                    result += " sealed";
                if ((mod_flags & (ushort)MethodAttributes.METHOD_ATTRIBUTE_VIRTUAL) != 0)
                    result += " virtual";

                return result;
            }

            public readonly string GetParameter(int index)
            {
                if (index < 0 || index >= param_count)
                    return string.Empty;

                int stringOffset = Memory.ReadValue<int>(IL2CPPLib.gMetadataGlobalHeader + 0x18);
                int parameterDefTableOffset = Memory.ReadValue<int>(IL2CPPLib.gMetadataGlobalHeader + 0x58);
                int parameterStart = Memory.ReadValue<int>(parameter_info + 0x10);

                ulong paramDefAddr = IL2CPPLib.gGlobalMetadata + (ulong)parameterDefTableOffset + 0xC * ((uint)index + (uint)parameterStart);
                int nameIndex = Memory.ReadValue<int>(paramDefAddr);
                if (nameIndex <= 0 || nameIndex > 0x1000000)
                    return string.Empty;

                ulong paramNamePtr = IL2CPPLib.gGlobalMetadata + (ulong)stringOffset + (ulong)nameIndex;

                string paramName = Memory.ReadAsciiString(paramNamePtr);
                if (string.IsNullOrEmpty(paramName) || paramName.Length > 100)
                    return string.Empty;

                ulong paramTypePtr = Memory.ReadValue<ulong>(parameters + (ulong)index * (ulong)IntPtr.Size);
                if (paramTypePtr < 0x10000 || paramTypePtr > 0x7FFFFFFFFFFF)
                    return string.Empty;

                Type type = Memory.ReadValue<Type>(paramTypePtr);
                string typeName = type.GetName();
                if (string.IsNullOrEmpty(typeName) || typeName.Length > 100)
                    return string.Empty;

                return typeName + " " + paramName;
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = FieldInfo.Size)]
        public readonly struct FieldInfo
        {
            public const int Size = 0x20;

            [FieldOffset(0x00)] public readonly ulong name;
            [FieldOffset(0x08)] public readonly ulong type;
            [FieldOffset(0x10)] public readonly ulong token;
            [FieldOffset(0x18)] public readonly ushort offset;

            public readonly string GetName() => Memory.ReadAsciiString(name);

            public Type GetTypeInfo()
            {
                return Memory.ReadValue<Type>(type);
            }

            public bool IsStatic()
            {
                return (GetTypeInfo().attrs & (ushort)FieldAttributes.FIELD_ATTRIBUTE_STATIC) != 0;
            }

            public string GetModifier()
            {
                ushort access = (ushort)(GetTypeInfo().attrs & (ushort)FieldAttributes.FIELD_ATTRIBUTE_FIELD_ACCESS_MASK);
                switch ((FieldAttributes)access)
                {
                    case FieldAttributes.FIELD_ATTRIBUTE_PRIVATE:
                        return "private";
                    case FieldAttributes.FIELD_ATTRIBUTE_PUBLIC:
                        return "public";
                    case FieldAttributes.FIELD_ATTRIBUTE_FAMILY:
                        return "protected";
                    case FieldAttributes.FIELD_ATTRIBUTE_ASSEMBLY:
                        return "internal";
                    case FieldAttributes.FIELD_ATTRIBUTE_FAM_AND_ASSEM:
                        return "protected internal";
                    case FieldAttributes.FIELD_ATTRIBUTE_FAM_OR_ASSEM:
                        return "protected internal";
                    case FieldAttributes.FIELD_ATTRIBUTE_PRIVATE_SCOPE:
                        return "private";
                    default:
                        return "unknown";
                }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public readonly struct Class
        {
            [FieldOffset(0x00)] public readonly ulong image;
            [FieldOffset(0x08)] public readonly ulong gc_desc;
            [FieldOffset(0x10)] public readonly ulong name;
            [FieldOffset(0x18)] public readonly ulong namespaze;
            [FieldOffset(0x20)] public readonly Type type;
            [FieldOffset(0x58)] public readonly ulong parent; // Class*
            [FieldOffset(0x80)] public readonly ulong fields;
            [FieldOffset(0x88)] public readonly ulong events;
            [FieldOffset(0x90)] public readonly ulong properties;
            [FieldOffset(0x98)] public readonly ulong methods;
            [FieldOffset(0x118)] public readonly uint flags;
            [FieldOffset(0x11C)] public readonly uint token;
            [FieldOffset(0x120)] public readonly ushort method_count;
            [FieldOffset(0x122)] public readonly ushort property_count;
            [FieldOffset(0x124)] public readonly ushort field_count;
            [FieldOffset(0x126)] public readonly ushort event_count;
            [FieldOffset(0xB8)] public readonly ulong static_fields;
            [FieldOffset(0xC8)] public readonly ulong typeHierarchy;

            public static Dictionary<string, ulong> GetTypeTable()
            {
                using var ptrs = _vmm.MemReadPooled<ulong>(_pid, gTypeInfoDefinitionTable, gTypeCount) ??
                    throw new InvalidOperationException("Failed to read type definition table.");
                var klasses = new Dictionary<string, ulong>(gTypeCount, StringComparer.OrdinalIgnoreCase);
                foreach (var ptr in ptrs.Memory.Span)
                {
                    var klass = Memory.ReadValue<Class>(ptr);
                    klasses.TryAdd(klass.ToString(), ptr);
                }
                return klasses;
            }

            public static ulong FindClass(string name)
            {
                using var ptrs = _vmm.MemReadPooled<ulong>(_pid, gTypeInfoDefinitionTable, gTypeCount) ??
                    throw new InvalidOperationException("Failed to read type definition table.");
                foreach (var ptr in ptrs.Memory.Span)
                {
                    var klass = Memory.ReadValue<Class>(ptr);
                    if (klass.ToString().Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return ptr;
                    }
                }
                throw new InvalidOperationException($"Class '{name}' not found.");
            }

            public readonly string GetName() => Memory.ReadAsciiString(name);
            public readonly string GetNamespace()
            {
                if (!namespaze.IsValidUserVA())
                    return string.Empty;
                return Memory.ReadAsciiString(namespaze);
            }
            public Class? GetParent()
            {
                if (!parent.IsValidUserVA())
                    return null;
                return Memory.ReadValue<Class>(parent);
            }

            public override string ToString()
            {
                string ns = GetNamespace();
                if (string.IsNullOrEmpty(ns))
                    return GetName();
                return $"{ns}.{GetName()}";
            }

            public FieldInfo GetField(int index)
            {
                ulong addr = fields + (ulong)index * (ulong)FieldInfo.Size;
                return Memory.ReadValue<FieldInfo>(addr);
            }

            public IReadOnlyList<FieldInfo> GetFields()
            {
                var fieldList = new List<FieldInfo>(field_count);
                for (int i = 0; i < field_count; i++)
                {
                    fieldList.Add(GetField(i));
                }
                return fieldList;
            }

            public MethodInfo GetMethod(int index)
            {
                ulong methodPtr = Memory.ReadValue<ulong>(methods + (ulong)index * (ulong)IntPtr.Size);
                return Memory.ReadValue<MethodInfo>(methodPtr);
            }

            public readonly string GetModifer()
            {
                string result;
                ushort access = (ushort)(flags & (uint)TypeAttributes.TYPE_ATTRIBUTE_VISIBILITY_MASK);

                switch ((TypeAttributes)access)
                {
                    case TypeAttributes.TYPE_ATTRIBUTE_PUBLIC:
                    case TypeAttributes.TYPE_ATTRIBUTE_NESTED_PUBLIC:
                        result = "public";
                        break;
                    case TypeAttributes.TYPE_ATTRIBUTE_NOT_PUBLIC:
                    case TypeAttributes.TYPE_ATTRIBUTE_NESTED_PRIVATE:
                        result = "private";
                        break;
                    case TypeAttributes.TYPE_ATTRIBUTE_NESTED_FAMILY:
                        result = "protected";
                        break;
                    case TypeAttributes.TYPE_ATTRIBUTE_NESTED_ASSEMBLY:
                        result = "internal";
                        break;
                    case TypeAttributes.TYPE_ATTRIBUTE_NESTED_FAM_AND_ASSEM:
                        result = "protected internal";
                        break;
                    case TypeAttributes.TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM:
                        result = "private protected";
                        break;
                    default:
                        result = "unknown";
                        break;
                }

                bool is_abstract = (flags & (uint)TypeAttributes.TYPE_ATTRIBUTE_ABSTRACT) != 0;
                bool is_sealed = (flags & (uint)TypeAttributes.TYPE_ATTRIBUTE_SEALED) != 0;

                if (is_abstract && is_sealed)
                    result += " static";
                else if (is_abstract)
                    result += " abstract";
                else if (is_sealed)
                    result += " sealed";

                return result;
            }

            public FieldInfo FindFieldByName(string modifer_name, string type_name, string field_name)
            {
                if (fields == 0)
                    return default;

                if (field_count <= 0)
                    return default;

                for (int i = 0; i < field_count; i++)
                {
                    var field = GetField(i);
                    if (field.name == 0)
                        continue;

                    string fieldname = field.GetName();
                    if (string.IsNullOrEmpty(fieldname))
                        continue;

                    var type = field.GetTypeInfo();
                    if (type.data < 0x1000 || type.data > 0x7FFFFFFFFFFF)
                        continue;

                    string tname = type.GetName();
                    if (string.IsNullOrEmpty(tname))
                        continue;

                    string modifername = field.GetModifier();

                    if ((string.IsNullOrEmpty(modifer_name) || modifer_name == modifername) &&
                        (string.IsNullOrEmpty(type_name) || tname.Contains(type_name)) &&
                        fieldname.Contains(field_name))
                    {
                        return field;
                    }
                }

                return default;
            }

            public MethodInfo FindMethodByName(string modifier_name, string return_type, string method_name, int param_count)
            {
                if (methods == 0 || method_count <= 0)
                    return default;

                for (int i = 0; i < method_count; i++)
                {
                    var method = GetMethod(i);
                    if (method.name == 0)
                        continue;

                    string methodname = method.GetName();
                    if (!string.IsNullOrEmpty(method_name) && !methodname.Contains(method_name))
                        continue;

                    string returntypename = method.GetReturnType().GetName();
                    if (!string.IsNullOrEmpty(return_type) && !returntypename.Contains(return_type))
                        continue;

                    string modifier = method.GetModifier();
                    if (!string.IsNullOrEmpty(modifier_name) && modifier != modifier_name)
                        continue;

                    string paramsstr = method.GetParameters();

                    if (paramsstr.Length >= 2 && paramsstr[0] == '(' && paramsstr[^1] == ')')
                        paramsstr = paramsstr.Substring(1, paramsstr.Length - 2);

                    int actual_param_count = 0;
                    if (!string.IsNullOrEmpty(paramsstr))
                        actual_param_count = paramsstr.Split(',').Length;

                    if (param_count != -1 && actual_param_count != param_count)
                        continue;

                    return method;
                }

                return default;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public readonly struct Image
        {
            [FieldOffset(0x00)] public readonly ulong name;
            [FieldOffset(0x18)] public readonly uint type_count;
            [FieldOffset(0x28)] public readonly ulong type_start;

            public readonly string GetName() => Memory.ReadAsciiString(name);

            public uint GetTypeIndexBase()
            {
                return Memory.ReadValue<uint>(type_start);
            }

            public Class GetClass(int index)
            {
                uint type_index = GetTypeIndexBase();
                ulong classPtrPtr = IL2CPPLib.gTypeInfoDefinitionTable + (ulong)((type_index + (uint)index) * (uint)IntPtr.Size);
                ulong classPtr = Memory.ReadValue<ulong>(classPtrPtr);
                return Memory.ReadValue<Class>(classPtr);
            }

            public IReadOnlyList<Class> GetAllClasses()
            {
                var klasses = new List<Class>((int)type_count);
                for (int i = 0; i < type_count; i++)
                {
                    var klass = GetClass(i);
                    klasses.Add(klass);
                }
                return klasses;
            }

            public Class FindClassByName(string class_name)
            {
                if (type_start == 0)
                    return default;

                if (type_count <= 0)
                    return default;

                for (int i = 0; i < type_count; i++)
                {
                    var klass = GetClass(i);
                    if (klass.image == 0)
                        continue;

                    var classname = klass.GetName();
                    if (string.IsNullOrEmpty(classname))
                        continue;

                    if (classname == class_name)
                        return klass;
                }

                return default;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public readonly struct Assembly
        {
            [FieldOffset(0x00)] public readonly ulong image;

            public static int GetAssemblyCount() => (int)((gAssembliesEnd - gAssembliesStart) / 8u);
            public static Assembly GetAssembly(int index)
            {
                ulong assemblyPtrPtr = gAssembliesStart + (ulong)(index * 8);
                ulong assemblyPtr = Memory.ReadValue<ulong>(assemblyPtrPtr);
                assemblyPtr.ThrowIfInvalidUserVA(nameof(assemblyPtr));
                return Memory.ReadValue<Assembly>(assemblyPtr);
            }

            public static IReadOnlyList<Assembly> GetAllAssemblies()
            {
                int count = GetAssemblyCount();
                var assemblies = new List<Assembly>(count);
                for (int i = 0; i < count; i++)
                {
                    assemblies.Add(GetAssembly(i));
                }
                return assemblies;
            }

            public Image GetImage()
            {
                return Memory.ReadValue<Image>(image);
            }
        }
    }
}
