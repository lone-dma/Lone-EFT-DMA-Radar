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

global using LoneEftDmaRadar.DMA;
using Collections.Pooled;
using LoneEftDmaRadar.Misc;
using LoneEftDmaRadar.Tarkov.GameWorld;
using LoneEftDmaRadar.Tarkov.GameWorld.Exits;
using LoneEftDmaRadar.Tarkov.GameWorld.Explosives;
using LoneEftDmaRadar.Tarkov.GameWorld.Loot;
using LoneEftDmaRadar.Tarkov.GameWorld.Player;
using LoneEftDmaRadar.Tarkov.Unity.Structures;
using VmmSharpEx;
using VmmSharpEx.Extensions;
using VmmSharpEx.Options;
using VmmSharpEx.Refresh;
using VmmSharpEx.Scatter;

namespace LoneEftDmaRadar.DMA
{
    /// <summary>
    /// DMA Memory Module.
    /// </summary>
    internal static class Memory
    {
        #region Init

        private const string GAME_PROCESS_NAME = "EscapeFromTarkov.exe";
        internal const uint MAX_READ_SIZE = 0x1000u * 1500u;
        private static readonly string _mmap = Path.Combine(App.ConfigPath.FullName, "mmap.txt");
        private static Vmm _vmm;
        private static InputManager _input;
        private static uint _pid;

        public static string MapID => Game?.MapID;
        public static ulong UnityBase { get; private set; }
        public static ulong GOM { get; private set; }
        public static bool Starting { get; private set; }
        public static bool Ready { get; private set; }
        public static bool InRaid => Game?.InRaid ?? false;

        public static IReadOnlyCollection<AbstractPlayer> Players => Game?.Players;
        public static IReadOnlyCollection<IExplosiveItem> Explosives => Game?.Explosives;
        public static IReadOnlyCollection<IExitPoint> Exits => Game?.Exits;
        public static LocalPlayer LocalPlayer => Game?.LocalPlayer;
        public static LootManager Loot => Game?.Loot;
        public static LocalGameWorld Game { get; private set; }

