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

using LoneEftDmaRadar.Tarkov;
using LoneEftDmaRadar.Tarkov.GameWorld.Loot;
using LoneEftDmaRadar.UI.ColorPicker;
using LoneEftDmaRadar.UI.Data;
using LoneEftDmaRadar.UI.Hotkeys;
using LoneEftDmaRadar.UI.Misc;
using LoneEftDmaRadar.UI.Radar.Views;
using LoneEftDmaRadar.UI.Skia;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace LoneEftDmaRadar.UI.Radar.ViewModels
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
            OpenConfigCommand = new SimpleCommand(OnOpenConfig);
            InitializeContainers();
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
                var backupFile = Path.Combine(App.ConfigPath.FullName, $"{EftDmaConfig.Filename}.userbak");
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

        public ICommand OpenConfigCommand { get; }
        private async void OnOpenConfig()
        {
            try
            {
                Process.Start(new ProcessStartInfo(App.ConfigPath.FullName) { UseShellExecute = true });
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

        public int MaxDistance
        {
            get => (int)Math.Round(App.Config.UI.MaxDistance);
            set
            {
                if (App.Config.UI.MaxDistance != value)
                {
                    App.Config.UI.MaxDistance = value;
                    OnPropertyChanged(nameof(MaxDistance));
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
            // Update Widgets
            MainWindow.Instance?.Radar?.ViewModel?.InfoWidget?.SetScaleFactor(newScale);

            #region UpdatePaints

            /// Outlines
            SKPaints.TextOutline.StrokeWidth = 2f * newScale;
            // Shape Outline is computed before usage due to different stroke widths

            SKPaints.PaintConnectorGroup.StrokeWidth = 2.25f * newScale;
            SKPaints.PaintMouseoverGroup.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintLocalPlayer.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintTeammate.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintPMC.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintWatchlist.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintStreamer.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintScav.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintRaider.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintBoss.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintFocused.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintPScav.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintCorpse.StrokeWidth = 3 * newScale;
            SKPaints.PaintMeds.StrokeWidth = 3 * newScale;
            SKPaints.PaintFood.StrokeWidth = 3 * newScale;
            SKPaints.PaintBackpacks.StrokeWidth = 3 * newScale;
            SKPaints.PaintQuestItem.StrokeWidth = 3 * newScale;
            SKPaints.PaintWishlistItem.StrokeWidth = 3 * newScale;
            SKPaints.QuestHelperPaint.StrokeWidth = 3 * newScale;
            SKPaints.PaintDeathMarker.StrokeWidth = 3 * newScale;
            SKPaints.PaintLoot.StrokeWidth = 3 * newScale;
            SKPaints.PaintImportantLoot.StrokeWidth = 3 * newScale;
            SKPaints.PaintContainerLoot.StrokeWidth = 3 * newScale;
            SKPaints.PaintTransparentBacker.StrokeWidth = 1 * newScale;
            SKPaints.PaintExplosives.StrokeWidth = 3 * newScale;
            SKPaints.PaintExfilOpen.StrokeWidth = 1 * newScale;
            SKPaints.PaintExfilTransit.StrokeWidth = 1 * newScale;
            SKPaints.PaintExfilPending.StrokeWidth = 1 * newScale;
            SKPaints.PaintExfilClosed.StrokeWidth = 1 * newScale;
            SKPaints.PaintExfilInactive.StrokeWidth = 1 * newScale;
            // Fonts
            SKFonts.UIRegular.Size = 12f * newScale;
            SKFonts.UILarge.Size = 48f * newScale;
            SKFonts.InfoWidgetFont.Size = 12f * newScale;
            // SKWidgetControl
            AbstractSKWidget.SetScaleFactorInternal(newScale);
            // Loot Paints
            LootItem.ScaleLootPaints(newScale);

            #endregion
        }

        public int ContainerDistance
        {
            get => (int)Math.Round(App.Config.Containers.DrawDistance);
            set
            {
                if (App.Config.Containers.DrawDistance != value)
                {
                    App.Config.Containers.DrawDistance = value;
                    OnPropertyChanged(nameof(ContainerDistance));
                }
            }
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

        public bool PlayerInfoWidget
        {
            get => App.Config.InfoWidget.Enabled;
            set
            {
                if (App.Config.InfoWidget.Enabled != value)
                {
                    App.Config.InfoWidget.Enabled = value;
                    OnPropertyChanged(nameof(PlayerInfoWidget));
                }
            }
        }

        public bool ConnectGroups
        {
            get => App.Config.UI.ConnectGroups;
            set
            {
                if (App.Config.UI.ConnectGroups != value)
                {
                    App.Config.UI.ConnectGroups = value;
                    OnPropertyChanged(nameof(ConnectGroups));
                }
            }
        }

        public bool HideNames
        {
            get => App.Config.UI.HideNames;
            set
            {
                if (App.Config.UI.HideNames != value)
                {
                    App.Config.UI.HideNames = value;
                    OnPropertyChanged(nameof(HideNames));
                }
            }
        }

        public bool ShowMines
        {
            get => App.Config.UI.ShowMines;
            set
            {
                if (App.Config.UI.ShowMines != value)
                {
                    App.Config.UI.ShowMines = value;
                    OnPropertyChanged(nameof(ShowMines));
                }
            }
        }

        public bool TeammateAimlines
        {
            get => App.Config.UI.TeammateAimlines;
            set
            {
                if (App.Config.UI.TeammateAimlines != value)
                {
                    App.Config.UI.TeammateAimlines = value;
                    OnPropertyChanged(nameof(TeammateAimlines));
                }
            }
        }

        public bool AIAimlines
        {
            get => App.Config.UI.AIAimlines;
            set
            {
                if (App.Config.UI.AIAimlines != value)
                {
                    App.Config.UI.AIAimlines = value;
                    OnPropertyChanged(nameof(AIAimlines));
                }
            }
        }

        public bool MarkSusPlayers
        {
            get => App.Config.UI.MarkSusPlayers;
            set
            {
                if (App.Config.UI.MarkSusPlayers != value)
                {
                    App.Config.UI.MarkSusPlayers = value;
                    OnPropertyChanged(nameof(MarkSusPlayers));
                }
            }
        }

        public bool ShowLoot
        {
            get => App.Config.Loot.Enabled;
            set
            {
                if (App.Config.Loot.Enabled != value)
                {
                    App.Config.Loot.Enabled = value;
                    if (MainWindow.Instance?.Radar?.Overlay?.ViewModel is RadarOverlayViewModel vm)
                    {
                        vm.IsLootButtonVisible = value;
                    }
                    OnPropertyChanged(nameof(ShowLoot));
                }
            }
        }

        #endregion

        #region Loot

        public bool LootWishlist
        {
            get => App.Config.Loot.ShowWishlist;
            set
            {
                if (App.Config.Loot.ShowWishlist != value)
                {
                    App.Config.Loot.ShowWishlist = value;
                    OnPropertyChanged(nameof(LootWishlist));
                }
            }
        }

        public bool ShowStaticContainers
        {
            get => App.Config.Containers.Enabled;
            set
            {
                if (App.Config.Containers.Enabled != value)
                {
                    App.Config.Containers.Enabled = value;
                    OnPropertyChanged(nameof(ShowStaticContainers));
                }
            }
        }

        public bool StaticContainersSelectAll
        {
            get => App.Config.Containers.SelectAll;
            set
            {
                if (App.Config.Containers.SelectAll != value)
                {
                    App.Config.Containers.SelectAll = value;
                    foreach (var item in StaticContainers) item.IsTracked = value;
                    OnPropertyChanged(nameof(StaticContainersSelectAll));
                }
            }
        }

        private void InitializeContainers()
        {
            var entries = TarkovDataManager.AllContainers.Values
                .OrderBy(x => x.Name)
                .Select(x => new StaticContainerEntry(x));
            foreach (var entry in entries)
            {
                StaticContainers.Add(entry);
            }
        }

        public ObservableCollection<StaticContainerEntry> StaticContainers { get; } = new();

        public bool ContainerIsTracked(string id) => StaticContainers.Any(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase) && x.IsTracked);

        #endregion

    }
}
