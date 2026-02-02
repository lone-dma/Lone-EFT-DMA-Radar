/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
namespace LoneEftDmaRadar
{
    /// <summary>
    /// Custom enumeration representing the various states of the application.
    /// </summary>
    public enum AppState
    {
        /// <summary>
        /// Application is initializing on first startup.
        /// </summary>
        Initializing,
        /// <summary>
        /// Application is initialized, but the Game Process is not started.
        /// </summary>
        ProcessNotStarted,
        /// <summary>
        /// Application is starting the Game Process.
        /// </summary>
        ProcessStarting,
        /// <summary>
        /// Application is waiting for the player to enter a raid.
        /// </summary>
        WaitingForRaid,
        /// <summary>
        /// Application is currently in a raid.
        /// </summary>
        InRaid
    }
}

