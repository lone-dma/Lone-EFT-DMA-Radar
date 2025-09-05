using Collections.Pooled;
using EftDmaRadarLite.Tarkov.Data;
using EftDmaRadarLite.Tarkov.GameWorld;
using EftDmaRadarLite.Tarkov.Quests;
using EftDmaRadarLite.UI.ColorPicker;
using EftDmaRadarLite.UI.Data;
using EftDmaRadarLite.UI.Hotkeys;
using EftDmaRadarLite.UI.Misc;
using EftDmaRadarLite.UI.Radar.Views;
using EftDmaRadarLite.UI.Skia;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EftDmaRadarLite.UI.Radar.ViewModels
{
    public sealed class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly SettingsTab _parent;
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public SettingsViewModel(SettingsTab parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            RestartRadarCommand = new SimpleCommand(OnRestartRadar);
            OpenHotkeyManagerCommand = new SimpleCommand(OnOpenHotkeyManager);
            OpenColorPickerCommand = new SimpleCommand(OnOpenColorPicker);
            BackupConfigCommand = new SimpleCommand(OnBackupConfig);
            SaveConfigCommand = new SimpleCommand(OnSaveConfig);
            MonitorDetectResCommand = new SimpleCommand(async () => await OnMonitorDetectResAsync());
            InitializeContainers();
            CameraManager.UpdateViewportRes();
            SetScaleValues(UIScale);
            parent.IsVisibleChanged += Parent_IsVisibleChanged;
        }

        private void Parent_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool visible && visible &&
                Memory.QuestManager?.CurrentQuests is IReadOnlyDictionary<string, QuestEntry> quests)
            {
                using var currentQuests = CurrentQuests.ToPooledList(); // snapshot
                using var currentIds = new PooledSet<string>(currentQuests.Select(q => q.Id), StringComparer.OrdinalIgnoreCase);
                using var desiredIds = new PooledSet<string>(quests.Keys, StringComparer.OrdinalIgnoreCase);

                // remove stale
                foreach (var q in currentQuests.Where(q => !desiredIds.Contains(q.Id)))
                    CurrentQuests.Remove(q);

                // add missing
                foreach (var key in desiredIds.Except(currentIds))
                {
                    if (quests.TryGetValue(key, out var newQuest))
                        CurrentQuests.Add(newQuest);
                }
            }
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
                var backupFile = EftDmaConfig.Filename + ".bak";
                if (File.Exists(backupFile) &&
                    MessageBox.Show("Overwrite backup?", "Backup Config", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                await File.WriteAllTextAsync(backupFile, JsonSerializer.Serialize(App.Config, new JsonSerializerOptions { WriteIndented = true }));
                MessageBox.Show($"Backed up to {backupFile}", "Backup Config");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Backup Config", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public ICommand SaveConfigCommand { get; }
        private async void OnSaveConfig()
        {
            try
            {
                await App.Config.SaveAsync();
                MessageBox.Show($"Config saved to {App.ConfigPath.FullName}", "Save Config");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Save Config", MessageBoxButton.OK, MessageBoxImage.Error);
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
            MainWindow.Instance?.Radar?.ViewModel?.ESPWidget?.SetScaleFactor(newScale);
            MainWindow.Instance?.Radar?.ViewModel?.InfoWidget?.SetScaleFactor(newScale);

            #region UpdatePaints

            /// Outlines
            SKPaints.TextOutline.StrokeWidth = 2f * newScale;
            // Shape Outline is computed before usage due to different stroke widths

            SKPaints.PaintConnectorGroup.StrokeWidth = 2.25f * newScale;
            SKPaints.PaintMouseoverGroup.StrokeWidth = 3 * newScale;
            SKPaints.PaintLocalPlayer.StrokeWidth = 3 * newScale;
            SKPaints.PaintTeammate.StrokeWidth = 3 * newScale;
            SKPaints.PaintPMC.StrokeWidth = 3 * newScale;
            SKPaints.PaintWatchlist.StrokeWidth = 3 * newScale;
            SKPaints.PaintStreamer.StrokeWidth = 3 * newScale;
            SKPaints.PaintScav.StrokeWidth = 3 * newScale;
            SKPaints.PaintRaider.StrokeWidth = 3 * newScale;
            SKPaints.PaintBoss.StrokeWidth = 3 * newScale;
            SKPaints.PaintFocused.StrokeWidth = 3 * newScale;
            SKPaints.PaintPScav.StrokeWidth = 3 * newScale;
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
            SKFonts.EspWidgetFont.Size = 9f * newScale;
            SKFonts.InfoWidgetFont.Size = 12f * newScale;
            // SKWidgetControl
            SKWidgetControl.SetScaleFactorInternal(newScale);
            // Loot Paints
            Tarkov.Loot.LootItem.ScaleLootPaints(newScale);

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

        public bool ESPWidget
        {
            get => App.Config.EspWidget.Enabled;
            set
            {
                if (App.Config.EspWidget.Enabled != value)
                {
                    App.Config.EspWidget.Enabled = value;
                    OnPropertyChanged(nameof(ESPWidget));
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

        public bool StaticContainersHideSearched
        {
            get => App.Config.Containers.HideSearched;
            set
            {
                if (App.Config.Containers.HideSearched != value)
                {
                    App.Config.Containers.HideSearched = value;
                    OnPropertyChanged(nameof(StaticContainersHideSearched));
                }
            }
        }

        private void InitializeContainers()
        {
            var entries = EftDataManager.AllContainers.Values
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

        #region Quest Helper

        public bool QuestHelperEnabled
        {
            get => App.Config.QuestHelper.Enabled;
            set
            {
                if (App.Config.QuestHelper.Enabled != value)
                {
                    App.Config.QuestHelper.Enabled = value;
                    OnPropertyChanged(nameof(QuestHelperEnabled));
                }
            }
        }

        public ObservableCollection<QuestEntry> CurrentQuests { get; } = new();

        #endregion

        #region Monitor Info

        public string MonitorWidth
        {
            get => App.Config.EspWidget.MonitorWidth.ToString();
            set
            {
                if (App.Config.EspWidget.MonitorWidth.ToString() != value)
                {
                    if (int.TryParse(value, out var w))
                    {
                        App.Config.EspWidget.MonitorWidth = w;
                        CameraManager.UpdateViewportRes();
                        OnPropertyChanged(nameof(MonitorWidth));
                    }
                }
            }
        }

        public string MonitorHeight
        {
            get => App.Config.EspWidget.MonitorHeight.ToString();
            set
            {
                if (App.Config.EspWidget.MonitorHeight.ToString() != value)
                {
                    if (int.TryParse(value, out var h))
                    {
                        App.Config.EspWidget.MonitorHeight = h;
                        CameraManager.UpdateViewportRes();
                        OnPropertyChanged(nameof(MonitorHeight));
                    }
                }
            }
        }

        public ICommand MonitorDetectResCommand { get; }

        private async Task OnMonitorDetectResAsync()
        {
            try
            {
                if (!Memory.Ready)
                {
                    MessageBox.Show("Game not running!", "Detect Res", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var res = await Task.Run(() => Memory.GetMonitorRes());
                MonitorWidth = res.Width.ToString();
                MonitorHeight = res.Height.ToString();
                MessageBox.Show($"Detected {res.Width}×{res.Height}", "Detect Res");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Detect Res", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
