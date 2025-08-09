using eft_dma_radar.UI.Hotkeys;
using eft_dma_radar.DMA;
using eft_dma_radar.DMA.ScatterAPI;

namespace eft_dma_radar.Unity
{
    internal static class InputManager
    {
        private static ulong _inputManager;

        static InputManager()
        {
            new Thread(Worker)
            {
                IsBackground = true
            }.Start();
        }

        /// <summary>
        /// Attempts to load Input Manager.
        /// </summary>
        /// <param name="unityBase">UnityPlayer.dll Base Addr</param>
        public static void Initialize(ulong unityBase)
        {
            try
            {
                _inputManager = Memory.ReadPtr(unityBase + UnityOffsets.ModuleBase.InputManager, false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("ERROR Initializing Input Manager", ex);
            }
        }

        /// <summary>
        /// Reset InputManager (usually after game closure).
        /// </summary>
        public static void Reset()
        {
            _inputManager = 0x0;
        }

        /// <summary>
        /// InputManager Managed thread.
        /// </summary>
        private static void Worker()
        {
            Debug.WriteLine("InputManager thread starting...");
            while (true)
            {
                try
                {
                    if (MemDMA.WaitForProcess())
                    {
                        ProcessAllHotkeys();
                    }
                }
                catch { }
                finally
                {
                    Thread.Sleep(10);
                }
            }
        }
        /// <summary>
        /// Check all hotkeys, and execute delegates.
        /// </summary>
        private static void ProcessAllHotkeys()
        {
            if (HotkeyManagerViewModel.Hotkeys.Count > 0)
            {
                using var mapLease = ScatterReadMap.Lease(out var map);
                var round1 = map.AddRound(false);
                int i = 0;
                var currentKeyState = Memory.ReadPtr(_inputManager + UnityOffsets.UnityInputManager.CurrentKeyState);
                foreach (var kvp in HotkeyManagerViewModel.Hotkeys)
                {
                    ProcessHotkey(kvp.Key, kvp.Value, currentKeyState, round1[i]);
                    i++;
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
            sr.AddEntry<uint>(0, v10 + v6 * 0x4); // v10[v6] = Result
            sr.Callbacks += x2 =>
            {
                if (x2.TryGetResult<uint>(0, out var v12))
                {
                    bool isKeyDown = (v12 & 1u << (int)v11) != 0;
                    action.Execute(isKeyDown);
                }
            };
        }
    }

}
