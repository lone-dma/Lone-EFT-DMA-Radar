namespace EftDmaRadarLite.Misc.Workers
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

        public WorkerThread(TimeSpan? sleepDuration = null, ThreadPriority? threadPriority = null, string workerName = null)
        {
            if (sleepDuration is TimeSpan sleepDurationParam)
                SleepDuration = sleepDurationParam;
            if (threadPriority is ThreadPriority threadPriorityParam)
                ThreadPriority = threadPriorityParam;
            if (workerName is string workerNameParam)
                Name = workerNameParam;
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
            while (!_disposed)
            {
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
                    if (shouldSleep)
                        Thread.Sleep(SleepDuration);
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
