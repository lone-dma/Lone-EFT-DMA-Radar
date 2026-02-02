/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
namespace LoneEftDmaRadar.Misc.Workers
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

