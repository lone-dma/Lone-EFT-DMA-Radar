using eft_dma_radar.DMA.ScatterAPI;
using eft_dma_radar.DMA;
using eft_dma_radar.Misc;
using eft_dma_radar.Unity.Collections;

namespace eft_dma_radar.Unity
{
    internal static class MonoLib
    {
        /// <summary>
        /// GameWorld Mono Field. Can be deref'd for a new LocalGameWorld instance each raid.
        /// </summary>
        public static ulong GameWorldField { get; private set; }

        /// <summary>
        /// Initialize Mono at Game Startup.
        /// </summary>
        public static void InitializeEFT()
        {
            try
            {
                Debug.WriteLine("Initializing Mono...");
                var gameWorldField = Singleton.FindOne("GameWorld");
                gameWorldField.ThrowIfInvalidVirtualAddress("Failed to get GameWorld");
                GameWorldField = gameWorldField;
                Debug.WriteLine("Mono Init [OK]");
            }
            catch (Exception ex)
            {
                Reset();
                throw new InvalidOperationException("Mono Init [FAIL]", ex);
            }
        }


        /// <summary>
        /// Reset MonoLib (usually after game closure/reopening).
        /// </summary>
        public static void Reset()
        {
            GameWorldField = default;
        }


        #region Internal Functions

