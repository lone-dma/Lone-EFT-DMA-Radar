namespace eft_dma_radar.Misc
{
    /// <summary>
    /// Type Placeholder for a Unicode (UTF-16) String.
    /// Can be implicitly casted to a string.
    /// </summary>
    public sealed class UnicodeString
    {
        public static implicit operator string(UnicodeString x) => x?._value;
        public static implicit operator UnicodeString(string x) => new(x);
        private readonly string _value;

        private UnicodeString(string value)
        {
            _value = value;
        }
    }
}
