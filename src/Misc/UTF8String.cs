namespace eft_dma_radar.Misc
{
    /// <summary>
    /// Type Placeholder for a UTF-8 String.
    /// Can be implicitly casted to a string.
    /// </summary>
    public sealed class UTF8String
    {
        public static implicit operator string(UTF8String x) => x?._value;
        public static implicit operator UTF8String(string x) => new(x);
        private readonly string _value;

        private UTF8String(string value)
        {
            _value = value;
        }
    }
}
