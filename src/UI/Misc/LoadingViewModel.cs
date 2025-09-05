namespace EftDmaRadarLite.UI.Misc
{
    public class LoadingViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly PeriodicTimer _intervalTimer = new(TimeSpan.FromMilliseconds(233));
        private readonly LoadingWindow _parent;

        public LoadingViewModel(LoadingWindow parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        private double _progress;
        /// <summary>
        /// Progress value (0–100)
        /// </summary>
        public double Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged(nameof(Progress));
                }
            }
        }

        private string _statusText = "";
        /// <summary>
        /// Status message
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Call to update both progress bar and status text.
        /// </summary>
        public async Task UpdateProgressAsync(double percent, string status)
        {
            _parent.Dispatcher.Invoke(() =>
            {
                Progress = percent;
                StatusText = status;
            });
            await _intervalTimer.WaitForNextTickAsync();
        }

        public void Dispose()
        {
            _intervalTimer.Dispose();
        }
    }
}
