/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
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
        // Panel-local state (moved from RadarUIState)
        private static string _bindAddress;
        private static string _port;
        private static string _tickRate;
        private static bool _upnpEnabled;
        private static readonly string _password;
        private static bool _isRunning;
        private static string _startButtonText = "Start";
        private static string _serverUrl = string.Empty;
        private static bool _uiEnabled = true;

        private static EftDmaConfig Config { get; } = Program.Config;

        static WebRadarPanel()
        {
            // Initialize from config
            _bindAddress = Config.WebRadar.IP ?? "0.0.0.0";
            _port = Config.WebRadar.Port ?? "55555";
            _tickRate = Config.WebRadar.TickRate ?? "60";
            _upnpEnabled = Config.WebRadar.UPnP;
            _password = WebRadarServer.Password;
        }

        /// <summary>
        /// Draw the web radar panel.
        /// </summary>
        public static void Draw()
        {
            ImGui.SeparatorText("Web Radar Server");

            if (!_uiEnabled)
            {
                ImGui.BeginDisabled();
            }

            // Server Configuration
            ImGui.Text("Bind Address:");
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputText("##BindAddress", ref _bindAddress, 64))
            {
                Config.WebRadar.IP = _bindAddress;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("IP address to bind the server to (0.0.0.0 for all interfaces)");
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "(e.g., 0.0.0.0 for all interfaces)");

            ImGui.Text("Port:");
            ImGui.SetNextItemWidth(100);
            if (ImGui.InputText("##Port", ref _port, 6))
            {
                if (int.TryParse(_port, out _))
                {
                    Config.WebRadar.Port = _port;
                }
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Port number for the web radar server");

            ImGui.Text("Tick Rate (Hz):");
            ImGui.SetNextItemWidth(100);
            if (ImGui.InputText("##TickRate", ref _tickRate, 4))
            {
                if (int.TryParse(_tickRate, out _))
                {
                    Config.WebRadar.TickRate = _tickRate;
                }
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Update frequency in Hz (higher = more responsive, more bandwidth)");

            if (ImGui.Checkbox("Enable UPnP", ref _upnpEnabled))
            {
                Config.WebRadar.UPnP = _upnpEnabled;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Automatically configure port forwarding on your router");
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "(Automatic port forwarding)");

            ImGui.Separator();

            // Password (read-only, auto-generated)
            ImGui.Text("Session Password:");
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.2f, 0.8f, 0.2f, 1f), _password);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Auto-generated password for this session");

            if (!_uiEnabled)
            {
                ImGui.EndDisabled();
            }

            ImGui.Separator();

            // Start/Stop Button
            if (ImGui.Button(_startButtonText, new Vector2(150, 30)))
            {
                if (!_isRunning)
                {
                    StartServer();
                }
            }
            if (ImGui.IsItemHovered() && !_isRunning)
                ImGui.SetTooltip("Start the web radar server");

            // Server URL
            if (!string.IsNullOrEmpty(_serverUrl))
            {
                ImGui.Separator();
                ImGui.Text("Server URL:");
                ImGui.TextWrapped(_serverUrl);

                if (ImGui.Button("Copy URL"))
                {
                    CopyUrlToClipboard();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Copy the URL to share with teammates");
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
            _uiEnabled = false;
            _startButtonText = "Starting...";

            try
            {
                var tickRate = TimeSpan.FromSeconds(1) / int.Parse(_tickRate);
                string bindIP = _bindAddress.Trim();
                int port = int.Parse(_port);

                var externalIP = await WebRadarServer.GetExternalIPAsync();
                await WebRadarServer.StartAsync(bindIP, port, tickRate, _upnpEnabled);

                _isRunning = true;
                _startButtonText = "Running...";
                _serverUrl = $"https://webradar.lone-dma.org/?host={externalIP}&port={port}&password={_password}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(RadarWindow.Handle, $"ERROR Starting Web Radar Server: {ex.Message}", "Web Radar",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _startButtonText = "Start";
                _uiEnabled = true;
            }
        }

        private static void CopyUrlToClipboard()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_serverUrl))
                {
                    Clipboard.SetText(_serverUrl);
                    MessageBox.Show(RadarWindow.Handle, "Web Radar URL copied to clipboard.", "Web Radar",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(RadarWindow.Handle, $"Failed to copy URL: {ex.Message}", "Web Radar",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

