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

namespace LoneEftDmaRadar.Misc.Workers
{
    public sealed class WorkerThread : IDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly WorkerThreadArgs _args;
        private bool _started;

        /// <summary>
        /// Subscribe to this event to perform work on the worker thread.
        /// </summary>
        public event EventHandler<WorkerThreadArgs> PerformWork;
        void OnPerformWork() => PerformWork?.Invoke(this, _args);

        /// <summary>
        /// Sleep Duration for the worker thread. The thread will sleep for this duration after each work cycle.
        /// If no Sleep Duration is set, the thread will not sleep and will run continuously.
        /// </summary>
        public TimeSpan SleepDuration { get; init; } = TimeSpan.Zero;
        /// <summary>
        /// Thread priority for the Worker Thread.
        /// </summary>
        public ThreadPriority ThreadPriority { get; init; } = ThreadPriority.Normal;
        /// <summary>
        /// Worker Name/Label.
        /// </summary>
        public string Name { get; init; } = Guid.NewGuid().ToString();
        /// <summary>
        /// Defines how the worker thread should sleep between work cycles.
        /// </summary>
        public WorkerThreadSleepMode SleepMode { get; init; } = WorkerThreadSleepMode.Default;

        public WorkerThread() : this(null, null, null, null) { }

        public WorkerThread(TimeSpan? sleepDuration = null, ThreadPriority? threadPriority = null, string workerName = null, WorkerThreadSleepMode? sleepMode = null)
        {
            if (sleepDuration is TimeSpan sleepDurationParam)
                SleepDuration = sleepDurationParam;
            if (threadPriority is ThreadPriority threadPriorityParam)
                ThreadPriority = threadPriorityParam;
            if (workerName is string workerNameParam)
                Name = workerNameParam;
            if (sleepMode is WorkerThreadSleepMode sleepModeParam)
                SleepMode = sleepModeParam;
            _args = new(_cts.Token);
        }

        /// <summary>
        /// Start the worker thread.
        /// </summary>
        public void Start()
        {
            if (Interlocked.Exchange(ref _started, true) == false)
            {
                new Thread(Worker)
                {
                    IsBackground = true,
                    Priority = ThreadPriority,
                    Name = Name
                }.Start();
            }
        }

        private void Worker()
        {
            Debug.WriteLine($"[WorkerThread] '{Name}' thread starting...");
            bool shouldSleep = SleepDuration > TimeSpan.Zero;
            bool shouldDynamicSleep = shouldSleep && SleepMode == WorkerThreadSleepMode.DynamicSleep;
            while (!_disposed)
            {
                long start = shouldDynamicSleep ?
                    Stopwatch.GetTimestamp() : default;
                try
                {
                    OnPerformWork();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[WorkerThread] WARNING: Unhandled exception on '{Name}' thread: {ex}");
                }
                finally
                {
                    if (shouldDynamicSleep)
                    {
                        long end = Stopwatch.GetTimestamp();
                        var duration = SleepDuration - TimeSpan.FromTicks(end - start);
                        if (duration > TimeSpan.Zero)
                        {
                            Thread.Sleep(duration);
                        }
                    }
                    else if (shouldSleep)
                    {
                        Thread.Sleep(SleepDuration);
                    }    
                }
            }
            Debug.WriteLine($"[WorkerThread] '{Name}' thread stopping...");
        }

        #region IDisposable

        private bool _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, true) == false)
            {
                PerformWork = null;
                _cts.Cancel();
                _cts.Dispose();
            }
        }

        #endregion
    }
}
