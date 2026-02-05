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
        private static string _startButtonText = "启动";
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
            ImGui.SeparatorText("网络雷达服务器");

            if (!_uiEnabled)
            {
                ImGui.BeginDisabled();
            }

            // Server Configuration
            ImGui.Text("绑定地址:");
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputText("##BindAddress", ref _bindAddress, 64))
            {
                Config.WebRadar.IP = _bindAddress;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("服务器绑定的IP地址 (0.0.0.0 表示所有接口)");
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "(例如: 0.0.0.0 表示所有接口)");

            ImGui.Text("端口:");
            ImGui.SetNextItemWidth(100);
            if (ImGui.InputText("##Port", ref _port, 6))
            {
                if (int.TryParse(_port, out _))
                {
                    Config.WebRadar.Port = _port;
                }
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("网络雷达服务器的端口号");

            ImGui.Text("刷新率 (Hz):");
            ImGui.SetNextItemWidth(100);
            if (ImGui.InputText("##TickRate", ref _tickRate, 4))
            {
                if (int.TryParse(_tickRate, out _))
                {
                    Config.WebRadar.TickRate = _tickRate;
                }
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("更新频率 (越高越流畅，但带宽占用更多)");

            if (ImGui.Checkbox("启用 UPnP", ref _upnpEnabled))
            {
                Config.WebRadar.UPnP = _upnpEnabled;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("自动配置路由器的端口转发");
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "(自动端口转发)");

            ImGui.Separator();

            // Password (read-only, auto-generated)
            ImGui.Text("会话密码:");
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.2f, 0.8f, 0.2f, 1f), _password);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("自动生成的会话密码");

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
                ImGui.SetTooltip("启动网络雷达服务器");

            // Server URL
            if (!string.IsNullOrEmpty(_serverUrl))
            {
                ImGui.Separator();
                ImGui.Text("服务器地址:");
                ImGui.TextWrapped(_serverUrl);

                if (ImGui.Button("复制地址"))
                {
                    CopyUrlToClipboard();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("复制地址以分享给队友");
            }

            ImGui.Separator();

            // Instructions
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "使用说明:");
            ImGui.BulletText("在上方配置服务器设置");
            ImGui.BulletText("点击'启动'开始网络雷达服务器");
            ImGui.BulletText("将生成的地址分享给队友");
            ImGui.BulletText("他们可以在浏览器中查看雷达");
        }

        private static async void StartServer()
        {
            _uiEnabled = false;
            _startButtonText = "启动中...";

            try
            {
                var tickRate = TimeSpan.FromSeconds(1) / int.Parse(_tickRate);
                string bindIP = _bindAddress.Trim();
                int port = int.Parse(_port);

                var externalIP = await WebRadarServer.GetExternalIPAsync();
                await WebRadarServer.StartAsync(bindIP, port, tickRate, _upnpEnabled);

                _isRunning = true;
                _startButtonText = "运行中...";
                _serverUrl = $"https://webradar.lone-dma.org/?host={externalIP}&port={port}&password={_password}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(RadarWindow.Handle, $"启动网络雷达服务器出错: {ex.Message}", "网络雷达",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _startButtonText = "启动";
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
                    MessageBox.Show(RadarWindow.Handle, "网络雷达地址已复制到剪贴板。", "网络雷达",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(RadarWindow.Handle, $"复制地址失败: {ex.Message}", "网络雷达",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

