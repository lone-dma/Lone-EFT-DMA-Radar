using System.ComponentModel;

namespace LoneEftDmaRadar.UI.Misc
{

    /// <summary>
    /// Simple clipboard helper for non-WPF applications.
    /// </summary>
    internal static partial class Clipboard
    {
        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool OpenClipboard(IntPtr hWndNewOwner);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CloseClipboard();

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EmptyClipboard();

        [LibraryImport("user32.dll", SetLastError = true)]
        private static partial IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [LibraryImport("kernel32.dll")]
        private static partial IntPtr GlobalFree(IntPtr hMem);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial IntPtr GlobalLock(IntPtr hMem);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GlobalUnlock(IntPtr hMem);

        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE = 0x0002;
        private const uint GMEM_ZEROINIT = 0x0040;

        public static unsafe void SetText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (!OpenClipboard(IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not open clipboard");

            IntPtr hGlobal = IntPtr.Zero;

            try
            {
                EmptyClipboard();

                int charCount = text.Length + 1;
                int byteCount = charCount * sizeof(char);

                hGlobal = GlobalAlloc(GMEM_MOVEABLE | GMEM_ZEROINIT, (nuint)byteCount);
                if (hGlobal == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "GlobalAlloc failed");

                char* target = (char*)GlobalLock(hGlobal);
                if (target == null)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "GlobalLock failed");

                try
                {
                    var span = new Span<char>(target, charCount);
                    text.AsSpan().CopyTo(span);
                    span[^1] = '\0';
                }
                finally
                {
                    GlobalUnlock(hGlobal);
                }

                if (SetClipboardData(CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "SetClipboardData failed");

                // Clipboard owns it now
                hGlobal = IntPtr.Zero;
            }
            finally
            {
                if (hGlobal != IntPtr.Zero)
                    GlobalFree(hGlobal); // only if not handed to clipboard

                CloseClipboard();
            }
        }
    }
}
