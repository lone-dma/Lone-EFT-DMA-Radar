namespace SDK
{
    public readonly struct IL2CPPOffsets
    {
        // mov rax, cs:qword_59759D8
        // lea     r14, [rax+rsi * 8]
        // mov rdi, [r14]
        public const string TypeInfoDefinitionTableSig = "48 8B 05 ? ? ? ? 4C 8D 34 F0 49 8B 3E";
        public const uint TypeInfoDefinitionTable = 0x598BAD8;
    }
}