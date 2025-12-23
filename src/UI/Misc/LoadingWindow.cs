/*
 * Lone EFT DMA Radar
 * Brought to you by Lone (Lone DMA)
 * 
MIT License

Copyright (c) 2025 Lone DMA

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 *
*/

using ImGuiNET;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace LoneEftDmaRadar.UI.Misc
{
    /// <summary>
    /// A simple loading window using Silk.NET and ImGui.
    /// </summary>
    internal sealed partial class LoadingWindow : IDisposable
    {
        private readonly IWindow _window;
        private GL _gl;
        private ImGuiController _imgui;

        private float _progress;
        private string _statusText = "Loading...";
        private bool _isRunning;
        private bool _disposed;

        /// <summary>
        /// Current progress value (0-100).
        /// </summary>
        public float Progress
        {
            get => _progress;
            set => _progress = Math.Clamp(value, 0f, 100f);
        }

        /// <summary>
        /// Current status text to display.
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set => _statusText = value ?? string.Empty;
        }

        public LoadingWindow()
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(650, 150);
            options.Title = "Loading...";
            options.WindowBorder = WindowBorder.Hidden;
            options.WindowState = WindowState.Normal;
            options.VSync = false;
            options.TopMost = true;
            options.ShouldSwapAutomatically = false;

            _window = Window.Create(options);
            _window.Load += OnLoad;
            _window.Closing += OnClosing;
        }

        /// <summary>
        /// Shows the loading window and initializes it.
        /// </summary>
        public void Show()
        {
            _window.Initialize();
            _isRunning = true;

            // Center the window on screen
            var monitor = _window.Monitor;
            if (monitor is not null)
            {
                var screenSize = monitor.VideoMode.Resolution ?? new Vector2D<int>(1920, 1080);
                var windowSize = _window.Size;
                _window.Position = new Vector2D<int>(
                    (screenSize.X - windowSize.X) / 2,
                    (screenSize.Y - windowSize.Y) / 2);
            }
        }

        /// <summary>
        /// Process window events and render a frame. Call this periodically to keep the window responsive.
        /// </summary>
        public void DoEvents()
        {
            if (!_isRunning || _disposed || _window.IsClosing)
                return;

            _window.DoEvents();

            if (_window.IsClosing)
                return;

            RenderFrame();
        }

        private void RenderFrame()
        {
            if (_imgui is null || _gl is null)
                return;

            _gl.ClearColor(0.15f, 0.15f, 0.15f, 1.0f);
            _gl.Clear(ClearBufferMask.ColorBufferBit);

            _imgui.Update(1f / 60f);

            DrawLoadingUI();

            _imgui.Render();

            _window.SwapBuffers();
        }

        /// <summary>
        /// Update progress and status text.
        /// </summary>
        public void UpdateProgress(float percent, string status)
        {
            Progress = percent;
            StatusText = status;
        }

        /// <summary>
        /// Close and dispose the window.
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _isRunning = false;

            // Dispose ImGui controller first
            if (_imgui is not null)
            {
                _imgui.Dispose();
                _imgui = null;
            }

            // Destroy the ImGui context we created
            var ctx = ImGui.GetCurrentContext();
            if (ctx != IntPtr.Zero)
            {
                ImGui.DestroyContext(ctx);
            }

            // Close and dispose the window
            if (!_window.IsClosing)
            {
                _window.Close();
            }

            _window.Reset();
            _window.Dispose();
        }

        private void OnLoad()
        {
            _gl = GL.GetApi(_window);

            // Apply dark mode and window icon (Windows only)
            if (_window.Native?.Win32 is { } win32)
            {
                EnableDarkMode(win32.Hwnd);
            }

            ImGui.CreateContext();
            _imgui = new ImGuiController(
                _gl,
                _window,
                _window.Size.X,
                _window.Size.Y
            );

            ConfigureStyle();
        }

        private void ConfigureStyle()
        {
            var style = ImGui.GetStyle();
            style.WindowRounding = 0f;
            style.FrameRounding = 3.0f;

            var colors = style.Colors;
            colors[(int)ImGuiCol.WindowBg] = new Vector4(0.15f, 0.15f, 0.15f, 1.0f);
            colors[(int)ImGuiCol.Text] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0.2f, 0.2f, 0.2f, 1.0f);
            colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.2f, 0.7f, 0.2f, 1.0f);
        }

        private void DrawLoadingUI()
        {
            var io = ImGui.GetIO();
            var windowSize = io.DisplaySize;

            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(windowSize);

            var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
                        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings;

            if (ImGui.Begin("LoadingContent", flags))
            {
                // Center content vertically
                float contentHeight = 60f;
                float startY = (windowSize.Y - contentHeight) / 2f;
                ImGui.SetCursorPosY(startY);

                // Status text - centered
                float textWidth = ImGui.CalcTextSize(_statusText).X;
                ImGui.SetCursorPosX((windowSize.X - textWidth) / 2f);
                ImGui.TextUnformatted(_statusText);

                ImGui.Spacing();
                ImGui.Spacing();

                // Progress bar - centered with padding, no overlay text
                float progressBarWidth = windowSize.X - 80f;
                float progressBarX = 40f;
                float progressBarHeight = 30f;
                ImGui.SetCursorPosX(progressBarX);

                // Draw progress bar without text overlay (empty string)
                ImGui.ProgressBar(_progress / 100f, new Vector2(progressBarWidth, progressBarHeight), "");

                // Draw centered percentage text on top of progress bar
                var percentText = $"{_progress:F0}%";
                var percentTextSize = ImGui.CalcTextSize(percentText);
                var progressBarPos = ImGui.GetItemRectMin();
                var progressBarSize = ImGui.GetItemRectSize();

                // Calculate centered position for text
                float textX = progressBarPos.X + (progressBarSize.X - percentTextSize.X) / 2f;
                float textY = progressBarPos.Y + (progressBarSize.Y - percentTextSize.Y) / 2f;

                // Draw the text at the calculated position
                ImGui.GetWindowDrawList().AddText(
                    new Vector2(textX, textY),
                    ImGui.GetColorU32(ImGuiCol.Text),
                    percentText);

                ImGui.End();
            }
        }

        private void OnClosing()
        {
            _isRunning = false;
        }

        #region Win32 Interop

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [LibraryImport("dwmapi.dll")]
        private static partial int DwmSetWindowAttribute(nint hwnd, int attr, ref int attrValue, int attrSize);

        [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static partial nint GetModuleHandleW(string lpModuleName);

        private const uint IMAGE_ICON = 1;
        private const uint LR_DEFAULTCOLOR = 0x00000000;
        private const uint WM_SETICON = 0x0080;
        private const nint ICON_SMALL = 0;
        private const nint ICON_BIG = 1;
        private const int IDI_APPLICATION = 32512;

        private static void EnableDarkMode(nint hwnd)
        {
            int useImmersiveDarkMode = 1;
            _ = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int));
        }

        #endregion
    }
}
