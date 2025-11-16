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

using LoneEftDmaRadar.UI.Radar.ViewModels;
using LoneEftDmaRadar.UI.Skia;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace LoneEftDmaRadar
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        /// <summary>
        /// Global Singleton instance of the MainWindow.
        /// </summary>
        [MaybeNull]
        public static MainWindow Instance { get; private set; }

        /// <summary>
        /// ViewModel for the MainWindow.
        /// </summary>
        public MainWindowViewModel ViewModel { get; }

        public MainWindow()
        {
            if (Instance is not null)
                throw new InvalidOperationException("MainWindow instance already exists. Only one instance is allowed.");
            InitializeComponent();
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
                if (Radar?.ViewModel?.InfoWidget is PlayerInfoWidget infoWidget)
                {
                    App.Config.InfoWidget.Location = infoWidget.Rectangle;
                    App.Config.InfoWidget.Minimized = infoWidget.Minimized;
                }

                Memory.Dispose(); // Close FPGA
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
                if (!Radar?.IsVisible ?? false)
                    return; // Ignore if radar is not visible
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
                if (!Radar?.IsVisible ?? false)
                    return; // Ignore if radar is not visible
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