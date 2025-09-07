using EftDmaRadarLite.ESP;
using EftDmaRadarLite.UI.Hotkeys;
using EftDmaRadarLite.UI.Radar.ViewModels;

namespace EftDmaRadarLite
{
    public sealed class MainWindowViewModel
    {
        private readonly MainWindow _parent;
        //public event PropertyChangedEventHandler PropertyChanged;

        public MainWindowViewModel(MainWindow parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            LoadHotkeyManager();
        }

        public void ToggleFullscreen(bool toFullscreen)
        {
            if (toFullscreen)
            {
                // Full‐screen
                _parent.WindowStyle = WindowStyle.None;
                _parent.ResizeMode = ResizeMode.NoResize;
                _parent.Topmost = true;
                _parent.WindowState = WindowState.Maximized;
            }
            else
            {
                _parent.WindowStyle = WindowStyle.SingleBorderWindow;
                _parent.ResizeMode = ResizeMode.CanResize;
                _parent.Topmost = false;
                _parent.WindowState = WindowState.Normal;
            }
        }

        #region Hotkey Manager

        private const int HK_ZOOMTICKAMT = 5; // amt to zoom
        private const int HK_ZOOMTICKDELAY = 120; // ms

        /// <summary>
        /// Loads Hotkey Manager resources.
        /// Only call from Primary Thread/Window (ONCE!)
        /// </summary>
        private void LoadHotkeyManager()
        {
            var zoomIn = new HotkeyActionController("Zoom In");
            zoomIn.Delay = HK_ZOOMTICKDELAY;
            zoomIn.HotkeyDelayElapsed += ZoomIn_HotkeyDelayElapsed;
            var zoomOut = new HotkeyActionController("Zoom Out");
            zoomOut.Delay = HK_ZOOMTICKDELAY;
            zoomOut.HotkeyDelayElapsed += ZoomOut_HotkeyDelayElapsed;
            var toggleLoot = new HotkeyActionController("Toggle Loot");
            toggleLoot.HotkeyStateChanged += ToggleLoot_HotkeyStateChanged;
            var toggleESPWidget = new HotkeyActionController("Toggle ESP Widget");
            toggleESPWidget.HotkeyStateChanged += ToggleESPWidget_HotkeyStateChanged;
            var toggleNames = new HotkeyActionController("Toggle Player Names");
            toggleNames.HotkeyStateChanged += ToggleNames_HotkeyStateChanged;
            var toggleInfo = new HotkeyActionController("Toggle Game Info Tab");
            toggleInfo.HotkeyStateChanged += ToggleInfo_HotkeyStateChanged;
            var toggleQuestHelper = new HotkeyActionController("Toggle Quest Helper");
            toggleQuestHelper.HotkeyStateChanged += ToggleQuestHelper_HotkeyStateChanged;
            var toggleShowFood = new HotkeyActionController("Toggle Show Food");
            toggleShowFood.HotkeyStateChanged += ToggleShowFood_HotkeyStateChanged;
            var toggleShowMeds = new HotkeyActionController("Toggle Show Meds");
            toggleShowMeds.HotkeyStateChanged += ToggleShowMeds_HotkeyStateChanged;
            var espWidgZoomIn = new HotkeyActionController("ESP Zoom In");
            espWidgZoomIn.HotkeyStateChanged += EspZoomIn_HotkeyStateChanged;
            var espWidgZoomOut = new HotkeyActionController("ESP Zoom Out");
            espWidgZoomOut.HotkeyStateChanged += EspZoomOut_HotkeyStateChanged;
            // Add to Static Collection:
            HotkeyAction.RegisterController(zoomIn);
            HotkeyAction.RegisterController(zoomOut);
            HotkeyAction.RegisterController(toggleLoot);
            HotkeyAction.RegisterController(toggleESPWidget);
            HotkeyAction.RegisterController(toggleNames);
            HotkeyAction.RegisterController(toggleInfo);
            HotkeyAction.RegisterController(toggleQuestHelper);
            HotkeyAction.RegisterController(toggleShowFood);
            HotkeyAction.RegisterController(toggleShowMeds);
            HotkeyAction.RegisterController(espWidgZoomIn);
            HotkeyAction.RegisterController(espWidgZoomOut);
        }

        private void EspZoomOut_HotkeyStateChanged(object sender, HotkeyEventArgs e)
        {
            if (e.State)
            {
                var zooms = ViewMatrix.ZoomLevels.Span;
                // find the current zoom's index (-1 if somehow not found)
                int idx = zooms.IndexOf(App.Config.EspWidget.Zoom);
                // step backward one, wrapping to last when we go below 0
                idx = (idx - 1 + ViewMatrix.ZoomLevels.Length) % ViewMatrix.ZoomLevels.Length;
                // apply it
                App.Config.EspWidget.Zoom = zooms[idx];
            }
        }

        private void EspZoomIn_HotkeyStateChanged(object sender, HotkeyEventArgs e)
        {
            if (e.State)
            {
                var zooms = ViewMatrix.ZoomLevels.Span;
                // find the current zoom's index (-1 if somehow not found)
                int idx = zooms.IndexOf(App.Config.EspWidget.Zoom);
                // step forward one, wrapping back to 0 when we pass the end
                idx = (idx + 1) % ViewMatrix.ZoomLevels.Length;
                // apply it
                App.Config.EspWidget.Zoom = zooms[idx];
            }
        }

        private void ToggleShowMeds_HotkeyStateChanged(object sender, HotkeyEventArgs e)
        {
            if (e.State && _parent.Radar?.Overlay?.ViewModel is RadarOverlayViewModel vm)
            {
                vm.ShowMeds = !vm.ShowMeds;
            }
        }

        private void ToggleShowFood_HotkeyStateChanged(object sender, HotkeyEventArgs e)
        {
            if (e.State && _parent.Radar?.Overlay?.ViewModel is RadarOverlayViewModel vm)
            {
                vm.ShowFood = !vm.ShowFood;
            }
        }

        private void ToggleQuestHelper_HotkeyStateChanged(object sender, HotkeyEventArgs e)
        {
            if (e.State)
                App.Config.QuestHelper.Enabled = !App.Config.QuestHelper.Enabled;
        }

        private void ToggleInfo_HotkeyStateChanged(object sender, HotkeyEventArgs e)
        {
            if (e.State && _parent.Settings?.ViewModel is SettingsViewModel vm)
                vm.PlayerInfoWidget = !vm.PlayerInfoWidget;
        }

        private void ToggleNames_HotkeyStateChanged(object sender, HotkeyEventArgs e)
        {
            if (e.State && _parent.Settings?.ViewModel is SettingsViewModel vm)
                vm.HideNames = !vm.HideNames;
        }

        private void ToggleESPWidget_HotkeyStateChanged(object sender, HotkeyEventArgs e)
        {
            if (e.State && _parent.Settings?.ViewModel is SettingsViewModel vm)
                vm.ESPWidget = !vm.ESPWidget;
        }

        private void ToggleLoot_HotkeyStateChanged(object sender, HotkeyEventArgs e)
        {
            if (e.State && _parent.Settings?.ViewModel is SettingsViewModel vm)
                vm.ShowLoot = !vm.ShowLoot;
        }

        private void ZoomOut_HotkeyDelayElapsed(object sender, EventArgs e)
        {
            _parent.Radar?.ViewModel?.ZoomOut(HK_ZOOMTICKAMT);
        }

        private void ZoomIn_HotkeyDelayElapsed(object sender, EventArgs e)
        {
            _parent.Radar?.ViewModel?.ZoomIn(HK_ZOOMTICKAMT);
        }

        #endregion
    }
}
