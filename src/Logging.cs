namespace LoneEftDmaRadar
{
    internal static partial class Logging
    {
        private static bool _useConsole;

        [ModuleInitializer]
        internal static void ModuleInit()
        {
            var args = Environment.GetCommandLineArgs();
            _useConsole = args?.Any(arg => arg.Equals("-console", StringComparison.OrdinalIgnoreCase)) ?? false;
            if (_useConsole)
            {
                AllocConsole();
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            }
        }

        /// <summary>
        /// Writes the provided value to the Log followed by a new line.
        /// </summary>
        /// <param name="value">Value to be written to logging output.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLine(object value)
        {
            if (_useConsole)
            {
                Console.WriteLine(value);
            }
#if DEBUG
            else
            {
                Debug.WriteLine(value);
            }
#endif
        }

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool AllocConsole();
    }
}