        /// <summary>
        /// Custom Read Method for Mono Interop.
        /// Does not throw exceptions.
        /// </summary>
        private static MonoValue<T> MonoRead<T>(ulong addr, bool useCache = true)
            where T : unmanaged
        {
            try
            {
                ArgumentOutOfRangeException.ThrowIfZero(addr, nameof(addr));
                var result = Memory.ReadValue<T>(addr, useCache);
                return new MonoValue<T>(addr, ref result);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Custom Ptr Read Method for Mono Interop.
        /// Does not throw exceptions.
        /// </summary>
        private static ulong MonoReadPtr(ulong addr, bool useCache = true)
        {
            try
            {
                var pointer = Memory.ReadValue<ulong>(addr, useCache);
                pointer.ThrowIfInvalidVirtualAddress();
                return pointer;
            }
            catch
            {
                return 0x0;
            }
        }

        private static ushort UTF8ToUTF16(string val)
        {
            Span<byte> utf16Bytes = stackalloc byte[2 * val.Length]; // UTF-16 can be up to 2 bytes per char
            utf16Bytes.Clear();

            var byteCount = Encoding.Unicode.GetBytes(val.AsSpan(), utf16Bytes);
            if (byteCount < 2)
                throw new ArgumentException("Input string is too short.", nameof(val));

            return BitConverter.ToUInt16(utf16Bytes);
        }

        private static string ReadWidechar(ulong addr, int size)
        {
            try
            {
                return Memory.ReadString(addr, size);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ReadName(ulong addr, int length)
        {
            try
            {
                if (length % 2 != 0)
                    length++;

                ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 0x1000, nameof(length));
                Span<byte> buffer = stackalloc byte[length];
                buffer.Clear();
                Memory.ReadBuffer(addr, buffer);
                if (buffer[0] >= 0xE0)
                {
                    var nullIndex = buffer.IndexOf((byte)0);
                    var value = nullIndex >= 0 ? Encoding.UTF8.GetString(buffer.Slice(0, nullIndex)) : Encoding.UTF8.GetString(buffer);
                    return $"\\u{UTF8ToUTF16(value):X4}";
                }
                else
                {
                    var nullIndex = buffer.IndexOf((byte)0);
                    return nullIndex >= 0 ? Encoding.UTF8.GetString(buffer.Slice(0, nullIndex)) : Encoding.UTF8.GetString(buffer);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Wrapper for values read from Memory.
        /// Includes virtual address of the value read.
        /// </summary>
        /// <typeparam name="T">Value Type</typeparam>
        public readonly struct MonoValue<T>
            where T : unmanaged
        {
            public static implicit operator ulong(MonoValue<T> x) => x.Address;

            /// <summary>
            /// Virtual address of this value.
            /// </summary>
            public readonly ulong Address;
            /// <summary>
            /// Value structure.
            /// </summary>
            public readonly T Value;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="address">Virtual address of value.</param>
            /// <param name="value">Value to set.</param>
            public MonoValue(ulong address, ref T value)
            {
                Address = address;
                Value = value;
            }
        }

        #endregion

        #region Mono Types
        private static class Singleton
        {
            [StructLayout(LayoutKind.Explicit, Pack = 1)]
            private readonly struct SingletonHashTable
            {
                [FieldOffset(0x0)]
                public readonly int TableSize;
                [FieldOffset(0x8)]
                public readonly ulong KVS; // key_value_pair
            }

            [StructLayout(LayoutKind.Explicit, Pack = 1)]
            private readonly struct GenericClassPtrEntry
            {
                [FieldOffset(0x8)]
                public readonly ulong Ptr;
            }

            public static ulong FindOne(string className)
            {
                return FindMany(className)[0];
            }

            public static ulong[] FindMany(params string[] classNames)
            {
                ArgumentNullException.ThrowIfNull(classNames, nameof(classNames));
                ulong[] results = new ulong[classNames.Length];
                int foundCount = 0;

                ulong monoImageSetPtrBase = Memory.MonoBase + 0x751980; // img_set_cache (MonoImageSet)

                using var monoImageSetPtrArrayLease = MemArray<ulong>.Lease(monoImageSetPtrBase, 1103, true, out var monoImageSetPtrArray);
                using var mapOuterLease = ScatterReadMap.Lease(out var mapOuter);
                var r1 = mapOuter.AddRound();
                var r2 = mapOuter.AddRound();
                var r3 = mapOuter.AddRound();
                for (int ix = 0; ix < 1103; ix++)
                {
                    int i = ix;
                    ulong monoImageSetPtr = monoImageSetPtrArray[i];
                    if (monoImageSetPtr == 0x0)
                        continue;
                    r1[i].AddEntry<MemPointer>(0, monoImageSetPtr + 0x28); // gclass_cache
                    r1[i].Callbacks += x1 =>
                    {
                        if (foundCount == classNames.Length)
                        {
                            return;
                        }
                        if (x1.TryGetResult<MemPointer>(0, out var gclassCache))
                        {
                            r2[i].AddEntry<MemPointer>(1, gclassCache + 0x0);
                            r2[i].Callbacks += x2 =>
                            {
                                if (x2.TryGetResult<MemPointer>(1, out var table))
                                {
                                    r3[i].AddEntry<SingletonHashTable>(2, table);
                                    r3[i].Callbacks += x3 =>
                                    {
                                        if (x3.TryGetResult<SingletonHashTable>(2, out var tableData))
                                        {
                                            if (tableData.TableSize > 100000 || tableData.KVS == 0x0)
                                                return;
                                            using var genericClassPtrArrayLease = MemArray<GenericClassPtrEntry>.Lease(tableData.KVS, tableData.TableSize, true, out var genericClassPtrArray);
                                            using var mapInnerLease = ScatterReadMap.Lease(out var mapInner);
                                            var r11 = mapInner.AddRound();
                                            var r22 = mapInner.AddRound();
                                            for (int iix = 0; iix < genericClassPtrArray.Count; iix++)
                                            {
                                                int ii = iix;
                                                var genericClassPtr = genericClassPtrArray[ii];
                                                if (genericClassPtr.Ptr == 0x0)
                                                    continue;
                                                r11[ii].AddEntry<MemPointer>(0, genericClassPtr.Ptr + 0x20);
                                                r11[ii].Callbacks += x11 =>
                                                {
                                                    if (foundCount == classNames.Length)
                                                    {
                                                        return;
                                                    }
                                                    if (x11.TryGetResult<MemPointer>(0, out var classPtr))
                                                    {
                                                        r22[ii].AddEntry<MonoClass>(1, classPtr);
                                                        r22[ii].Callbacks += x22 =>
                                                        {
                                                            if (x22.TryGetResult<MonoClass>(1, out var monoClass))
                                                            {
                                                                if ((monoClass.Inited & 1) != 1 ||           // !class->inited
                                                                    (monoClass.Flags & 0x100000) != 0 ||     // class->exception_type != MONO_EXCEPTION_NONE
                                                                    (monoClass.ClassKind != 3))  // class->class_kind != MONO_CLASS_GINST
                                                                {
                                                                    return;
                                                                }
                                                                if (monoClass.IsSingleton)
                                                                {
                                                                    string name = monoClass.GetSingletonName(classPtr);

                                                                    int index = Array.IndexOf(classNames, name);
                                                                    if (index != -1)
                                                                    {
                                                                        var vTable = monoClass.GetVTable(MonoRootDomain.Get());
                                                                        if (vTable == 0x0)
                                                                            return;

                                                                        ulong staticDataPtr = vTable.Value.GetStaticFieldData(vTable);
                                                                        if (staticDataPtr == 0x0)
                                                                            return;
                                                                        results[index] = staticDataPtr;
                                                                        Interlocked.Increment(ref foundCount);
                                                                    }
                                                                }
                                                            }
                                                        };
                                                    }
                                                };
                                            }
                                            mapInner.Execute();
                                        }
                                    };
                                }
                            };
                        }
                    };
                }
                mapOuter.Execute();

                return results;
            }
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public readonly struct GList
        {
            [FieldOffset(0x0)]
            public readonly ulong pData;
            [FieldOffset(0x8)]
            public readonly ulong pNext;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public readonly struct MonoRootDomain
        {
            [FieldOffset(0x94)]
            public readonly int DomainID; // domain_id
            [FieldOffset(0xA0)]
            public readonly ulong pDomainAssemblies; // domain_assemblies
            [FieldOffset(0x120)]
            public readonly ulong pJittedFunctionTable; // jit_info_table

            public readonly MonoValue<GList> GetDomainAssemblies() =>
                MonoRead<GList>(pDomainAssemblies);

            public static MonoValue<MonoRootDomain> Get()
            {
                try
                {
                    var monoModule = Memory.MonoBase;
                    ArgumentOutOfRangeException.ThrowIfZero(monoModule, nameof(monoModule));
                    return MonoRead<MonoRootDomain>(MonoReadPtr(monoModule + 0x751020));
                }
                catch
                {
                    return default;
                }
            }
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private readonly struct MonoTableInfo
        {
            [FieldOffset(0x8)]
            public readonly int nRows;

            public readonly int GetRows() => nRows & 0xFFFFFF;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public readonly struct MonoMethod
        {
            [FieldOffset(0x18)]
            public readonly ulong pName;

            public readonly string GetName() =>
                ReadName(pName, 128);
        }
        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public readonly struct MonoClassField
        {
            [FieldOffset(0x8)]
            public readonly ulong pName;
            [FieldOffset(0x18)]
            public readonly int Offset;

            public readonly string GetName() =>
                ReadName(pName, 128);
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private readonly struct MonoClassRuntimeInfo
        {
            [FieldOffset(0x0)]
            public readonly int MaxDomain;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public readonly struct MonoVTable
        {
            [FieldOffset(0x0)]
            public readonly ulong p0;
            [FieldOffset(0x30)]
            public readonly byte Flags;

            public readonly ulong GetStaticFieldData(MonoValue<MonoVTable> pThis)
            {
                if ((Flags & 4) != 0)
                    return MonoReadPtr(pThis + 0x48 + 8 * (uint)MonoRead<int>(p0 + 0x5C).Value);
                return 0x0;
            }
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public readonly struct MonoClass
        {
            [FieldOffset(0x1B)]
            public readonly byte ClassKind; // class_kind
            [FieldOffset(0x20)]
            public readonly byte Inited;
            [FieldOffset(0x28)]
            public readonly uint Flags;
            [FieldOffset(0x2A)]
            public readonly int n1;
            [FieldOffset(0xFC)]
            public readonly int n2;
            [FieldOffset(0xF0)]
            public readonly int n3;
            [FieldOffset(0x48)]
            public readonly ulong pName;
            [FieldOffset(0x50)]
            public readonly ulong pNamespaceName;
            [FieldOffset(0xD0)]
            public readonly ulong pRuntimeInfo;
            [FieldOffset(0x98)]
            public readonly ulong pFields;
            [FieldOffset(0xA0)]
            public readonly ulong pMethods;
            [FieldOffset(0xB8)]
            public readonly ulong Type;
            [FieldOffset(0x100)]
            public readonly int NumFields;

            public readonly bool IsSingleton => GetNamespaceName().Contains("Comfort.Common", StringComparison.OrdinalIgnoreCase) && GetName().Contains("Singleton", StringComparison.OrdinalIgnoreCase);

            public readonly string GetSingletonName(ulong pThis)
            {
                try
                {
                    ulong genericClassPtr = Memory.ReadPtr(pThis + 0xF0);
                    ulong genericClassContextPtr = Memory.ReadPtr(genericClassPtr + 0x8);
                    ulong argumentClassTypePtr = Memory.ReadPtr(genericClassContextPtr + 0x8);
                    ulong argumentClassPtr = Memory.ReadPtr(argumentClassTypePtr);
                    ulong monoClassNamePtr = Memory.ReadPtr(argumentClassPtr + 0x48);

                    return ReadName(monoClassNamePtr, 64);
                }
                catch { return string.Empty; }
            }

            public readonly string GetName() =>
                ReadName(pName, 128);

            public readonly string GetNamespaceName() =>
                ReadName(pNamespaceName, 128);

            public readonly int GetNumMethods()
            {
                var v2 = ClassKind - 1;
                switch (v2)
                {
                    case 0:
                    case 1:
                        return n2;

                    case 3:
                    case 5:
                        return 0;

                    case 4:
                        return n3;
                }

                return 0;
            }

            public readonly MonoValue<MonoMethod> GetMethod(int i) =>
                MonoRead<MonoMethod>(MonoReadPtr(pMethods + 0x8 * (uint)i));

            public readonly MonoValue<MonoClassField> GetField(int i) =>
                MonoRead<MonoClassField>(MonoReadPtr(pFields + (ulong)(0x20 * i)));


            public readonly MonoValue<MonoVTable> GetVTable(MonoValue<MonoRootDomain> domain)
            {
                if (domain == 0x0)
                    return default;
                var runtimeInfo = MonoRead<MonoClassRuntimeInfo>(pRuntimeInfo);
                if (runtimeInfo == 0x0)
                    return default;

                var domainID = domain.Value.DomainID;
                if (runtimeInfo.Value.MaxDomain < domainID)
                    return default;

                return MonoRead<MonoVTable>(MonoReadPtr(runtimeInfo + 8 * (uint)domainID + 8));
            }

            public readonly ulong FindMethod(string methodName)
            {
                ulong monoPtr = 0x0;

                int methodCount = GetNumMethods();
                ArgumentOutOfRangeException.ThrowIfGreaterThan(methodCount, 10000, nameof(methodCount));
                for (int i = 0; i < methodCount; i++)
                {
                    var method = GetMethod(i);

                    if (method == 0x0)
                        continue;

                    if (method.Value.GetName() == methodName)
                        monoPtr = method;
                }

                if (!monoPtr.IsValidVirtualAddress())
                {
                    throw new InvalidOperationException($"'{methodName}' Function not found / Invalid address!");
                }
                return monoPtr;
            }

            public readonly MonoClassField FindField(string field_name)
            {
                int fieldCount = NumFields;
                ArgumentOutOfRangeException.ThrowIfGreaterThan(fieldCount, 10000, nameof(fieldCount));
                for (int i = 0; i < fieldCount; i++)
                {
                    var pField = GetField(i);
                    if (pField == 0x0)
                        continue;
                    if (pField.Value.GetName() == field_name)
                    {
                        var field = MonoRead<MonoClassField>(pField);
                        ArgumentOutOfRangeException.ThrowIfZero((ulong)field);
                        return field.Value;
                    }
                }
                throw new InvalidOperationException();
            }

            public ulong GetStaticFieldData()
            {
                var vTable = GetVTable(MonoRootDomain.Get());
                ArgumentOutOfRangeException.ThrowIfZero((ulong)vTable, nameof(vTable));
                var staticFieldData = vTable.Value.GetStaticFieldData(vTable);
                ArgumentOutOfRangeException.ThrowIfZero(staticFieldData, nameof(staticFieldData));
                return staticFieldData;
            }

            public static MonoClass Find(string assemblyName, string className, out ulong addressOf)
            {
                var rootDomain = MonoRootDomain.Get();
                ArgumentOutOfRangeException.ThrowIfZero((ulong)rootDomain, nameof(rootDomain));
                var domainAssembly = MonoAssembly.Open(rootDomain.Value, assemblyName);
                var monoImage = domainAssembly.GetMonoImage();
                ArgumentOutOfRangeException.ThrowIfZero((ulong)monoImage, nameof(monoImage));
                var tableInfo = monoImage.Value.GetTableInfo(monoImage, 2);
                ArgumentOutOfRangeException.ThrowIfZero((ulong)tableInfo, nameof(tableInfo));
                int rowCount = tableInfo.Value.GetRows();
                ArgumentOutOfRangeException.ThrowIfGreaterThan(rowCount, 25000, nameof(rowCount));
                bool mainClassFound = false;
                bool findSubClass = className.Contains('+');
                for (int i = 0; i < rowCount; i++)
                {
                    var ptr = MonoRead<MonoClass>(MonoRead<MonoHashTable>(monoImage + 0x4D0).Value.Lookup((ulong)(0x02000000 | i + 1)));
                    if (ptr == 0x0)
                        continue;
                    var name = ptr.Value.GetName();
                    var ns = ptr.Value.GetNamespaceName();
                    if (ns.Length != 0)
                        name = ns + "." + name;
                    if (mainClassFound && findSubClass)
                    {
                        if (name.Contains(className.Split('+')[1]))
                        {
                            ArgumentOutOfRangeException.ThrowIfZero((ulong)ptr, nameof(ptr));
                            addressOf = ptr;
                            return ptr.Value;
                        }
                    }
                    else if (findSubClass)
                    {
                        if (name == className.Split('+')[0])
                            mainClassFound = true;
                    }
                    else if (!findSubClass && name == className)
                    {
                        ArgumentOutOfRangeException.ThrowIfZero((ulong)ptr, nameof(ptr));
                        addressOf = ptr;
                        return ptr.Value;
                    }
                }
                throw new InvalidOperationException("Cannot find class " + className);
            }
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private readonly struct MonoHashTable
        {
            [FieldOffset(0x18)]
            public readonly int Size;
            [FieldOffset(0x20)]
            public readonly ulong pData;
            [FieldOffset(0x108)]
            public readonly ulong pNextValue;
            [FieldOffset(0x58)]
            public readonly uint KeyExtract;

            public readonly ulong Lookup(ulong key)
            {
                var v4 = MonoRead<MonoHashTable>(MonoReadPtr(pData + 0x8 * (ulong)((uint)key % Size)));
                if (v4 == 0x0)
                    return default;

                while (v4.Value.KeyExtract != key)
                {
                    v4 = MonoRead<MonoHashTable>(v4.Value.pNextValue);
                    if (v4 == 0x0)
                        return default;
                }

                return v4;
            }
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private readonly struct MonoImage
        {
            [FieldOffset(0x1C)]
            public readonly int Flags;

            public readonly MonoValue<MonoTableInfo> GetTableInfo(MonoValue<MonoImage> pThis, int tableID)
            {
                if (tableID > 0x37)
                    return default;
                return MonoRead<MonoTableInfo>(pThis + 0x10 * ((uint)tableID + 0xF));
            }

            public readonly MonoValue<MonoClass> Get(MonoValue<MonoImage> pThis, int typeID)
            {
                if ((Flags & 0x20) != 0)
                    return default;
                if ((typeID & 0xFF000000) != 0x2000000)
                    return default;
                return MonoRead<MonoClass>(MonoRead<MonoHashTable>(pThis + 0x4D0).Value.Lookup((ulong)typeID));
            }
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private readonly struct MonoAssembly
        {
            [FieldOffset(0x60)]
            public readonly ulong pMonoImage;
            public readonly MonoValue<MonoImage> GetMonoImage() =>
                MonoRead<MonoImage>(pMonoImage);

            public static MonoAssembly Open(MonoRootDomain domain, string name)
            {
                var domainAssemblies = domain.GetDomainAssemblies();
                ArgumentOutOfRangeException.ThrowIfZero((ulong)domainAssemblies, nameof(domainAssemblies));

                ulong data;
                while (true)
                {
                    data = domainAssemblies.Value.pData;
                    if (data == 0x0)
                        continue;

                    var dataName = ReadWidechar(MonoReadPtr(data + 0x10), 128);
                    if (dataName == name)
                        break;
                    domainAssemblies = MonoRead<GList>(domainAssemblies.Value.pNext);
                    if (domainAssemblies == 0x0)
                        break;
                }

                var monoAssembly = MonoRead<MonoAssembly>(data);
                ArgumentOutOfRangeException.ThrowIfZero((ulong)monoAssembly, nameof(monoAssembly));
                return monoAssembly.Value;
            }
        }

        /// <summary>
        /// Functions interop struct.
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private readonly struct MonoJittedMethod
        {
            [FieldOffset(0x0)]
            public readonly ulong pMonoMethod;
            [FieldOffset(0x10)]
            public readonly ulong pJittedMethod;
        }
        #endregion
    }
}