namespace LoneEftDmaRadar.UI.Misc
{

    /// <summary>
    /// Simple clipboard helper for non-WPF applications.
    /// </summary>
    internal static partial class Clipboard
    {
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool OpenClipboard(IntPtr hWndNewOwner);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CloseClipboard();

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EmptyClipboard();

        [LibraryImport("user32.dll")]
        private static partial IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [LibraryImport("kernel32.dll")]
        private static partial IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [LibraryImport("kernel32.dll")]
        private static partial IntPtr GlobalLock(IntPtr hMem);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GlobalUnlock(IntPtr hMem);

        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE = 0x0002;

        public static void SetText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (!OpenClipboard(IntPtr.Zero))
                throw new InvalidOperationException("Could not open clipboard");

            try
            {
                EmptyClipboard();

                var bytes = (text.Length + 1) * 2;
                var hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes);
                if (hGlobal == IntPtr.Zero)
                    throw new InvalidOperationException("Could not allocate memory");

                var target = GlobalLock(hGlobal);
                if (target == IntPtr.Zero)
                    throw new InvalidOperationException("Could not lock memory");

                try
                {
                    Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                    Marshal.WriteInt16(target, text.Length * 2, 0); // Null terminator
                }
                finally
                {
                    GlobalUnlock(hGlobal);
                }

                if (SetClipboardData(CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
                    throw new InvalidOperationException("Could not set clipboard data");
            }
            finally
            {
                CloseClipboard();
            }
        }
    }
}
