using eft_dma_radar.DMA;
using System.Runtime;

namespace eft_dma_radar.Common
{
    internal static partial class ResourceJanitor
    {
        private static readonly Lock _sync = new();

        static ResourceJanitor()
        {
            MemDMA.RaidStarted += MemDMA_RaidStarted;
            MemDMA.RaidStopped += MemDMA_RaidStopped;
            new Thread(Worker)
            {
                Priority = ThreadPriority.Lowest,
                IsBackground = true
            }.Start();
        }

        private static void MemDMA_RaidStarted(object sender, EventArgs e)
        {
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
        }

        private static void MemDMA_RaidStopped(object sender, EventArgs e)
        {
            GCSettings.LatencyMode = GCLatencyMode.Interactive;
        }

        private static void Worker()
        {
            while (true)
            {
                try
                {
                    var info = new MEMORYSTATUSEX();
                    if (GlobalMemoryStatusEx(ref info) && info.dwMemoryLoad >= 92) // Over 92% memory usage
                    {
                        Debug.WriteLine("[ResourceJanitor] High Memory Load, running cleanup...");
                        Run(false);
                    }
                }
                catch { }
                finally { Thread.Sleep(TimeSpan.FromSeconds(5)); }
            }
        }

        /// <summary>
        /// Runs resource cleanup on the app.
        /// </summary>
        public static void Run(bool aggressive = true)
        {
            lock (_sync)
            {
                try
                {
                    MainWindow.Instance?.Radar?.ViewModel?.PurgeSKResources();
                    if (aggressive)
                    {
                        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                        GC.Collect(
                            generation: GC.MaxGeneration,
                            mode: GCCollectionMode.Aggressive,
                            blocking: true,
                            compacting: true);
                    }
                    else
                    {
                        GC.Collect(
                            generation: GC.MaxGeneration,
                            mode: GCCollectionMode.Optimized,
                            blocking: false,
                            compacting: false);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ResourceJanitor ERROR: {ex}");
                }
            }
        }

        #region Native Interop
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private readonly struct MEMORYSTATUSEX
        {
            public readonly uint dwLength;
            public readonly uint dwMemoryLoad;
            public readonly ulong ullTotalPhys;
            public readonly ulong ullAvailPhys;
            public readonly ulong ullTotalPageFile;
            public readonly ulong ullAvailPageFile;
            public readonly ulong ullTotalVirtual;
            public readonly ulong ullAvailVirtual;
            public readonly ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
            }
        }

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
        #endregion
    }
}
