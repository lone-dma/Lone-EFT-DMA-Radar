/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
namespace LoneEftDmaRadar.UI.Panels
{
    /// <summary>
    /// Radar Menu Panel ( top bar )
    /// </summary>
    internal static class RadarMenuPanel
    {
        private static EftDmaConfig Config { get; } = Program.Config;

        /// <summary>
        /// Draw the overlay controls at the top of the radar.
        /// </summary>
        public static void Draw()
        {
            // Intentionally empty - overlay controls moved into the main menu bar.
        }
    }
}

