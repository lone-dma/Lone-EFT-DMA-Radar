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

using Collections.Pooled;
using LoneArenaDmaRadar.Arena.GameWorld;
using LoneArenaDmaRadar.Arena.GameWorld.Explosives;
using LoneArenaDmaRadar.Arena.GameWorld.Player;
using LoneArenaDmaRadar.Arena.Mono;
using LoneArenaDmaRadar.Arena.Unity;
using LoneArenaDmaRadar.Misc;
using System.Drawing;
using VmmSharpEx;
using VmmSharpEx.Options;
using VmmSharpEx.Refresh;
using VmmSharpEx.Scatter;

namespace LoneArenaDmaRadar.DMA
{
    /// <summary>
    /// DMA Memory Module.
    /// </summary>
    public sealed class MemDMA : IDisposable
    {
        #region Init

        private const string MEMORY_MAP_FILE = "mmap.txt";
        private const string GAME_PROCESS_NAME = "EscapeFromTarkovArena.exe";
        internal const uint MAX_READ_SIZE = 0x1000u * 1500u;
        private readonly Vmm _vmm;
        private uint _pid;
        private bool _restartRadar;

        public string MapID => Game?.MapID;
        public ulong MonoBase { get; private set; }
        public ulong UnityBase { get; private set; }
        public bool Starting { get; private set; }
        public bool Ready { get; private set; }
        public bool InRaid => Game?.InRaid ?? false;

        /// <summary>
        /// Set to TRUE to restart the Radar on the next game loop cycle.
        /// </summary>
        public bool RestartRadar
        {
            set
            {
                if (InRaid)
                    _restartRadar = value;
            }
        }

        public IReadOnlyCollection<AbstractPlayer> Players => Game?.Players;
        public IReadOnlyCollection<IExplosiveItem> Explosives => Game?.Explosives;
        public LocalPlayer LocalPlayer => Game?.LocalPlayer;
        public LocalGameWorld Game { get; private set; }

        static MemDMA()
        {
            RuntimeHelpers.RunClassConstructor(typeof(MonoLib).TypeHandle);
            RuntimeHelpers.RunClassConstructor(typeof(InputManager).TypeHandle);
        }

        internal MemDMA()
        {
            FpgaAlgo fpgaAlgo = App.Config.DMA.FpgaAlgo;
            bool useMemMap = App.Config.DMA.MemMapEnabled;
            Debug.WriteLine("Initializing DMA...");
            /// Check MemProcFS Versions...
            string vmmVersion = FileVersionInfo.GetVersionInfo("vmm.dll").FileVersion;
            string lcVersion = FileVersionInfo.GetVersionInfo("leechcore.dll").FileVersion;
            string versions = $"Vmm Version: {vmmVersion}\n" +
                $"Leechcore Version: {lcVersion}";
            string[] initArgs = new[] {
                "-norefresh",
                "-device",
                fpgaAlgo is FpgaAlgo.Auto ?
                    "fpga" : $"fpga://algo={(int)fpgaAlgo}",
                "-waitinitialize"};
            try
            {
                /// Begin Init...
                if (useMemMap)
                {
                    if (!File.Exists(MEMORY_MAP_FILE))
                    {
                        Debug.WriteLine("[DMA] No MemMap, attempting to generate...");
                        _vmm = new Vmm(args: initArgs)
                        {
                            EnableMemoryWriting = false
                        };
                        _ = _vmm.GetMemoryMap(
                            applyMap: true,
                            outputFile: MEMORY_MAP_FILE);
                    }
                    else
                    {
                        var mapArgs = new[] { "-memmap", MEMORY_MAP_FILE };
                        initArgs = initArgs.Concat(mapArgs).ToArray();
                    }
                }
                _vmm ??= new Vmm(args: initArgs)
                {
                    EnableMemoryWriting = false
                };
                _vmm.RegisterAutoRefresh(RefreshOption.MemoryPartial, TimeSpan.FromMilliseconds(300));
                _vmm.RegisterAutoRefresh(RefreshOption.TlbPartial, TimeSpan.FromSeconds(2));
                ProcessStopped += MemDMA_ProcessStopped;
                RaidStopped += MemDMA_RaidStopped;
                // Start Memory Thread after successful startup
                new Thread(MemoryPrimaryWorker)
                {
                    IsBackground = true
                }.Start();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                "DMA Initialization Failed!\n" +
                $"Reason: {ex.Message}\n" +
                $"{versions}\n\n" +
                "===TROUBLESHOOTING===\n" +
                "1. Reboot both your Game PC / Radar PC (This USUALLY fixes it).\n" +
                "2. Reseat all cables/connections and make sure they are secure.\n" +
                "3. Changed Hardware/Operating System on Game PC? Reset your DMA Config ('Options' menu in Client) and try again.\n" +
                "4. Make sure all Setup Steps are completed (See DMA Setup Guide/FAQ for additional troubleshooting).\n\n" +
                "PLEASE REVIEW THE ABOVE BEFORE CONTACTING SUPPORT!");
            }
        }

