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

using LoneEftDmaRadar.DMA;
using LoneEftDmaRadar.Misc.Workers;
using LoneEftDmaRadar.Tarkov.Unity.Structures;
using LoneEftDmaRadar.UI.Hotkeys;
using VmmSharpEx.Scatter;

namespace LoneEftDmaRadar.Tarkov.Unity
{
    public sealed class InputManager : IDisposable
    {
        private static InputManager _instance;
        private readonly WorkerThread _thread;
        private readonly ulong _inputManager;

        static InputManager()
        {
            MemDMA.ProcessStarting += MemDMA_ProcessStarting;
            MemDMA.ProcessStopped += MemDMA_ProcessStopped;
        }

        private static void MemDMA_ProcessStarting(object sender, EventArgs e)
        {
            _instance?.Dispose();
            _instance = new(Memory.UnityBase);
            Debug.WriteLine("InputManager initialized.");
        }

        private static void MemDMA_ProcessStopped(object sender, EventArgs e)
        {
            _instance?.Dispose();
            _instance = null;
        }

        private InputManager() { }

        private InputManager(ulong unityBase)
        {
            try
            {
                return; // TODO : InputManager
                unityBase.ThrowIfInvalidVirtualAddress(nameof(unityBase));
                //_inputManager = Memory.ReadPtr(unityBase + UnitySDK.ModuleBase.InputManager, false);
                _thread = new()
                {
                    Name = nameof(InputManager),
                    SleepDuration = TimeSpan.FromMilliseconds(12),
                    SleepMode = WorkerThreadSleepMode.DynamicSleep
                };
                _thread.PerformWork += InputManager_PerformWork;
                _thread.Start();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void InputManager_PerformWork(object sender, WorkerThreadArgs e)
        {
            //var hotkeys = HotkeyManagerViewModel.Hotkeys.AsEnumerable();
            //if (hotkeys.Any())
            //{
            //    var currentKeyState = Memory.ReadPtr(_inputManager + UnitySDK.UnityInputManager.CurrentKeyState);
            //    using var scatter = Memory.CreateScatter(VmmSharpEx.Options.VmmFlags.NOCACHE);
            //    foreach (var kvp in hotkeys)
            //    {
            //        ProcessHotkey(kvp.Key, kvp.Value, currentKeyState, scatter);
            //    }
            //    scatter.Execute();
            //}
        }

        /// <summary>
        /// Checks if a Hotkey is pressed, and if pressed executes the related Action Controller.
        /// </summary>
        /// <param name="keycode">Hotkey key value</param>
        /// <param name="action">Hotkey action controller</param>
        /// <param name="currentKeyState">Current key state addr</param>
        /// <param name="scatter">SR (cached)</param>
        private static void ProcessHotkey(UnityKeyCode keycode, HotkeyAction action, ulong currentKeyState, VmmScatter scatter)
        {
            uint v3 = (uint)keycode;
            uint v6 = v3 >> 5;
            ulong v10 = currentKeyState;

            uint v11 = v3 & 0x1F;
            ulong readAddr = v10 + v6 * 0x4;
            scatter.PrepareReadValue<uint>(readAddr); // v10[v6] = Result
            scatter.Completed += (sender, s) =>
            {
                if (s.ReadValue<uint>(readAddr, out var v12))
                {
                    bool isKeyDown = (v12 & 1u << (int)v11) != 0;
                    action.Execute(isKeyDown);
                }
            };
        }

        public void Dispose()
        {
            _thread?.Dispose();
        }
    }

}
