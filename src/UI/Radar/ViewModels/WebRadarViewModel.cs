/*
 * EFT DMA Radar Lite
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

using EftDmaRadarLite.UI.Misc;
using EftDmaRadarLite.WebRadar;
using System.Windows.Input;

namespace EftDmaRadarLite.UI.Radar.ViewModels
{
    public sealed class WebRadarViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public WebRadarViewModel()
        {
            StartServerCommand = new SimpleCommand(OnStartServer);
            CopyUrlCommand = new SimpleCommand(OnCopyUrl);
        }

        #region Commands

        public ICommand StartServerCommand { get; }
        public ICommand CopyUrlCommand { get; }

        private async void OnStartServer()
        {
            UiEnabled = false;
            StartButtonText = "Starting...";
            try
            {
                var tickRate = TimeSpan.FromSeconds(1) / int.Parse(TickRate);
                string bindIP = BindAddress.Trim();
                int port = int.Parse(Port);
                var externalIP = await WebRadarServer.GetExternalIPAsync();
                await WebRadarServer.StartAsync(bindIP, port, tickRate, UpnpEnabled);
                StartButtonText = "Running...";
                ServerUrl = $"http://dc64dcid9fd4.cloudfront.net/?host={externalIP}&port={port}&password={Password}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR Starting Web Radar Server: {ex.Message}", "Web Radar", MessageBoxButton.OK, MessageBoxImage.Error);
                StartButtonText = "Start";
                UiEnabled = true;
            }
        }

        private void OnCopyUrl()
        {
            try
            {
                var url = ServerUrl;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    Clipboard.SetText(url);
                    MessageBox.Show("Web Radar URL copied to clipboard.", "Web Radar", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy URL: {ex.Message}", "Web Radar", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Bindable Properties

        private bool _uiEnabled = true;
        public bool UiEnabled
        {
            get => _uiEnabled;
            set
            {
                if (_uiEnabled != value)
                {
                    _uiEnabled = value;
                    OnPropertyChanged(nameof(UiEnabled));
                }
            }
        }

        private string _startButtonText = "Start";
        public string StartButtonText
        {
            get => _startButtonText;
            set
            {
                if (!string.Equals(_startButtonText, value))
                {
                    _startButtonText = value ?? string.Empty;
                    OnPropertyChanged(nameof(StartButtonText));
                }
            }
        }

        public bool UpnpEnabled
        {
            get => App.Config.WebRadar.UPnP;
            set
            {
                if (App.Config.WebRadar.UPnP != value)
                {
                    App.Config.WebRadar.UPnP = value;
                    OnPropertyChanged(nameof(UpnpEnabled));
                }
            }
        }

        public string BindAddress
        {
            get => App.Config.WebRadar.IP;
            set
            {
                if (!string.Equals(App.Config.WebRadar.IP, value, StringComparison.OrdinalIgnoreCase))
                {
                    App.Config.WebRadar.IP = value ?? string.Empty;
                    OnPropertyChanged(nameof(BindAddress));
                }
            }
        }

        public string Port
        {
            get => App.Config.WebRadar.Port;
            set
            {
                if (!string.Equals(App.Config.WebRadar.Port, value, StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(value, out _))
                {
                    App.Config.WebRadar.Port = value ?? string.Empty;
                    OnPropertyChanged(nameof(Port));
                }
            }
        }

        public string TickRate
        {
            get => App.Config.WebRadar.TickRate;
            set
            {
                if (!string.Equals(App.Config.WebRadar.TickRate, value, StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(value, out _))
                {
                    App.Config.WebRadar.TickRate = value ?? string.Empty;
                    OnPropertyChanged(nameof(TickRate));
                }
            }
        }

        public string Password => WebRadarServer.Password; // always generates a new one on open

        private string _serverUrl;
        public string ServerUrl
        {
            get => _serverUrl;
            set
            {
                if (!string.Equals(_serverUrl, value))
                {
                    _serverUrl = value ?? string.Empty;
                    OnPropertyChanged(nameof(ServerUrl));
                }
            }
        }

        #endregion
    }
}
