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

using LoneEftDmaRadar.UI.Hotkeys;
using LoneEftDmaRadar.UI.Hotkeys.Internal;
using LoneEftDmaRadar.UI.Radar.ViewModels;

namespace LoneEftDmaRadar
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
            var methods = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttribute<HotkeyAttribute>() is not null);

            foreach (var method in methods)
            {
                var attr = (HotkeyAttribute)method.GetCustomAttributes(typeof(HotkeyAttribute), false).FirstOrDefault();
                if (attr is not null)
                {
                    var controller = new HotkeyActionController(attr.Name, attr.Type, method.CreateDelegate<HotkeyDelegate>(this), attr.Interval);
                    HotkeyAction.RegisterController(controller);
                }
            }
        }

        [Hotkey("Toggle Show Quest Items")]
        private void ToggleShowQuestItems_HotkeyStateChanged(bool isKeyDown)
        {
            if (isKeyDown && _parent.Radar?.Overlay?.ViewModel is RadarOverlayViewModel vm)
            {
                vm.ShowQuestItems = !vm.ShowQuestItems;
            }
        }

        [Hotkey("Toggle Aimview Widget")]
        private void ToggleAimviewWidget_HotkeyStateChanged(bool isKeyDown)
        {
            if (isKeyDown && _parent.Settings?.ViewModel is SettingsViewModel vm)
                vm.AimviewWidget = !vm.AimviewWidget;
        }

        [Hotkey("Toggle Show Meds")]
        private void ToggleShowMeds_HotkeyStateChanged(bool isKeyDown)
        {
            if (isKeyDown && _parent.Radar?.Overlay?.ViewModel is RadarOverlayViewModel vm)
            {
                vm.ShowMeds = !vm.ShowMeds;
            }
        }

        [Hotkey("Toggle Show Food")]
        private void ToggleShowFood_HotkeyStateChanged(bool isKeyDown)
        {
            if (isKeyDown && _parent.Radar?.Overlay?.ViewModel is RadarOverlayViewModel vm)
            {
                vm.ShowFood = !vm.ShowFood;
            }
        }

        [Hotkey("Toggle Game Info Tab")]
        private void ToggleInfo_HotkeyStateChanged(bool isKeyDown)
        {
            if (isKeyDown && _parent.Settings?.ViewModel is SettingsViewModel vm)
                vm.PlayerInfoWidget = !vm.PlayerInfoWidget;
        }

        [Hotkey("Toggle Player Names")]
        private void ToggleNames_HotkeyStateChanged(bool isKeyDown)
        {
            if (isKeyDown && _parent.Settings?.ViewModel is SettingsViewModel vm)
                vm.HideNames = !vm.HideNames;
        }

        [Hotkey("Toggle Loot")]
        private void ToggleLoot_HotkeyStateChanged(bool isKeyDown)
        {
            if (isKeyDown && _parent.Settings?.ViewModel is SettingsViewModel vm)
                vm.ShowLoot = !vm.ShowLoot;
        }

        [Hotkey("Zoom Out", HotkeyType.OnIntervalElapsed, HK_ZOOMTICKDELAY)]
        private void ZoomOut_HotkeyDelayElapsed(bool isKeyDown)
        {
            _parent.Radar?.ViewModel?.ZoomOut(HK_ZOOMTICKAMT);
        }

        [Hotkey("Zoom In", HotkeyType.OnIntervalElapsed, HK_ZOOMTICKDELAY)]
        private void ZoomIn_HotkeyDelayElapsed(bool isKeyDown)
        {
            _parent.Radar?.ViewModel?.ZoomIn(HK_ZOOMTICKAMT);
        }

        #endregion
    }
}
