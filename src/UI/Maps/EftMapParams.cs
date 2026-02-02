/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
namespace LoneEftDmaRadar.UI.Maps
{
    /// <summary>
    /// Contains multiple map parameters used by the GUI.
    /// </summary>
    public sealed class EftMapParams
    {
        /// <summary>
        /// Currently loaded Map File.
        /// </summary>
        public EftMapConfig Map { get; init; }
        /// <summary>
        /// Rectangular 'zoomed' bounds of the Bitmap to display.
        /// </summary>
        public SKRect Bounds { get; init; }
        /// <summary>
        /// Regular -> Zoomed 'X' Scale correction.
        /// </summary>
        public float XScale { get; init; }
        /// <summary>
        /// Regular -> Zoomed 'Y' Scale correction.
        /// </summary>
        public float YScale { get; init; }
    }
}

