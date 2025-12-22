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
using LoneEftDmaRadar.Web.WebRadar;

namespace LoneEftDmaRadar.UI.Panels
{
    /// <summary>
    /// Web Radar Panel for the ImGui-based Radar.
    /// </summary>
    internal static class WebRadarPanel
    {
        private static readonly RadarUIState _state = RadarUIState.Instance;
        private static string _bindAddress;
        private static string _port;
        private static string _tickRate;
        private static bool _upnpEnabled;
        private static string _password;

        static WebRadarPanel()
        {
            // Initialize from config
            _bindAddress = Program.Config.WebRadar.IP ?? "0.0.0.0";
            _port = Program.Config.WebRadar.Port ?? "55555";
            _tickRate = Program.Config.WebRadar.TickRate ?? "60";
            _upnpEnabled = Program.Config.WebRadar.UPnP;
            _password = WebRadarServer.Password;
        }

        /// <summary>
        /// Draw the web radar panel.
        /// </summary>
        public static void Draw()
        {
            ImGui.SeparatorText("Web Radar Server");

            bool uiEnabled = _state.IsWebRadarUiEnabled;
            if (!uiEnabled)
            {
                ImGui.BeginDisabled();
            }

            // Server Configuration
            ImGui.Text("Bind Address:");
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputText("##BindAddress", ref _bindAddress, 64))
            {
                Program.Config.WebRadar.IP = _bindAddress;
            }
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "(e.g., 0.0.0.0 for all interfaces)");

            ImGui.Text("Port:");
            ImGui.SetNextItemWidth(100);
            if (ImGui.InputText("##Port", ref _port, 6))
            {
                if (int.TryParse(_port, out _))
                {
                    Program.Config.WebRadar.Port = _port;
                }
            }

            ImGui.Text("Tick Rate (Hz):");
            ImGui.SetNextItemWidth(100);
            if (ImGui.InputText("##TickRate", ref _tickRate, 4))
            {
                if (int.TryParse(_tickRate, out _))
                {
                    Program.Config.WebRadar.TickRate = _tickRate;
                }
            }

            if (ImGui.Checkbox("Enable UPnP", ref _upnpEnabled))
            {
                Program.Config.WebRadar.UPnP = _upnpEnabled;
            }
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "(Automatic port forwarding)");

            ImGui.Separator();

            // Password (read-only, auto-generated)
            ImGui.Text("Session Password:");
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.2f, 0.8f, 0.2f, 1f), _password);

            if (!uiEnabled)
            {
                ImGui.EndDisabled();
            }

            ImGui.Separator();

            // Start/Stop Button
            if (ImGui.Button(_state.WebRadarStartButtonText, new Vector2(150, 30)))
            {
                if (!_state.IsWebRadarRunning)
                {
                    StartServer();
                }
            }

            // Server URL
            if (!string.IsNullOrEmpty(_state.WebRadarServerUrl))
            {
                ImGui.Separator();
                ImGui.Text("Server URL:");
                ImGui.TextWrapped(_state.WebRadarServerUrl);

                if (ImGui.Button("Copy URL"))
                {
                    CopyUrlToClipboard();
                }
            }

            ImGui.Separator();

            // Instructions
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "Instructions:");
            ImGui.BulletText("Configure the server settings above");
            ImGui.BulletText("Click 'Start' to begin the web radar server");
            ImGui.BulletText("Share the generated URL with teammates");
            ImGui.BulletText("They can view the radar in their web browser");
        }

        private static async void StartServer()
        {
            _state.IsWebRadarUiEnabled = false;
            _state.WebRadarStartButtonText = "Starting...";

            try
            {
                var tickRate = TimeSpan.FromSeconds(1) / int.Parse(_tickRate);
                string bindIP = _bindAddress.Trim();
                int port = int.Parse(_port);

                var externalIP = await WebRadarServer.GetExternalIPAsync();
                await WebRadarServer.StartAsync(bindIP, port, tickRate, _upnpEnabled);

                _state.IsWebRadarRunning = true;
                _state.WebRadarStartButtonText = "Running...";
                _state.WebRadarServerUrl = $"http://dc64dcid9fd4.cloudfront.net/?host={externalIP}&port={port}&password={_password}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR Starting Web Radar Server: {ex.Message}", "Web Radar",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _state.WebRadarStartButtonText = "Start";
                _state.IsWebRadarUiEnabled = true;
            }
        }

        private static void CopyUrlToClipboard()
        {
            try
            {
                var url = _state.WebRadarServerUrl;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    // Note: Clipboard access in non-WPF requires platform-specific handling
                    // For now, we'll use the Windows clipboard API via PInvoke
                    Clipboard.SetText(url);
                    MessageBox.Show("Web Radar URL copied to clipboard.", "Web Radar",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy URL: {ex.Message}", "Web Radar",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Simple clipboard helper for non-WPF applications.
    /// </summary>
    internal static class Clipboard
    {
        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(IntPtr hMem);

        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE = 0x0002;

        public static void SetText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (!OpenClipboard(IntPtr.Zero))
                throw new Exception("Could not open clipboard");

            try
            {
                EmptyClipboard();

                var bytes = (text.Length + 1) * 2;
                var hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes);
                if (hGlobal == IntPtr.Zero)
                    throw new Exception("Could not allocate memory");

                var target = GlobalLock(hGlobal);
                if (target == IntPtr.Zero)
                    throw new Exception("Could not lock memory");

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
                    throw new Exception("Could not set clipboard data");
            }
            finally
            {
                CloseClipboard();
            }
        }
    }
}