        internal static async Task ModuleInitAsync()
        {
            await Task.Run(() =>
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
                        if (!File.Exists(_mmap))
                        {
                            Debug.WriteLine("[DMA] No MemMap, attempting to generate...");
                            _vmm = new Vmm(args: initArgs)
                            {
                                EnableMemoryWriting = false
                            };
                            _ = _vmm.GetMemoryMap(
                                applyMap: true,
                                outputFile: _mmap);
                        }
                        else
                        {
                            var mapArgs = new[] { "-memmap", _mmap };
                            initArgs = initArgs.Concat(mapArgs).ToArray();
                        }
                    }
                    _vmm ??= new Vmm(args: initArgs)
                    {
                        EnableMemoryWriting = false
                    };
                    AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
                    _vmm.RegisterAutoRefresh(RefreshOption.MemoryPartial, TimeSpan.FromMilliseconds(300));
                    _vmm.RegisterAutoRefresh(RefreshOption.TlbPartial, TimeSpan.FromSeconds(2));
                    try
                    {
                        _input = new(_vmm);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            messageBoxText: $"WARNING: Failed to initialize InputManager (win32). Please note, this only works on Windows 11 (Game PC). Startup will continue without hotkeys.\n\n{ex}",
                            caption: App.Name,
                            button: MessageBoxButton.OK,
                            icon: MessageBoxImage.Warning,
                            defaultResult: MessageBoxResult.OK,
                            options: MessageBoxOptions.DefaultDesktopOnly);
                    }
                    ProcessStopped += MemDMA_ProcessStopped;
                    RaidStopped += MemDMA_RaidStopped;
                    // Start Memory Thread after successful startup
                    new Thread(MemoryPrimaryWorker)
                    {
                        IsBackground = true
                    }.Start();
                    Debug.WriteLine("DMA Initialized!");
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
            });
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            _vmm.Dispose();
            _vmm = null;
        }

        /// <summary>
        /// Main worker thread to perform DMA Reads on.
        /// </summary>
        private static void MemoryPrimaryWorker()
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

        #region Restart Radar

        private static readonly Lock _restartSync = new();
        private static CancellationTokenSource _cts = new();

        /// <summary>
        /// Signal the Radar to restart the raid/game loop.
        /// </summary>
        public static void RestartRadar()
        {
            lock (_restartSync)
            {
                var old = Interlocked.Exchange(ref _cts, new());
                old.Cancel();
                old.Dispose();
            }
        }

        #endregion

        #region Startup / Main Loop

        /// <summary>
        /// Starts up the Game Process and all mandatory modules.
        /// Returns to caller when the Game is ready.
        /// </summary>
        private static void RunStartupLoop()
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
                    Starting = true;
                    OnProcessStarting();
                    Ready = true;
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
        private static void RunGameLoop()
        {
            while (true)
            {
                try
                {
                    var ct = _cts.Token;
                    using (var game = Game = LocalGameWorld.CreateGameInstance(ct))
                    {
                        OnRaidStarted();
                        game.Start();
                        while (game.InRaid)
                        {
                            ct.ThrowIfCancellationRequested();
                            game.Refresh();
                            Thread.Sleep(133);
                        }
                    }
                }
                catch (OperationCanceledException ex) // Restart Radar
                {
                    Debug.WriteLine(ex.Message);
                    continue;
                }
                catch (ProcessNotRunningException ex) // Process Closed
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
        private static void MemDMA_ProcessStopped(object sender, EventArgs e)
        {
            Starting = default;
            Ready = default;
            UnityBase = default;
            GOM = default;
            _pid = default;
        }


        private static void MemDMA_RaidStopped(object sender, EventArgs e)
        {
            Game = null;
        }

        /// <summary>
        /// Obtain the PID for the Game Process.
        /// </summary>
        private static void LoadProcess()
        {

            if (!_vmm.PidGetFromName(GAME_PROCESS_NAME, out uint pid))
                throw new InvalidOperationException($"Unable to find '{GAME_PROCESS_NAME}'");
            _pid = pid;
        }

        /// <summary>
        /// Gets the Game Process Base Module Addresses.
        /// </summary>
        private static void LoadModules()
        {
            var unityBase = _vmm.ProcessGetModuleBase(_pid, "UnityPlayer.dll");
            unityBase.ThrowIfInvalidUserVA(nameof(unityBase));
            GOM = GameObjectManager.GetAddr(unityBase);
            UnityBase = unityBase;
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
        public static void ReadCache(params ulong[] va)
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
        public static void ReadSpan<T>(ulong addr, Span<T> span, bool useCache = true)
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
        public static void ReadSpanEnsure<T>(ulong addr, Span<T> span)
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
        public static IMemoryOwner<T> ReadPooled<T>(ulong addr, int count, bool useCache = true)
            where T : unmanaged
        {
            var flags = useCache ? VmmFlags.NONE : VmmFlags.NOCACHE;
            var arr = _vmm.MemReadPooled<T>(_pid, addr, count, flags) ??
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
        public static ulong ReadPtrChain(ulong addr, bool useCache, params Span<uint> offsets)
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
        public static ulong ReadPtr(ulong addr, bool useCache = true)
        {
            var pointer = ReadValue<VmmPointer>(addr, useCache);
            pointer.ThrowIfInvalidUserVA();
            return pointer;
        }

        /// <summary>
        /// Read value type/struct from specified address.
        /// </summary>
        /// <typeparam name="T">Specified Value Type.</typeparam>
        /// <param name="addr">Address to read from.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadValue<T>(ulong addr, bool useCache = true)
            where T : unmanaged, allows ref struct
        {
            var flags = useCache ? VmmFlags.NONE : VmmFlags.NOCACHE;
            return _vmm.MemReadValue<T>(_pid, addr, flags);
        }

        /// <summary>
        /// Read value type/struct from specified address multiple times to ensure the read is correct.
        /// </summary>
        /// <typeparam name="T">Specified Value Type.</typeparam>
        /// <param name="addr">Address to read from.</param>
        public static unsafe T ReadValueEnsure<T>(ulong addr)
            where T : unmanaged, allows ref struct
        {
            int cb = Unsafe.SizeOf<T>();
            T r1 = _vmm.MemReadValue<T>(_pid, addr, VmmFlags.NOCACHE);
            Thread.SpinWait(5);
            T r2 = _vmm.MemReadValue<T>(_pid, addr, VmmFlags.NOCACHE);
            Thread.SpinWait(5);
            T r3 = _vmm.MemReadValue<T>(_pid, addr, VmmFlags.NOCACHE);
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
        public static string ReadUtf8String(ulong addr, int cb, bool useCache = true) // read n bytes (string)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(cb, 0x1000, nameof(cb));
            var flags = useCache ? VmmFlags.NONE : VmmFlags.NOCACHE;
            return _vmm.MemReadString(_pid, addr, cb, Encoding.UTF8, flags) ??
                throw new VmmException("Memory Read Failed!");
        }

        /// <summary>
        /// Read null terminated Unity string (Unicode Encoding).
        /// </summary>
        public static string ReadUnityString(ulong addr, int cb = 128, bool useCache = true)
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
        public static VmmScatterMap CreateScatterMap() =>
            _vmm.CreateScatterMap(_pid);

        /// <summary>
        /// Creates a new <see cref="VmmScatter"/>.
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VmmScatter CreateScatter(VmmFlags flags = VmmFlags.NONE) =>
            _vmm.CreateScatter(_pid, flags);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong FindSignature(string signature)
        {
            return _vmm.FindSignature(_pid, signature, "UnityPlayer.dll");
        }

        /// <summary>
        /// Throws a special exception if no longer in game.
        /// </summary>
        /// <exception cref="ProcessNotRunningException"></exception>
        public static void ThrowIfProcessNotRunning()
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

            throw new ProcessNotRunningException();
        }

        private sealed class ProcessNotRunningException : Exception
        {
            public ProcessNotRunningException()
                : base("Process is not running!")
            {
            }
        }

        #endregion
    }
}