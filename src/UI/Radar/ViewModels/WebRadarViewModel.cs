using eft_dma_radar.UI.Misc;
using eft_dma_radar.WebRadar;
using System.Windows.Input;

namespace eft_dma_radar.UI.Radar.ViewModels
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
                var tickRate = TimeSpan.FromMilliseconds(1000d / int.Parse(TickRate));
                string bindIP = BindAddress.Trim();
                int port = int.Parse(Port);
                var externalIP = await WebRadarServer.GetExternalIPAsync();
                await WebRadarServer.StartAsync(bindIP, port, tickRate, UpnpEnabled);
                StartButtonText = "Running...";
                ServerUrl = $"http://dc64dcid9fd4.cloudfront.net/?host={externalIP}&port={port}&password={Password}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR Starting Web Radar Server: {ex.Message}", App.Name, MessageBoxButton.OK, MessageBoxImage.Error);
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
