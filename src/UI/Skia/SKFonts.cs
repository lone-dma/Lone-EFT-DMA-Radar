namespace eft_dma_radar.UI.Skia
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
        /// Regular body font (size 12) with Consolas typeface.
        /// </summary>
        public static SKFont InfoWidgetFont { get; } = new SKFont(SKTypeface.FromFamilyName("Consolas"), 12f)
        {
            Subpixel = true,
            Edging = SKFontEdging.SubpixelAntialias
        };
        /// <summary>
        /// Regular body font (size 9) with default typeface.
        /// </summary>
        public static SKFont EspWidgetFont { get; } = new SKFont(CustomFonts.NeoSansStdRegular, 9f)
        {
            Subpixel = true,
            Edging = SKFontEdging.SubpixelAntialias
        };
    }
}
