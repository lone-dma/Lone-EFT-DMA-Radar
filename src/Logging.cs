namespace LoneEftDmaRadar
{
    internal static partial class Logging
    {
        private static bool _useConsole;

        /// <summary>
        /// <see langword="true"/> if Console Logging is enabled via -console startup parameter.
        /// </summary>
        public static bool UseConsole => _useConsole;

        [ModuleInitializer]
        internal static void ModuleInit()
        {
            var args = Environment.GetCommandLineArgs();
            _useConsole = args?.Any(arg => arg.Equals("-console", StringComparison.OrdinalIgnoreCase)) ?? false;
            if (_useConsole)
            {
                AllocConsole();

                // Redirect native C runtime stdout/stderr to the new console
                nint stdoutFile = default, stderrFile = default;
                _ = freopen_s(ref stdoutFile, "CONOUT$", "w", __acrt_iob_func(1)); // stdout
                _ = freopen_s(ref stderrFile, "CONOUT$", "w", __acrt_iob_func(2)); // stderr

                // Also redirect using SetStdHandle for Win32 API calls
                SetStdHandle(STD_OUTPUT_HANDLE, CreateFileW(
                    "CONOUT$",
                    GENERIC_WRITE,
                    FILE_SHARE_WRITE,
                    nint.Zero,
                    OPEN_EXISTING,
                    0,
                    nint.Zero));

                SetStdHandle(STD_ERROR_HANDLE, CreateFileW(
                    "CONOUT$",
                    GENERIC_WRITE,
                    FILE_SHARE_WRITE,
                    nint.Zero,
                    OPEN_EXISTING,
                    0,
                    nint.Zero));

                // Reopen .NET console streams
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
                Console.SetIn(new StreamReader(Console.OpenStandardInput()));
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

        private const int STD_OUTPUT_HANDLE = -11;
        private const int STD_ERROR_HANDLE = -12;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool AllocConsole();

        [LibraryImport("kernel32.dll")]
        private static partial nint GetStdHandle(int nStdHandle);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetStdHandle(int nStdHandle, nint hHandle);

        [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static partial nint CreateFileW(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            nint lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            nint hTemplateFile);

        [LibraryImport("ucrtbase.dll", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int freopen_s(ref nint pFile, string filename, string mode, nint stream);

        // Returns pointer to C runtime FILE* streams (0=stdin, 1=stdout, 2=stderr)
        [LibraryImport("ucrtbase.dll")]
        private static partial nint __acrt_iob_func(int index);
    }
}
