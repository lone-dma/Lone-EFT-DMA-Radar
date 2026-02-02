/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using System.ComponentModel;

namespace LoneEftDmaRadar.UI.Misc
{
    /// <summary>
    /// Provides a WPF-like MessageBox API using native Win32 P/Invoke.
    /// </summary>
    internal static partial class MessageBox
    {
        #region Overloads without owner window

        /// <summary>
        /// Displays a message box with the specified text.
        /// </summary>
        public static MessageBoxResult Show(string messageBoxText)
            => ShowCore(IntPtr.Zero, messageBoxText, string.Empty, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxOptions.None);

        /// <summary>
        /// Displays a message box with the specified text and caption.
        /// </summary>
        public static MessageBoxResult Show(string messageBoxText, string caption)
            => ShowCore(IntPtr.Zero, messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxOptions.None);

        /// <summary>
        /// Displays a message box with the specified text, caption, and button(s).
        /// </summary>
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
            => ShowCore(IntPtr.Zero, messageBoxText, caption, button, MessageBoxImage.None, MessageBoxOptions.None);

        /// <summary>
        /// Displays a message box with the specified text, caption, button(s), and icon.
        /// </summary>
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
            => ShowCore(IntPtr.Zero, messageBoxText, caption, button, icon, MessageBoxOptions.None);

        /// <summary>
        /// Displays a message box with the specified text, caption, button(s), icon, and options.
        /// </summary>
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxOptions options)
            => ShowCore(IntPtr.Zero, messageBoxText, caption, button, icon, options);

        #endregion

        #region Overloads with owner window

        /// <summary>
        /// Displays a message box in front of the specified window with the specified text.
        /// </summary>
        public static MessageBoxResult Show(IntPtr owner, string messageBoxText)
            => ShowCore(owner, messageBoxText, string.Empty, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxOptions.None);

        /// <summary>
        /// Displays a message box in front of the specified window with the specified text and caption.
        /// </summary>
        public static MessageBoxResult Show(IntPtr owner, string messageBoxText, string caption)
            => ShowCore(owner, messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxOptions.None);

        /// <summary>
        /// Displays a message box in front of the specified window with the specified text, caption, and button(s).
        /// </summary>
        public static MessageBoxResult Show(IntPtr owner, string messageBoxText, string caption, MessageBoxButton button)
            => ShowCore(owner, messageBoxText, caption, button, MessageBoxImage.None, MessageBoxOptions.None);

        /// <summary>
        /// Displays a message box in front of the specified window with the specified text, caption, button(s), and icon.
        /// </summary>
        public static MessageBoxResult Show(IntPtr owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
            => ShowCore(owner, messageBoxText, caption, button, icon, MessageBoxOptions.None);

        /// <summary>
        /// Displays a message box in front of the specified window with the specified text, caption, button(s), icon, and options.
        /// </summary>
        public static MessageBoxResult Show(IntPtr owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxOptions options)
            => ShowCore(owner, messageBoxText, caption, button, icon, options);

        #endregion

        #region Core implementation

        private static MessageBoxResult ShowCore(
            IntPtr hWnd,
            string messageBoxText,
            string caption,
            MessageBoxButton button,
            MessageBoxImage icon,
            MessageBoxOptions options)
        {
            uint flags =
                (uint)button |
                (uint)icon |
                (uint)options;

            var result = MessageBoxW(hWnd, messageBoxText, caption, flags);
            if (result == MessageBoxResult.Error)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to display message box.");
            return result;
        }

        [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        private static partial MessageBoxResult MessageBoxW(nint hWnd, string lpText, string lpCaption, uint uType);

        #endregion
    }

    /// <summary>
    /// Specifies the buttons that are displayed on a message box.
    /// </summary>
    public enum MessageBoxButton : uint
    {
        OK = 0x00000000,
        OKCancel = 0x00000001,
        AbortRetryIgnore = 0x00000002,
        YesNoCancel = 0x00000003,
        YesNo = 0x00000004,
        RetryCancel = 0x00000005,
        CancelTryContinue = 0x00000006
    }

    /// <summary>
    /// Specifies the icon that is displayed by a message box.
    /// </summary>
    public enum MessageBoxImage : uint
    {
        None = 0x00000000,
        Error = 0x00000010,
        Question = 0x00000020,
        Warning = 0x00000030,
        Information = 0x00000040
    }

    /// <summary>
    /// Specifies the result of a message box.
    /// </summary>
    public enum MessageBoxResult : int
    {
        Error = 0,
        OK = 1,
        Cancel = 2,
        Abort = 3,
        Retry = 4,
        Ignore = 5,
        Yes = 6,
        No = 7,
        TryAgain = 10,
        Continue = 11
    }

    /// <summary>
    /// Specifies special display options for a message box.
    /// </summary>
    [Flags]
    public enum MessageBoxOptions : uint
    {
        None = 0x00000000,
        DefaultDesktopOnly = 0x00020000,
        RightAlign = 0x00080000,
        RtlReading = 0x00100000,
        ServiceNotification = 0x00200000
    }
}
