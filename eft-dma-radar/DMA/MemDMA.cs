using eft_dma_radar.Tarkov.Player;
using eft_dma_radar.Tarkov.GameWorld;
using eft_dma_radar.Tarkov.GameWorld.Exits;
using eft_dma_radar.Tarkov.GameWorld.Explosives;
using eft_dma_radar.Tarkov.Loot;
using eft_dma_radar.DMA.ScatterAPI;
using eft_dma_radar.Misc;
using eft_dma_radar.Unity;
using Vmmsharp;
using System.Drawing;
using eft_dma_radar.Tarkov.Quests;

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
        private const uint MAX_READ_SIZE = (uint)0x1000 * 1500;
        private static readonly ManualResetEvent _syncProcessRunning = new(false);
        private static readonly ManualResetEvent _syncInRaid = new(false);
        private readonly Vmm _hVMM;
        private VmmProcess _proc;
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
                        _hVMM = new Vmm(
                            initializePlugins: false,
                            args: initArgs);
                        var map = _hVMM.MapMemoryAsString() ??
                            throw new InvalidOperationException("MapMemoryAsString FAIL");
                        var mapBytes = Encoding.ASCII.GetBytes(map);
                        if (!_hVMM.LeechCore.Command(LeechCore.LC_CMD_MEMMAP_SET, mapBytes, out _))
                            throw new InvalidOperationException("LC_CMD_MEMMAP_SET FAIL");
                        File.WriteAllBytes(MEMORY_MAP_FILE, mapBytes);
                    }
                    else
                    {
                        var mapArgs = new[] { "-memmap", MEMORY_MAP_FILE };
                        initArgs = initArgs.Concat(mapArgs).ToArray();
                    }
                }
                _hVMM ??= new Vmm(
                            initializePlugins: false,
                            args: initArgs);
                SetupVmmRefresh();
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
                    ForceFullRefresh();
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
            _proc = default;
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
            
            if (_hVMM.GetProcessByName(GAME_PROCESS_NAME) is not VmmProcess proc)
                throw new InvalidOperationException($"Unable to find '{GAME_PROCESS_NAME}'");
            _proc = proc;
        }

        /// <summary>
        /// Gets the Game Process Base Module Addresses.
        /// </summary>
        private void LoadModules()
        {
            var unityBase = _proc.GetModuleBase("UnityPlayer.dll");
            ArgumentOutOfRangeException.ThrowIfZero(unityBase, nameof(unityBase));
            var monoBase = _proc.GetModuleBase("mono-2.0-bdwgc.dll");
            ArgumentOutOfRangeException.ThrowIfZero(monoBase, nameof(monoBase));
            UnityBase = unityBase;
            MonoBase = monoBase;
        }

        #endregion

        #region Vmm Refresh

        private readonly System.Timers.Timer _memCacheRefreshTimer = new(TimeSpan.FromMilliseconds(300));
        private readonly System.Timers.Timer _tlbRefreshTimer = new(TimeSpan.FromSeconds(2));

        /// <summary>
        /// Register Vmm Refreshing (we only refresh what/when we want)
        /// </summary>
        private void SetupVmmRefresh()
        {
            _memCacheRefreshTimer.Elapsed += MemCacheRefreshTimer_Elapsed;
            _tlbRefreshTimer.Elapsed += TlbRefreshTimer_Elapsed;
            _memCacheRefreshTimer.Start();
            _tlbRefreshTimer.Start();
        }

        private void MemCacheRefreshTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!_hVMM.SetConfig(Vmm.CONFIG_OPT_REFRESH_FREQ_MEM_PARTIAL, 1))
                Debug.WriteLine("WARNING: Vmm MEM CACHE Refresh (Partial) Failed!");
        }

        private void TlbRefreshTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!_hVMM.SetConfig(Vmm.CONFIG_OPT_REFRESH_FREQ_TLB_PARTIAL, 1))
                Debug.WriteLine("WARNING: Vmm TLB Refresh (Partial) Failed!");
        }

        /// <summary>
        /// Manually Force a Full Vmm Refresh.
        /// </summary>
        private void ForceFullRefresh()
        {
            if (!_hVMM.SetConfig(Vmm.CONFIG_OPT_REFRESH_ALL, 1))
                Debug.WriteLine("WARNING: Vmm FULL Refresh Failed!");
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

        #region Scatter Read

        /// <summary>
        /// Performs multiple reads in one sequence, significantly faster than single reads.
        /// Designed to run without throwing unhandled exceptions, which will ensure the maximum amount of
        /// reads are completed OK even if a couple fail.
        /// </summary>
        public void ReadScatter(IScatterEntry[] entries, bool useCache = true)
        {
            if (entries.Length == 0)
                return;
            var pagesToRead = new HashSet<ulong>(entries.Length); // Will contain each unique page only once to prevent reading the same page multiple times
            foreach (var entry in entries) // First loop through all entries - GET INFO
            {
                // INTEGRITY CHECK - Make sure the read is valid and within range
                if (entry.Address == 0x0 || entry.CB == 0 || (uint)entry.CB > MAX_READ_SIZE)
                {
                    //Debug.WriteLine($"[Scatter Read] Out of bounds read @ 0x{entry.Address.ToString("X")} ({entry.CB})");
                    entry.IsFailed = true;
                    continue;
                }

                // get the number of pages
                uint numPages = ADDRESS_AND_SIZE_TO_SPAN_PAGES(entry.Address, (uint)entry.CB);
                ulong basePage = PAGE_ALIGN(entry.Address);

                //loop all the pages we would need
                for (int p = 0; p < numPages; p++)
                {
                    ulong page = basePage + 0x1000 * (uint)p;
                    pagesToRead.Add(page);
                }
            }
            if (pagesToRead.Count == 0)
                return;

            uint flags = useCache ? 0 : Vmm.FLAG_NOCACHE;
            using var hScatter = _proc.MemReadScatter2(flags, pagesToRead.ToArray());

            foreach (var entry in entries) // Second loop through all entries - PARSE RESULTS
            {
                if (entry.IsFailed)
                    continue;
                entry.SetResult(hScatter);
            }
        }

        #endregion

        #region Read Methods

        /// <summary>
        /// Prefetch pages into the cache.
        /// </summary>
        /// <param name="va"></param>
        public void ReadCache(params ulong[] va)
        {
            _proc.MemPrefetchPages(va);
        }

        /// <summary>
        /// Read memory into a Buffer of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">Value Type <typeparamref name="T"/></typeparam>
        /// <param name="addr">Virtual Address to read from.</param>
        /// <param name="buffer">Buffer to receive memory read in.</param>
        /// <param name="useCache">Use caching for this read.</param>
        public void ReadBuffer<T>(ulong addr, Span<T> buffer, bool useCache = true, bool allowPartialRead = false)
            where T : unmanaged
        {
            uint cb = (uint)(SizeChecker<T>.Size * buffer.Length);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(cb, MAX_READ_SIZE, nameof(cb));
            uint flags = useCache ? 0 : Vmm.FLAG_NOCACHE;

            if (!_proc.MemReadSpan(addr, buffer, out uint cbRead, flags))
                throw new VmmException("Memory Read Failed!");

            if (cbRead == 0)
                throw new VmmException("Memory Read Failed!");
            if (!allowPartialRead && cbRead != cb)
                throw new VmmException("Memory Read Failed!");
        }

        /// <summary>
        /// Read memory into a Buffer of type <typeparamref name="T"/> and ensure the read is correct.
        /// </summary>
        /// <typeparam name="T">Value Type <typeparamref name="T"/></typeparam>
        /// <param name="addr">Virtual Address to read from.</param>
        /// <param name="buffer1">Buffer to receive memory read in.</param>
        /// <param name="useCache">Use caching for this read.</param>
        public void ReadBufferEnsure<T>(ulong addr, Span<T> buffer1)
            where T : unmanaged
        {
            uint cb = (uint)(SizeChecker<T>.Size * buffer1.Length);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(cb, MAX_READ_SIZE, nameof(cb));
            var buffer2 = new T[buffer1.Length].AsSpan();
            var buffer3 = new T[buffer1.Length].AsSpan();
            uint cbRead;
            if (!_proc.MemReadSpan(addr, buffer3, out cbRead, Vmm.FLAG_NOCACHE))
                throw new VmmException("Memory Read Failed!");
            if (cbRead != cb)
                throw new VmmException("Memory Read Failed!");
            Thread.SpinWait(5);
            if (!_proc.MemReadSpan(addr, buffer2, out cbRead, Vmm.FLAG_NOCACHE))
                throw new VmmException("Memory Read Failed!");
            if (cbRead != cb)
                throw new VmmException("Memory Read Failed!");
            Thread.SpinWait(5);
            if (!_proc.MemReadSpan(addr, buffer1, out cbRead, Vmm.FLAG_NOCACHE))
                throw new VmmException("Memory Read Failed!");
            if (cbRead != cb)
                throw new VmmException("Memory Read Failed!");
            if (!buffer1.SequenceEqual(buffer2) || !buffer1.SequenceEqual(buffer3) || !buffer2.SequenceEqual(buffer3))
            {
                throw new VmmException("Memory Read Failed!");
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
            uint flags = useCache ? 0 : Vmm.FLAG_NOCACHE;
            if (!_proc.MemReadRefAs<T>(addr, out var result, flags))
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
            if (!_proc.MemReadRefAs<T>(addr, out var r1, Vmm.FLAG_NOCACHE))
                throw new VmmException("Memory Read Failed!");
            Thread.SpinWait(5);
            if (!_proc.MemReadRefAs<T>(addr, out var r2, Vmm.FLAG_NOCACHE))
                throw new VmmException("Memory Read Failed!");
            Thread.SpinWait(5);
            if (!_proc.MemReadRefAs<T>(addr, out var r3, Vmm.FLAG_NOCACHE))
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
        /// Read byref value type/struct from specified address multiple times to ensure the read is correct.
        /// </summary>
        /// <typeparam name="T">Specified Value Type.</typeparam>
        /// <param name="addr">Address to read from.</param>
        public unsafe void ReadValueEnsure<T>(ulong addr, out T result)
            where T : unmanaged, allows ref struct
        {
            int cb = sizeof(T);
            if (!_proc.MemReadRefAs<T>(addr, out var r1, Vmm.FLAG_NOCACHE))
                throw new VmmException("Memory Read Failed!");
            Thread.SpinWait(5);
            if (!_proc.MemReadRefAs<T>(addr, out var r2, Vmm.FLAG_NOCACHE))
                throw new VmmException("Memory Read Failed!");
            Thread.SpinWait(5);
            if (!_proc.MemReadRefAs<T>(addr, out var r3, Vmm.FLAG_NOCACHE))
                throw new VmmException("Memory Read Failed!");
            var b1 = new ReadOnlySpan<byte>(&r1, cb);
            var b2 = new ReadOnlySpan<byte>(&r2, cb);
            var b3 = new ReadOnlySpan<byte>(&r3, cb);
            if (!b1.SequenceEqual(b2) || !b1.SequenceEqual(b3) || !b2.SequenceEqual(b3))
            {
                throw new VmmException("Memory Read Failed!");
            }
            result = r1;
        }

        /// <summary>
        /// Read null terminated string (utf-8/default).
        /// </summary>
        /// <param name="length">Number of bytes to read.</param>
        /// <exception cref="Exception"></exception>
        public string ReadString(ulong addr, int length, bool useCache = true) // read n bytes (string)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 0x1000, nameof(length));
            Span<byte> buffer = stackalloc byte[length];
            buffer.Clear();
            ReadBuffer(addr, buffer, useCache, true);
            var nullIndex = buffer.IndexOf((byte)0);
            return nullIndex >= 0
                ? Encoding.UTF8.GetString(buffer.Slice(0, nullIndex))
                : Encoding.UTF8.GetString(buffer);
        }

        /// <summary>
        /// Read UnityEngineString structure
        /// </summary>
        public string ReadUnityString(ulong addr, int length = 64, bool useCache = true)
        {
            if (length % 2 != 0)
                length++;
            length *= 2; // Unicode 2 bytes per char
            ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 0x1000, nameof(length));
            Span<byte> buffer = stackalloc byte[length];
            buffer.Clear();
            ReadBuffer(addr + 0x14, buffer, useCache, true);
            var nullIndex = buffer.FindUtf16NullTerminatorIndex();
            return nullIndex >= 0
                ? Encoding.Unicode.GetString(buffer.Slice(0, nullIndex))
                : Encoding.Unicode.GetString(buffer);
        }

        #endregion

        #region Misc

        /// <summary>
        /// Throws a special exception if no longer in game.
        /// </summary>
        /// <exception cref="OperationCanceledException"></exception>
        public void ThrowIfProcessNotRunning()
        {
            ForceFullRefresh();
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    if (_hVMM.GetProcessByName(GAME_PROCESS_NAME) is not VmmProcess proc)
                        continue;
                    if (proc.PID != _proc.PID)
                        continue;
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
                _hVMM.Dispose();
            }
        }

        #endregion
    }
}