        /// <summary>
        /// Main worker thread to perform DMA Reads on.
        /// </summary>
        private void MemoryPrimaryWorker()
        {
            Debug.WriteLine("Memory thread starting...");
            while (MainWindow.Instance is null)
                Thread.Sleep(1);
            while (true)
            {
                try
                {
                    while (true) // Main Loop
                    {
                        RunStartupLoop();
                        OnProcessStarted();
                        RunGameLoop();
                        OnProcessStopped();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"FATAL ERROR on Memory Thread: {ex}");
                    OnProcessStopped();
                    Thread.Sleep(1000);
                }
            }
        }

        #endregion

        #region Startup / Main Loop

        /// <summary>
        /// Starts up the Game Process and all mandatory modules.
        /// Returns to caller when the Game is ready.
        /// </summary>
        private void RunStartupLoop()
        {
            Debug.WriteLine("New Process Startup");
            while (true) // Startup loop
            {
                try
                {
                    _vmm.ForceFullRefresh();
                    ResourceJanitor.Run();
                    LoadProcess();
                    LoadModules();
                    this.Starting = true;
                    OnProcessStarting();
                    this.Ready = true;
                    Debug.WriteLine("Process Startup [OK]");
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Process Startup [FAIL]: {ex}");
                    OnProcessStopped();
                    Thread.Sleep(1000);
                }
            }
        }

        /// <summary>
        /// Main Game Loop Method.
        /// Returns to caller when Game is no longer running.
        /// </summary>
        private void RunGameLoop()
        {
            while (true)
            {
                try
                {
                    using (var game = Game = LocalGameWorld.CreateGameInstance())
                    {
                        OnRaidStarted();
                        game.Start();
                        while (game.InRaid)
                        {
                            if (_restartRadar)
                            {
                                Debug.WriteLine("Restarting Radar per User Request.");
                                _restartRadar = false;
                                break;
                            }
                            game.Refresh();
                            Thread.Sleep(133);
                        }
                    }
                }
                catch (OperationCanceledException ex) // Process Closed
                {
                    Debug.WriteLine(ex.Message);
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unhandled Exception in Game Loop: {ex}");
                    break;
                }
                finally
                {
                    OnRaidStopped();
                    Thread.Sleep(100);
                }
            }
        }

        /// <summary>
        /// Raised when the game is stopped.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MemDMA_ProcessStopped(object sender, EventArgs e)
        {
            _restartRadar = default;
            this.Starting = default;
            this.Ready = default;
            UnityBase = default;
            MonoBase = default;
            _pid = default;
        }


        private void MemDMA_RaidStopped(object sender, EventArgs e)
        {
            Game = null;
        }

        /// <summary>
        /// Obtain the PID for the Game Process.
        /// </summary>
        private void LoadProcess()
        {
            
            if (!_vmm.PidGetFromName(GAME_PROCESS_NAME, out uint pid))
                throw new InvalidOperationException($"Unable to find '{GAME_PROCESS_NAME}'");
            _pid = pid;
        }

