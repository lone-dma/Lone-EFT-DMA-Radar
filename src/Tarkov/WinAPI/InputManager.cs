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

/*
Refactored to C# from Metick's DMA Lib - https://github.com/Metick/DMALibrary/blob/Master/DMALibrary/Memory/InputManager.cpp
Thanks to Metick for the original implementation!
*/

using LoneEftDmaRadar.DMA;
using LoneEftDmaRadar.Misc.Workers;
using LoneEftDmaRadar.UI.Hotkeys;
using VmmSharpEx;
using VmmSharpEx.Extensions;

namespace LoneEftDmaRadar.Tarkov.WinAPI
{
    public sealed class InputManager
    {
        private readonly Vmm _vmm;
        private readonly WorkerThread _thread;

        private readonly byte[] _state = new byte[64];
        private readonly byte[] _previousState = new byte[64];
        private readonly ulong _gafAsyncKeyStateExport;
        private readonly uint _winLogonPid;

        public InputManager(Vmm vmm)
        {
            _vmm = vmm;
            if (!vmm.PidGetFromName("winlogon.exe", out _winLogonPid))
                throw new InvalidOperationException("Failed to get winlogon.exe PID");
            var pids = _vmm.PidGetAllFromName("csrss.exe");
            ulong gafAsyncKeyStateExport = 0;

            foreach (var pid in pids)
            {
                if (!_vmm.Map_GetModuleFromName(pid, "win32ksgd.sys", out var win32kModule))
                {
                    if (!_vmm.Map_GetModuleFromName(pid, "win32k.sys", out win32kModule))
                        throw new InvalidOperationException("Failed to get win32kModule");
                }
                ulong win32kBase = win32kModule.vaBase;
                ulong win32kSize = win32kModule.cbImageSize;

                ulong gSessionPtr = _vmm.FindSignature(pid, "48 8B 05 ?? ?? ?? ?? 48 8B 04 C8", win32kBase, win32kBase + win32kSize);
                if (gSessionPtr == 0)
                {
                    gSessionPtr = _vmm.FindSignature(pid, "48 8B 05 ?? ?? ?? ?? FF C9", win32kBase, win32kBase + win32kSize);
                    gSessionPtr.ThrowIfInvalidVirtualAddress(nameof(gSessionPtr));
                }
                int relative = Read<int>(pid, gSessionPtr + 3);
                ulong gSessionGlobalSlots = gSessionPtr + 7 + (ulong)relative;
                ulong userSessionState = 0;
                for (int i = 0; i < 4; i++)
                {
                    userSessionState = Read<ulong>(pid, Read<ulong>(pid, Read<ulong>(pid, gSessionGlobalSlots) + (ulong)(8 * i)));
                    if (userSessionState > 0x7FFFFFFFFFFF)
                        break;
                }

                if (!_vmm.Map_GetModuleFromName(pid, "win32kbase.sys", out var win32kbaseModule))
                    throw new InvalidOperationException("failed to get module win32kbase info");
                ulong win32kbaseBase = win32kbaseModule.vaBase;
                ulong win32kbaseSize = win32kbaseModule.cbImageSize;

                ulong ptr = _vmm.FindSignature(pid, "48 8D 90 ?? ?? ?? ?? E8 ?? ?? ?? ?? 0F 57 C0", win32kbaseBase, win32kbaseBase + win32kbaseSize);
                uint sessionOffset = 0;
                if (ptr != 0)
                {
                    sessionOffset = Read<uint>(pid, ptr + 3);
                    gafAsyncKeyStateExport = userSessionState + sessionOffset;
                }
                else
                {
                    throw new InvalidOperationException("failed to find offset for gafAyncKeyStateExport");
                }

                if (gafAsyncKeyStateExport > 0x7FFFFFFFFFFF)
                    break;
            }
            if (gafAsyncKeyStateExport <= 0x7FFFFFFFFFFF)
                throw new InvalidOperationException("Invalid gafAsyncKeyStateExport");

            _gafAsyncKeyStateExport = gafAsyncKeyStateExport;
            _thread = new() 
            {
                Name = nameof(InputManager),
                SleepDuration = TimeSpan.FromMilliseconds(12),
                SleepMode = WorkerThreadSleepMode.DynamicSleep
            };
            _thread.PerformWork += InputManager_PerformWork;
            _thread.Start();
        }

        private T Read<T>(uint pid, ulong address)
            where T : unmanaged
        {
            if (!_vmm.MemReadValue<T>(pid, address, out var result))
                throw new VmmException("Memory Read Failed!");
            return result;
        }

        private void InputManager_PerformWork(object sender, WorkerThreadArgs e)
        {
            var hotkeys = HotkeyManagerViewModel.Hotkeys.AsEnumerable();
            if (hotkeys.Any())
            {
                UpdateKeys();
                foreach (var kvp in hotkeys)
                {
                    bool isKeyDown = IsKeyDown((uint)kvp.Key);
                    kvp.Value.Execute(isKeyDown);
                }
            }
        }

        private void UpdateKeys()
        {
            var previous_key_state_bitmap = new byte[64];
            _state.CopyTo(previous_key_state_bitmap);

            // Read 64 bytes from gafAsyncKeyStateExport
            if (!_vmm.MemReadSpan(_winLogonPid | Vmm.PID_PROCESS_WITH_KERNELMEMORY, _gafAsyncKeyStateExport, _state, VmmSharpEx.Options.VmmFlags.NOCACHE))
                throw new VmmException("Failed to read key state bitmap.");

            for (int vk = 0; vk < 256; ++vk)
            {
                int idx = (vk * 2) / 8;
                int bit = 1 << (vk % 4 * 2);
                if ((_state[idx] & bit) != 0 && (previous_key_state_bitmap[idx] & bit) == 0)
                    _previousState[vk / 8] |= (byte)(1 << (vk % 8));
            }
        }

        private bool IsKeyDown(uint vkeyCode)
        {
            if (_gafAsyncKeyStateExport < 0x7FFFFFFFFFFF)
                return false;
            int idx = (int)(vkeyCode * 2 / 8);
            int bit = 1 << ((int)vkeyCode % 4 * 2);
            return (_state[idx] & bit) != 0;
        }
    }

}
