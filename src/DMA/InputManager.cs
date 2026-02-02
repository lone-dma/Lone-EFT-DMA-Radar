/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using LoneEftDmaRadar.Misc.Workers;
using LoneEftDmaRadar.UI.Hotkeys;
using VmmSharpEx;
using VmmSharpEx.Extensions.Input;

namespace LoneEftDmaRadar.DMA
{
    public sealed class InputManager
    {
        private readonly VmmInputManager _input;
        private readonly WorkerThread _thread;

        public InputManager(Vmm vmm)
        {
            _input = new VmmInputManager(vmm);
            _thread = new()
            {
                Name = nameof(InputManager),
                SleepDuration = TimeSpan.FromMilliseconds(12),
                SleepMode = WorkerThreadSleepMode.DynamicSleep
            };
            _thread.PerformWork += InputManager_PerformWork;
            _thread.Start();
        }

        private void InputManager_PerformWork(object sender, WorkerThreadArgs e)
        {
            var hotkeys = HotkeyManager.Hotkeys;
            if (hotkeys.Count == 0)
                return;

            _input.UpdateKeys();
            foreach (var kvp in hotkeys)
            {
                bool isKeyDown = _input.IsKeyDown(kvp.Key);
                kvp.Value.Execute(isKeyDown);
            }
        }
    }

}

