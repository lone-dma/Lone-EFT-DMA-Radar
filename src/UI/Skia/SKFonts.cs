/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
namespace LoneEftDmaRadar.UI.Skia
{
    internal static class SKFonts
    {
        /// <summary>
        /// Regular body font (size 12) with default typeface.
        /// </summary>
        public static SKFont UIRegular { get; } = new SKFont(CustomFonts.NeoSansStdRegular, 12f)
        {
            Subpixel = true,
            Edging = SKFontEdging.SubpixelAntialias
        };
        /// <summary>
        /// Large header font (size 48) for radar status.
        /// </summary>
        public static SKFont UILarge { get; } = new SKFont(CustomFonts.NeoSansStdRegular, 48f)
        {
            Subpixel = true,
            Edging = SKFontEdging.SubpixelAntialias
        };
        /// <summary>
        /// Regular body font (size 9) with default typeface.
        /// </summary>
        public static SKFont AimviewWidgetFont { get; } = new SKFont(CustomFonts.NeoSansStdRegular, 9f)
        {
            Subpixel = true,
            Edging = SKFontEdging.SubpixelAntialias
        };
    }
}

