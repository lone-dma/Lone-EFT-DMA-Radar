/*
 * EFT DMA Radar Lite
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

using EftDmaRadarLite.UI.Hotkeys;
using EftDmaRadarLite.DMA;
using VmmSharpEx.Scatter;
using EftDmaRadarLite.Misc;
using EftDmaRadarLite.Misc.Workers;

namespace EftDmaRadarLite.Unity
{
    internal static class InputManager
    {
        private static readonly WorkerThread _thread;
        private static ulong _inputManager;

        static InputManager()
        {
            MemDMA.ProcessStarting += MemDMA_ProcessStarting;
            MemDMA.ProcessStopped += MemDMA_ProcessStopped;
            _thread = new()
            {
                Name = "InputManager",
                SleepDuration = TimeSpan.FromMilliseconds(12),
                SleepMode = WorkerThreadSleepMode.DynamicSleep
            };
            _thread.PerformWork += Thread_PerformWork;
            _thread.Start();
        }

        private static void MemDMA_ProcessStarting(object sender, EventArgs e)
        {
            ulong unityBase = Memory.UnityBase;
            unityBase.ThrowIfInvalidVirtualAddress(nameof(unityBase));
            _inputManager = Memory.ReadPtr(unityBase + UnityOffsets.ModuleBase.InputManager, false);
        }

        private static void MemDMA_ProcessStopped(object sender, EventArgs e)
        {
            _inputManager = 0x0;
        }

        private static void Thread_PerformWork(object sender, WorkerThreadArgs e)
        {
            if (MemDMA.WaitForProcess())
            {
                ProcessAllHotkeys();
            }
        }

        /// <summary>
        /// Check all hotkeys, and execute delegates.
        /// </summary>
        private static void ProcessAllHotkeys()
        {
            var hotkeys = HotkeyManagerViewModel.Hotkeys.AsEnumerable();
            if (hotkeys.Any())
            {
                var currentKeyState = Memory.ReadPtr(_inputManager + UnityOffsets.UnityInputManager.CurrentKeyState);
                using var map = Memory.CreateScatterMap();
                var round1 = map.AddRound(false);
                int i = 0;
                foreach (var kvp in hotkeys)
                {
                    ProcessHotkey(kvp.Key, kvp.Value, currentKeyState, round1[i++]);
                }
                map.Execute();
            }
        }

        /// <summary>
        /// Checks if a Hotkey is pressed, and if pressed executes the related Action Controller.
        /// </summary>
        /// <param name="keycode">Hotkey key value</param>
        /// <param name="action">Hotkey action controller</param>
        /// <param name="currentKeyState">Current key state addr</param>
        /// <param name="sr">SR (cached)</param>
        private static void ProcessHotkey(UnityKeyCode keycode, HotkeyAction action, ulong currentKeyState, ScatterReadIndex sr)
        {
            uint v3 = (uint)keycode;
            uint v6 = v3 >> 5;
            ulong v10 = currentKeyState;

            uint v11 = v3 & 0x1F;
            sr.AddValueEntry<uint>(0, v10 + v6 * 0x4); // v10[v6] = Result
            sr.Completed += (sender, x1) =>
            {
                if (x1.TryGetValue<uint>(0, out var v12))
                {
                    bool isKeyDown = (v12 & 1u << (int)v11) != 0;
                    action.Execute(isKeyDown);
                }
            };
        }
    }

}
