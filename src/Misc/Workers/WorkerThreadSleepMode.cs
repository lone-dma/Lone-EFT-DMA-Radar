namespace EftDmaRadarLite.Misc.Workers
{
    /// <summary>
    /// Defines how a worker thread should sleep between work cycles.
    /// </summary>
    public enum WorkerThreadSleepMode
    {
        /// <summary>
        /// The worker will sleep for the specified Sleep Duration.
        /// </summary>
        Default,
        /// <summary>
        /// The worker will sleep for the spcecified Sleep Duration minus the time taken to perform work.
        /// </summary>
        DynamicSleep
    }
}
