﻿using eft_dma_radar.UI.Radar;
using eft_dma_radar.UI.Radar.ViewModels;
using System.Windows.Input;

namespace eft_dma_radar
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        /// <summary>
        /// Global Singleton instance of the MainWindow.
        /// </summary>
        public static MainWindow Instance { get; private set; }

        private readonly CancellationTokenSource _cts;
        /// <summary>
        /// Will be cancelled when the MainWindow is closing down.
        /// </summary>
        public CancellationToken CancellationToken { get; }
        /// <summary>
        /// ViewModel for the MainWindow.
        /// </summary>
        public MainWindowViewModel ViewModel { get; }

        public MainWindow()
        {
            if (Instance is not null)
                throw new InvalidOperationException("MainWindow instance already exists. Only one instance is allowed.");
            InitializeComponent();
            _cts = new();
            this.CancellationToken = _cts.Token;
            this.Width = App.Config.UI.WindowSize.Width;
            this.Height = App.Config.UI.WindowSize.Height;
            if (App.Config.UI.WindowMaximized)
                this.WindowState = WindowState.Maximized;
            else
                this.WindowState = WindowState.Normal;
            DataContext = ViewModel = new MainWindowViewModel(this);
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
                _cts.Cancel(); // Cancel any ongoing GUI operations
                _cts.Dispose();
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