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

using EftDmaRadarLite.DMA;
using System.Runtime;

namespace EftDmaRadarLite.Common
{
    internal static partial class ResourceJanitor
    {
        private static readonly Lock _sync = new();

        static ResourceJanitor()
        {
            MemDMA.RaidStarted += MemDMA_RaidStarted;
            MemDMA.RaidStopped += MemDMA_RaidStopped;
            _ = Task.Run(WorkerRoutineAsync);
        }

        private static void MemDMA_RaidStarted(object sender, EventArgs e)
        {
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
        }

        private static void MemDMA_RaidStopped(object sender, EventArgs e)
        {
            GCSettings.LatencyMode = GCLatencyMode.Interactive;
        }

        private static async Task WorkerRoutineAsync()
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
                finally { await Task.Delay(TimeSpan.FromSeconds(5)); }
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
