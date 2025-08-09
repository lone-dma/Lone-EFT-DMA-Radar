namespace eft_dma_radar.Misc.Workers
{
    /// <summary>
    /// Contains arguments for Worker Thread API.
    /// </summary>
    public sealed class WorkerThreadArgs : EventArgs
    {
        public WorkerThreadArgs(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
        }
        /// <summary>
        /// Cancellation Token for this Thread. When the object is disposed this token will be cancelled to signal the thread to stop.
        /// </summary>
        public CancellationToken CancellationToken { get; }
    }
}
