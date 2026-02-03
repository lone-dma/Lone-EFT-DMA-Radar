using eft_dma_radar.UI.Radar;
using eft_dma_radar.UI.Radar.ViewModels;
using eft_dma_radar.UI.Radar.Views;
using System.Windows.Input;
using System.Windows.Interop;
using static SDK.Offsets;

namespace eft_dma_radar
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }
        public MainWindowViewModel ViewModel { get; }
        public MainWindow()
        {
            InitializeComponent();
            DataContext = ViewModel = new MainWindowViewModel(this);
            this.Width = App.Config.UI.WindowSize.Width;
            this.Height = App.Config.UI.WindowSize.Height;
            if (App.Config.UI.WindowMaximized)
                this.WindowState = WindowState.Maximized;
            else
                this.WindowState = WindowState.Normal;
            Instance = this;
        }

        /// <summary>
        /// Make sure the program really closes.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {
            try
            {
                App.Config.UI.WindowSize = new Size(this.Width, this.Height);
                App.Config.UI.WindowMaximized = this.WindowState == WindowState.Maximized;
                if (Radar?.ViewModel?.ESPWidget is EspWidget espWidget)
                {
                    App.Config.EspWidget.Location = espWidget.ClientRectangle;
                    App.Config.EspWidget.Minimized = espWidget.Minimized;
                }
                if (Radar?.ViewModel?.InfoWidget is PlayerInfoWidget infoWidget)
                {
                    App.Config.InfoWidget.Location = infoWidget.Rectangle;
                    App.Config.InfoWidget.Minimized = infoWidget.Minimized;
                }

                Memory.Dispose(); // Close FPGA
                Instance = null;
            }
            finally
            {
                base.OnClosing(e);
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            try
            {
                if (e.Key is Key.F1)
                {
                    Radar?.ViewModel?.ZoomIn(5);
                }
                else if (e.Key is Key.F2)
                {
                    Radar?.ViewModel?.ZoomOut(5);
                }
                else if (e.Key is Key.F3 && Settings?.ViewModel is SettingsViewModel vm)
                {
                    vm.ShowLoot = !vm.ShowLoot; // Toggle loot
                }
                else if (e.Key is Key.F11)
                {
                    var toFullscreen = this.WindowStyle is WindowStyle.SingleBorderWindow;
                    ViewModel.ToggleFullscreen(toFullscreen);
                }
            }
            finally
            {
                base.OnPreviewKeyDown(e);
            }
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            const double wheelDelta = 120d; // Standard mouse wheel delta value
            try
            {
                if (e.Delta > 0) // mouse wheel up (zoom in)
                {
                    int amt = (int)((e.Delta / wheelDelta) * 5d); // Calculate zoom amount based on number of deltas
                    Radar?.ViewModel?.ZoomIn(amt);
                }
                else if (e.Delta < 0) // mouse wheel down (zoom out)
                {
                    int amt = (int)((e.Delta / -wheelDelta) * 5d); // Calculate zoom amount based on number of deltas
                    Radar?.ViewModel?.ZoomOut(amt);
                }
            }
            finally
            {
                base.OnPreviewMouseWheel(e);
            }
        }
    }
}