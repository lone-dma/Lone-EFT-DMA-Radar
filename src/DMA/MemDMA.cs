using eft_dma_radar.Tarkov.Player;
using eft_dma_radar.Tarkov.GameWorld;
using eft_dma_radar.Tarkov.GameWorld.Exits;
using eft_dma_radar.Tarkov.GameWorld.Explosives;
using eft_dma_radar.Tarkov.Loot;
using eft_dma_radar.Misc;
using eft_dma_radar.Unity;
using VmmSharpEx;
using System.Drawing;
using eft_dma_radar.Tarkov.Quests;
using VmmSharpEx.Refresh;
using VmmSharpEx.Options;
using VmmSharpEx.Scatter;

namespace eft_dma_radar.DMA
{
    /// <summary>
    /// DMA Memory Module.
    /// </summary>
    public sealed class MemDMA : IDisposable
    {
        #region Init

        private const string MEMORY_MAP_FILE = "mmap.txt";
        private const string GAME_PROCESS_NAME = "EscapeFromTarkov.exe";
        internal const uint MAX_READ_SIZE = (uint)0x1000 * 1500;
        private static readonly ManualResetEvent _syncProcessRunning = new(false);
        private static readonly ManualResetEvent _syncInRaid = new(false);
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

        public IReadOnlyCollection<PlayerBase> Players => Game?.Players;
        public IReadOnlyCollection<IExplosiveItem> Explosives => Game?.Explosives;
        public IReadOnlyCollection<IExitPoint> Exits => Game?.Exits;
        public LocalPlayer LocalPlayer => Game?.LocalPlayer;
        public LootManager Loot => Game?.Loot;
        public QuestManager QuestManager => Game?.QuestManager;
        public LocalGameWorld Game { get; private set; }

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
            Debug.WriteLine("New Game Startup");
            while (true) // Startup loop
            {
                try
                {
                    _vmm.ForceFullRefresh();
                    ResourceJanitor.Run();
                    LoadProcess();
                    LoadModules();
                    this.Starting = true;
                    MonoLib.InitializeEFT();
                    InputManager.Initialize(UnityBase);
                    CameraManager.Initialize();
                    this.Ready = true;
                    Debug.WriteLine("Game Startup [OK]");
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Game Startup [FAIL]: {ex}");
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
            MonoLib.Reset();
            InputManager.Reset();
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
            ArgumentOutOfRangeException.ThrowIfZero(unityBase, nameof(unityBase));
            var monoBase = _vmm.ProcessGetModuleBase(_pid, "mono-2.0-bdwgc.dll");
            ArgumentOutOfRangeException.ThrowIfZero(monoBase, nameof(monoBase));
            UnityBase = unityBase;
            MonoBase = monoBase;
        }

        #endregion

        #region Events

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
        /// Raises the ProcessStarted Event.
        /// </summary>
        private static void OnProcessStarted()
        {
            ProcessStarted?.Invoke(null, EventArgs.Empty);
            _syncProcessRunning.Set();
        }

        /// <summary>
        /// Raises the ProcessStopped Event.
        /// </summary>
        private static void OnProcessStopped()
        {
            ProcessStopped?.Invoke(null, EventArgs.Empty);
            _syncProcessRunning.Reset();
        }

        /// <summary>
        /// Raises the RaidStarted Event.
        /// </summary>
        private static void OnRaidStarted()
        {
            RaidStarted?.Invoke(null, EventArgs.Empty);
            _syncInRaid.Set();
        }

        /// <summary>
        /// Raises the RaidStopped Event.
        /// </summary>
        private static void OnRaidStopped()
        {
            RaidStopped?.Invoke(null, EventArgs.Empty);
            _syncInRaid.Reset();
        }

        /// <summary>
        /// Blocks indefinitely until the Game Process is Running, otherwise returns immediately.
        /// </summary>
        /// <returns>True if the Process is running, otherwise this method never returns.</returns>
        public static bool WaitForProcess() => _syncProcessRunning.WaitOne();

        /// <summary>
        /// Blocks indefinitely until In Raid/Match, otherwise returns immediately.
        /// </summary>
        /// <returns>True if In Raid/Match, otherwise this method never returns.</returns>
        public static bool WaitForRaid() => _syncInRaid.WaitOne();

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
            uint cb = (uint)(SizeChecker<T>.Size * span.Length);
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
            uint cb = (uint)(SizeChecker<T>.Size * span.Length);
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
        /// IMPORTANT: You must call <see cref="Dispose"/> on the returned SharedArray when done."/>
        /// </summary>
        /// <typeparam name="T">Value type to read.</typeparam>
        /// <param name="addr">Address to read from.</param>
        /// <param name="count">Number of array elements to read.</param>
        /// <param name="useCache">Use caching for this read.</param>
        /// <returns><see cref="SharedArray{T}"/> value.</returns>
        public SharedArray<T> ReadArray<T>(ulong addr, int count, bool useCache = true)
            where T : unmanaged
        {
            var arr = new SharedArray<T>(count);
            try
            {
                ReadSpan(
                    addr: addr,
                    span: arr.Span, 
                    useCache: useCache);
                return arr;
            }
            catch
            {
                arr.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Read a chain of pointers and get the final result.
        /// </summary>
        public ulong ReadPtrChain(ulong addr, uint[] offsets, bool useCache = true)
        {
            var pointer = addr; // push ptr to first address value
            for (var i = 0; i < offsets.Length; i++)
                pointer = ReadPtr(pointer + offsets[i], useCache);

            return pointer;
        }

        /// <summary>
        /// Resolves a pointer and returns the memory address it points to.
        /// </summary>
        public ulong ReadPtr(ulong addr, bool useCache = true)
        {
            var pointer = ReadValue<ulong>(addr, useCache);
            pointer.ThrowIfInvalidVirtualAddress();
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
            int cb = sizeof(T);
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
        /// Read null terminated string (utf-8/default).
        /// </summary>
        /// <param name="length">Number of bytes to read.</param>
        /// <exception cref="Exception"></exception>
        public string ReadString(ulong addr, int length, bool useCache = true) // read n bytes (string)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 0x1000, nameof(length));
            var flags = useCache ? VmmFlags.NONE : VmmFlags.NOCACHE;
            return _vmm.MemReadString(_pid, addr, (uint)length, Encoding.UTF8, flags) ??
                throw new VmmException("Memory Read Failed!");
        }

        /// <summary>
        /// Read UnityEngineString structure
        /// </summary>
        public string ReadUnityString(ulong addr, int length = 128, bool useCache = true)
        {
            if (length % 2 != 0)
                length++;
            ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 0x1000, nameof(length));
            var flags = useCache ? VmmFlags.NONE : VmmFlags.NOCACHE;
            return _vmm.MemReadString(_pid, addr + 0x14, (uint)length, Encoding.Unicode, flags) ??
                throw new VmmException("Memory Read Failed!");
        }

        #endregion

        #region Misc
        /// <summary>
        /// Get a new ScatterReadMap instance for performing batched reads.
        /// </summary>
        /// <returns></returns>
        public ScatterReadMap GetScatterMap() => new(_vmm, _pid);

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
                var gfx = ReadPtr(UnityBase + UnityOffsets.ModuleBase.GfxDevice, false);
                var res = ReadValue<Rectangle>(gfx + UnityOffsets.GfxDeviceClient.Viewport, false);
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
            if (va < 0x100000 || va >= 0x7FFFFFFFFFFF)
                return false;
            return true;
        }

        /// <summary>
        /// The PAGE_ALIGN macro takes a virtual address and returns a page-aligned
        /// virtual address for that page.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong PAGE_ALIGN(ulong va) => va & ~(0x1000ul - 1);

        /// <summary>
        /// The ADDRESS_AND_SIZE_TO_SPAN_PAGES macro takes a virtual address and size and returns the number of pages spanned by
        /// the size.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ADDRESS_AND_SIZE_TO_SPAN_PAGES(ulong va, uint size) =>
            (uint)(BYTE_OFFSET(va) + size + (0x1000ul - 1) >> 12);

        /// <summary>
        /// The BYTE_OFFSET macro takes a virtual address and returns the byte offset
        /// of that address within the page.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint BYTE_OFFSET(ulong va) => (uint)(va & 0x1000ul - 1);

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