        /// <summary>
        /// Gets the Game Process Base Module Addresses.
        /// </summary>
        private void LoadModules()
        {
            var unityBase = _vmm.ProcessGetModuleBase(_pid, "UnityPlayer.dll");
            unityBase.ThrowIfInvalidVirtualAddress(nameof(unityBase));
            var monoBase = _vmm.ProcessGetModuleBase(_pid, "mono-2.0-bdwgc.dll");
            monoBase.ThrowIfInvalidVirtualAddress(nameof(monoBase));
            UnityBase = unityBase;
            MonoBase = monoBase;
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised when the game process is starting up (after getting PID/Module Base).
        /// Outside Subscribers should handle exceptions!
        /// </summary>
        public static event EventHandler<EventArgs> ProcessStarting;
        /// <summary>
        /// Raised when the game process is successfully started.
        /// Outside Subscribers should handle exceptions!
        /// </summary>
        public static event EventHandler<EventArgs> ProcessStarted;
        /// <summary>
        /// Raised when the game process is no longer running.
        /// Outside Subscribers should handle exceptions!
        /// </summary>
        public static event EventHandler<EventArgs> ProcessStopped;
        /// <summary>
        /// Raised when a raid starts.
        /// Outside Subscribers should handle exceptions!
        /// </summary>
        public static event EventHandler<EventArgs> RaidStarted;
        /// <summary>
        /// Raised when a raid ends.
        /// Outside Subscribers should handle exceptions!
        /// </summary>
        public static event EventHandler<EventArgs> RaidStopped;

        /// <summary>
        /// Raises the ProcessStarting Event.
        /// </summary>
        private static void OnProcessStarting()
        {
            ProcessStarting?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the ProcessStarted Event.
        /// </summary>
        private static void OnProcessStarted()
        {
            ProcessStarted?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the ProcessStopped Event.
        /// </summary>
        private static void OnProcessStopped()
        {
            ProcessStopped?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the RaidStarted Event.
        /// </summary>
        private static void OnRaidStarted()
        {
            RaidStarted?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the RaidStopped Event.
        /// </summary>
        private static void OnRaidStopped()
        {
            RaidStopped?.Invoke(null, EventArgs.Empty);
        }

        #endregion

        #region Read Methods

        /// <summary>
        /// Prefetch pages into the cache.
        /// </summary>
        /// <param name="va"></param>
        public void ReadCache(params ulong[] va)
        {
            _vmm.MemPrefetchPages(_pid, va);
        }

        /// <summary>
        /// Read memory into a Buffer of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">Value Type <typeparamref name="T"/></typeparam>
        /// <param name="addr">Virtual Address to read from.</param>
        /// <param name="span">Buffer to receive memory read in.</param>
        /// <param name="useCache">Use caching for this read.</param>
        public void ReadSpan<T>(ulong addr, Span<T> span, bool useCache = true)
            where T : unmanaged
        {
            uint cb = (uint)checked(Unsafe.SizeOf<T>() * span.Length);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(cb, MAX_READ_SIZE, nameof(cb));
            var flags = useCache ? VmmFlags.NONE : VmmFlags.NOCACHE;

            if (!_vmm.MemReadSpan(_pid, addr, span, flags))
                throw new VmmException("Memory Read Failed!");
        }

        /// <summary>
        /// Read memory into a Buffer of type <typeparamref name="T"/> and ensure the read is correct.
        /// </summary>
        /// <typeparam name="T">Value Type <typeparamref name="T"/></typeparam>
        /// <param name="addr">Virtual Address to read from.</param>
        /// <param name="span">Buffer to receive memory read in.</param>
        public void ReadSpanEnsure<T>(ulong addr, Span<T> span)
            where T : unmanaged
        {
            uint cb = (uint)checked(Unsafe.SizeOf<T>() * span.Length);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(cb, MAX_READ_SIZE, nameof(cb));
            var buffer2 = new T[span.Length].AsSpan();
            var buffer3 = new T[span.Length].AsSpan();
            if (!_vmm.MemReadSpan(_pid, addr, buffer3, VmmFlags.NOCACHE))
                throw new VmmException("Memory Read Failed!");
            Thread.SpinWait(5);
            if (!_vmm.MemReadSpan(_pid, addr, buffer2, VmmFlags.NOCACHE))
                throw new VmmException("Memory Read Failed!");
            Thread.SpinWait(5);
            if (!_vmm.MemReadSpan(_pid, addr, span, VmmFlags.NOCACHE))
                throw new VmmException("Memory Read Failed!");
            if (!span.SequenceEqual(buffer2) || !span.SequenceEqual(buffer3) || !buffer2.SequenceEqual(buffer3))
            {
                throw new VmmException("Memory Read Failed!");
            }
        }

        /// <summary>
        /// Read an array of type <typeparamref name="T"/> from memory.
        /// The first element begins reading at 0x0 and the array is assumed to be contiguous.
        /// IMPORTANT: You must call <see cref="IDisposable.Dispose"/> on the returned SharedArray when done."/>
        /// </summary>
        /// <typeparam name="T">Value type to read.</typeparam>
        /// <param name="addr">Address to read from.</param>
        /// <param name="count">Number of array elements to read.</param>
        /// <param name="useCache">Use caching for this read.</param>
        /// <returns><see cref="PooledMemory{T}"/> value. Be sure to call <see cref="IDisposable.Dispose"/>!</returns>
        public PooledMemory<T> ReadArray<T>(ulong addr, int count, bool useCache = true)
            where T : unmanaged
        {
            var flags = useCache ? VmmFlags.NONE : VmmFlags.NOCACHE;
            var arr = _vmm.MemReadArray<T>(_pid, addr, count, flags) ??
                throw new VmmException("Memory Read Failed!");
            return arr;
        }


        /// <summary>
        /// Read a chain of pointers and get the final result.
        /// </summary>
        /// <param name="addr">Base virtual address to read from.</param>
        /// <param name="useCache">Use caching for this read (recommended).</param>
        /// <param name="offsets">Offsets to read in succession.</param>
        /// <returns>Pointer address after final offset.</returns>
        public ulong ReadPtrChain(ulong addr, bool useCache, params Span<uint> offsets)
        {
            ulong pointer = addr;
            foreach (var offset in offsets)
            {
                pointer = ReadPtr(checked(pointer + offset), useCache);
            }
            return pointer;
        }

        /// <summary>
        /// Resolves a pointer and returns the memory address it points to.
        /// </summary>
        public ulong ReadPtr(ulong addr, bool useCache = true)
        {
            var pointer = ReadValue<VmmPointer>(addr, useCache);
            pointer.ThrowIfInvalid();
            return pointer;
        }

        /// <summary>
        /// Read value type/struct from specified address.
        /// </summary>
        /// <typeparam name="T">Specified Value Type.</typeparam>
        /// <param name="addr">Address to read from.</param>
        public T ReadValue<T>(ulong addr, bool useCache = true)
            where T : unmanaged, allows ref struct
        {
            var flags = useCache ? VmmFlags.NONE : VmmFlags.NOCACHE;
            if (!_vmm.MemReadValue<T>(_pid, addr, out var result, flags))
                throw new VmmException("Memory Read Failed!");
            return result;
        }

        /// <summary>
        /// Read value type/struct from specified address multiple times to ensure the read is correct.
        /// </summary>
        /// <typeparam name="T">Specified Value Type.</typeparam>
        /// <param name="addr">Address to read from.</param>
        public unsafe T ReadValueEnsure<T>(ulong addr)
            where T : unmanaged, allows ref struct
        {
            int cb = Unsafe.SizeOf<T>();
            if (!_vmm.MemReadValue<T>(_pid, addr, out var r1, VmmFlags.NOCACHE))
                throw new VmmException("Memory Read Failed!");
            Thread.SpinWait(5);
            if (!_vmm.MemReadValue<T>(_pid, addr, out var r2, VmmFlags.NOCACHE))
                throw new VmmException("Memory Read Failed!");
            Thread.SpinWait(5);
            if (!_vmm.MemReadValue<T>(_pid, addr, out var r3, VmmFlags.NOCACHE))
                throw new VmmException("Memory Read Failed!");
            var b1 = new ReadOnlySpan<byte>(&r1, cb);
            var b2 = new ReadOnlySpan<byte>(&r2, cb);
            var b3 = new ReadOnlySpan<byte>(&r3, cb);
            if (!b1.SequenceEqual(b2) || !b1.SequenceEqual(b3) || !b2.SequenceEqual(b3))
            {
                throw new VmmException("Memory Read Failed!");
            }
            return r1;
        }

        /// <summary>
        /// Read null terminated UTF8 string.
        /// </summary>
        public string ReadUtf8String(ulong addr, int cb, bool useCache = true) // read n bytes (string)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(cb, 0x1000, nameof(cb));
            var flags = useCache ? VmmFlags.NONE : VmmFlags.NOCACHE;
            return _vmm.MemReadString(_pid, addr, cb, Encoding.UTF8, flags) ??
                throw new VmmException("Memory Read Failed!");
        }

        /// <summary>
        /// Read null terminated Unicode string.
        /// </summary>
        public string ReadUnicodeString(ulong addr, int cb = 128, bool useCache = true)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(cb, 0x1000, nameof(cb));
            var flags = useCache ? VmmFlags.NONE : VmmFlags.NOCACHE;
            return _vmm.MemReadString(_pid, addr + 0x14, cb, Encoding.Unicode, flags) ??
                throw new VmmException("Memory Read Failed!");
        }

        #endregion

        #region Misc

        /// <summary>
        /// Creates a new <see cref="VmmScatterMap"/>.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VmmScatterMap CreateScatterMap() => 
            _vmm.CreateScatterMap(_pid);

        /// <summary>
        /// Creates a new <see cref="VmmScatter"/>.
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VmmScatter CreateScatter(VmmFlags flags = VmmFlags.NONE) =>
            _vmm.CreateScatter(_pid, flags);

        /// <summary>
        /// Throws a special exception if no longer in game.
        /// </summary>
        /// <exception cref="OperationCanceledException"></exception>
        public void ThrowIfProcessNotRunning()
        {
            _vmm.ForceFullRefresh();
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    if (!_vmm.PidGetFromName(GAME_PROCESS_NAME, out uint pid))
                        throw new InvalidOperationException();
                    if (pid != _pid)
                        throw new InvalidOperationException();
                    return;
                }
                catch
                {
                    Thread.Sleep(150);
                }
            }

            throw new OperationCanceledException("Process is not running!");
        }

        /// <summary>
        /// Get the Monitor Resolution from the Game Monitor.
        /// </summary>
        /// <returns>Monitor Resolution Result</returns>
        public Rectangle GetMonitorRes()
        {
            try
            {
                var gfx = ReadPtr(UnityBase + UnitySDK.ModuleBase.GfxDevice, false);
                var res = ReadValue<Rectangle>(gfx + UnitySDK.GfxDeviceClient.Viewport, false);
                if (res.Width <= 0 || res.Width > 10000 ||
                    res.Height <= 0 || res.Height > 5000)
                    throw new ArgumentOutOfRangeException(nameof(res));
                return res;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("ERROR Getting Game Monitor Res", ex);
            }
        }

        #endregion

        #region Memory Macros

        /// <summary>
        /// Checks if a Virtual Address is valid.
        /// </summary>
        /// <param name="va">Virtual Address to validate.</param>
        /// <returns>True if valid, otherwise False.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidVirtualAddress(ulong va)
        {
            return va >= 0x10000 && ((long)va << 16) >> 16 == (long)va;
        }

        /// <summary>
        /// The PAGE_ALIGN macro returns a page-aligned virtual address for a given virtual address.
        /// https://learn.microsoft.com/windows-hardware/drivers/ddi/wdm/nf-wdm-page_align
        /// </summary>
        /// <param name="va">Virtual address.</param>
        /// <returns>Page-aligned virtual address.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong PAGE_ALIGN(ulong va) => va & ~(0x1000ul - 1);

        /// <summary>
        /// The BYTE_OFFSET macro takes a virtual address and returns the byte offset of that address within the page.
        /// https://learn.microsoft.com/windows-hardware/drivers/ddi/wdm/nf-wdm-byte_offset
        /// </summary>
        /// <param name="va">virtual address.</param>
        /// <returns>Offset portion of the virtual address within the page.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint BYTE_OFFSET(ulong va) => (uint)(va & (0x1000ul - 1));

        /// <summary>
        /// The ADDRESS_AND_SIZE_TO_SPAN_PAGES macro returns the number of pages that a virtual range spans.
        /// The virtual range is defined by a virtual address and the size in bytes of a transfer request.
        /// https://learn.microsoft.com/windows-hardware/drivers/ddi/wdm/nf-wdm-address_and_size_to_span_pages
        /// </summary>
        /// <param name="va">Virtual address that is the base of the range.</param>
        /// <param name="size">Specifies the size in bytes.</param>
        /// <returns>Returns the number of pages spanned by the virtual range starting at Va.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ADDRESS_AND_SIZE_TO_SPAN_PAGES(ulong va, ulong size) =>
            (BYTE_OFFSET(va) + size + (0x1000ul - 1)) >> 12;

        /// <summary>
        /// Returns a length aligned to 8 bytes.
        /// Always rounds up.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint AlignLength(uint length) => (length + 7) & ~7u;

        /// <summary>
        /// Returns an address aligned to 8 bytes.
        /// Always the next aligned address.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong AlignAddress(ulong address) => (address + 7) & ~7ul;

        #endregion

        #region IDisposable

        private bool _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, true) == false)
            {
                _vmm.Dispose();
            }
        }

        #endregion
    }
}