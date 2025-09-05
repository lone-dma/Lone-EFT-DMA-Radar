namespace EftDmaRadarLite.UI.Misc
{
    public sealed partial class LoadingWindow : Window, IDisposable
    {
        public LoadingViewModel ViewModel { get; }

        public LoadingWindow()
        {
            InitializeComponent();
            DataContext = ViewModel = new LoadingViewModel(this);
            this.Show();
        }

        private bool _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, true) == false)
            {
                this.Close();
                ViewModel.Dispose();
            }
        }
    }
}
