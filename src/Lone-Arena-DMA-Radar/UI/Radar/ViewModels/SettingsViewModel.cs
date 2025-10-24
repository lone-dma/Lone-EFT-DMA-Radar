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

using LoneArenaDmaRadar.UI.ColorPicker;
using LoneArenaDmaRadar.UI.Hotkeys;
using LoneArenaDmaRadar.UI.Misc;
using LoneArenaDmaRadar.UI.Radar.Views;
using LoneArenaDmaRadar.UI.Skia;
using System.Windows.Input;

namespace LoneArenaDmaRadar.UI.Radar.ViewModels
{
    public sealed class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly SettingsTab _parent;
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public ICommand AboutUrlCommand { get; }

        public SettingsViewModel(SettingsTab parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            AboutUrlCommand = new SimpleCommand(OnAboutUrl);
            RestartRadarCommand = new SimpleCommand(OnRestartRadar);
            OpenHotkeyManagerCommand = new SimpleCommand(OnOpenHotkeyManager);
            OpenColorPickerCommand = new SimpleCommand(OnOpenColorPicker);
            BackupConfigCommand = new SimpleCommand(OnBackupConfig);
            SaveConfigCommand = new SimpleCommand(OnSaveConfig);
            SetScaleValues(UIScale);
        }

        private void OnAboutUrl()
        {
            const string url = "https://lone-dma.org/";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        #region General Settings

        public ICommand RestartRadarCommand { get; }
        private void OnRestartRadar()
        {
            Memory.RestartRadar = true;
        }

        private bool _hotkeyManagerIsEnabled = true;
        public bool HotkeyManagerIsEnabled
        {
            get => _hotkeyManagerIsEnabled;
            set
            {
                if (_hotkeyManagerIsEnabled != value)
                {
                    _hotkeyManagerIsEnabled = value;
                    OnPropertyChanged(nameof(HotkeyManagerIsEnabled));
                }
            }
        }
        public ICommand OpenHotkeyManagerCommand { get; }
        private void OnOpenHotkeyManager()
        {
            HotkeyManagerIsEnabled = false;
            try
            {
                var wnd = new HotkeyManagerWindow()
                {
                    Owner = MainWindow.Instance
                };
                wnd.ShowDialog();
            }
            finally
            {
                HotkeyManagerIsEnabled = true;
            }
        }

        private bool _colorPickerIsEnabled = true;
        public bool ColorPickerIsEnabled
        {
            get => _colorPickerIsEnabled;
            set
            {
                if (_colorPickerIsEnabled != value)
                {
                    _colorPickerIsEnabled = value;
                    OnPropertyChanged(nameof(ColorPickerIsEnabled));
                }
            }
        }
        public ICommand OpenColorPickerCommand { get; }
        private void OnOpenColorPicker()
        {
            ColorPickerIsEnabled = false;
            try
            {
                var wnd = new ColorPickerWindow()
                {
                    Owner = MainWindow.Instance
                };
                wnd.ShowDialog();
            }
            finally
            {
                ColorPickerIsEnabled = true;
            }
        }

        public ICommand BackupConfigCommand { get; }
        private async void OnBackupConfig()
        {
            try
            {
                var backupFile = ArenaDmaConfig.Filename + ".bak";
                if (File.Exists(backupFile) &&
                    MessageBox.Show(MainWindow.Instance, "Overwrite backup?", "Backup Config", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                await File.WriteAllTextAsync(backupFile, JsonSerializer.Serialize(App.Config, new JsonSerializerOptions { WriteIndented = true }));
                MessageBox.Show(MainWindow.Instance, $"Backed up to {backupFile}", "Backup Config");
            }
            catch (Exception ex)
            {
                MessageBox.Show(MainWindow.Instance, $"Error: {ex.Message}", "Backup Config", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public ICommand SaveConfigCommand { get; }
        private async void OnSaveConfig()
        {
            try
            {
                await App.Config.SaveAsync();
                MessageBox.Show(MainWindow.Instance, $"Config saved to {App.ConfigPath.FullName}", "Save Config");
            }
            catch (Exception ex)
            {
                MessageBox.Show(MainWindow.Instance, $"Error: {ex.Message}", "Save Config", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public int AimlineLength
        {
            get => App.Config.UI.AimLineLength;
            set
            {
                if (App.Config.UI.AimLineLength != value)
                {
                    App.Config.UI.AimLineLength = value;
                    OnPropertyChanged(nameof(AimlineLength));
                }
            }
        }

        public float UIScale
        {
            get => App.Config.UI.UIScale;
            set
            {
                if (App.Config.UI.UIScale == value)
                    return;
                App.Config.UI.UIScale = value;
                SetScaleValues(value);
                OnPropertyChanged(nameof(UIScale));
            }
        }

        private static void SetScaleValues(float newScale)
        {
            #region UpdatePaints

            /// Outlines
            SKPaints.TextOutline.StrokeWidth = 2f * newScale;
            // Shape Outline is computed before usage due to different stroke widths

            SKPaints.PaintLocalPlayer.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintTeammate.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintPlayer.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintStreamer.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintBot.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintFocused.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintDeathMarker.StrokeWidth = 3 * newScale;
            SKPaints.PaintTransparentBacker.StrokeWidth = 1 * newScale;
            SKPaints.PaintExplosives.StrokeWidth = 3 * newScale;
            // Fonts
            SKFonts.UIRegular.Size = 12f * newScale;
            SKFonts.UILarge.Size = 48f * newScale;
            SKFonts.EspWidgetFont.Size = 9f * newScale;
            SKFonts.InfoWidgetFont.Size = 12f * newScale;

            #endregion
        }

        private bool _showMapSetupHelper;
        public bool ShowMapSetupHelper
        {
            get => _showMapSetupHelper;
            set
            {
                if (_showMapSetupHelper != value)
                {
                    _showMapSetupHelper = value;
                    if (MainWindow.Instance?.Radar?.MapSetupHelper?.ViewModel is MapSetupHelperViewModel vm)
                    {
                        vm.IsVisible = value;
                    }
                    OnPropertyChanged(nameof(ShowMapSetupHelper));
                }
            }
        }

        #endregion
    }
}
