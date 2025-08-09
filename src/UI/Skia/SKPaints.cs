namespace eft_dma_radar.UI.Skia
{
    internal static class SKPaints
    {
        /// <summary>
        /// Gets an SKColorFilter that will reduce an image's brightness level.
        /// </summary>
        /// <param name="brightnessFactor">Adjust this value between 0 (black) and 1 (original brightness), where values less than 1 reduce brightness</param>
        /// <returns>SKColorFilter Object.</returns>
        public static SKColorFilter GetDarkModeColorFilter(float brightnessFactor)
        {
            float[] colorMatrix = {
                brightnessFactor, 0, 0, 0, 0, // Red channel
                0, brightnessFactor, 0, 0, 0, // Green channel
                0, 0, brightnessFactor, 0, 0, // Blue channel
                0, 0, 0, 1, 0, // Alpha channel
            };
            return SKColorFilter.CreateColorMatrix(colorMatrix);
        }

        #region Radar Paints

        public static SKPaint PaintBitmap { get; } = new()
        {
            IsAntialias = true,
        };

        public static SKPaint PaintBitmapAlpha { get; } = new()
        {
            Color = SKColor.Empty.WithAlpha(127),
            IsAntialias = true,
        };

        public static SKPaint PaintConnectorGroup { get; } = new()
        {
            Color = SKColors.LawnGreen.WithAlpha(60),
            StrokeWidth = 2.25f,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };

        public static SKPaint PaintMouseoverGroup { get; } = new()
        {
            Color = SKColors.LawnGreen,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };

        public static SKPaint TextMouseoverGroup { get; } = new()
        {
            Color = SKColors.LawnGreen,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint PaintLocalPlayer { get; } = new()
        {
            Color = SKColors.Green,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };

        public static SKPaint TextLocalPlayer { get; } = new()
        {
            Color = SKColors.Green,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint PaintTeammate { get; } = new()
        {
            Color = SKColors.LimeGreen,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };

        public static SKPaint TextTeammate { get; } = new()
        {
            Color = SKColors.LimeGreen,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint PaintPMC { get; } = new()
        {
            Color = SKColors.Red,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };

        public static SKPaint TextPMC { get; } = new()
        {
            Color = SKColors.Red,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint PaintWatchlist { get; } = new()
        {
            Color = SKColors.HotPink,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };

        public static SKPaint TextWatchlist { get; } = new()
        {
            Color = SKColors.HotPink,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint PaintStreamer { get; } = new()
        {
            Color = SKColors.MediumPurple,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };

        public static SKPaint TextStreamer { get; } = new()
        {
            Color = SKColors.MediumPurple,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint PaintScav { get; } = new()
        {
            Color = SKColors.Yellow,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };

        public static SKPaint TextScav { get; } = new()
        {
            Color = SKColors.Yellow,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint PaintRaider { get; } = new()
        {
            Color = SKColor.Parse("ffc70f"),
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };

        public static SKPaint TextRaider { get; } = new()
        {
            Color = SKColor.Parse("ffc70f"),
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint PaintBoss { get; } = new()
        {
            Color = SKColors.Fuchsia,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };

        public static SKPaint TextBoss { get; } = new()
        {
            Color = SKColors.Fuchsia,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint PaintFocused { get; } = new()
        {
            Color = SKColors.Coral,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };

        public static SKPaint TextFocused { get; } = new()
        {
            Color = SKColors.Coral,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint PaintPScav { get; } = new()
        {
            Color = SKColors.White,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };

        public static SKPaint TextPScav { get; } = new()
        {
            Color = SKColors.White,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint TextMouseover { get; } = new()
        {
            Color = SKColors.White,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint PaintDeathMarker { get; } = new()
        {
            Color = SKColors.Black,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };

        #endregion

        #region Loot Paints
        public static SKPaint PaintLoot { get; } = new()
        {
            Color = SKColors.WhiteSmoke,
            StrokeWidth = 0.25f,
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        public static SKPaint PaintImportantLoot { get; } = new()
        {
            Color = SKColors.Turquoise,
            StrokeWidth = 0.25f,
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        public static SKPaint PaintContainerLoot { get; } = new()
        {
            Color = SKColor.Parse("FFFFCC"),
            StrokeWidth = 0.25f,
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        public static SKPaint TextLoot { get; } = new()
        {
            Color = SKColors.WhiteSmoke,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint TextImportantLoot { get; } = new()
        {
            Color = SKColors.Turquoise,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint PaintCorpse { get; } = new()
        {
            Color = SKColors.Silver,
            StrokeWidth = 0.25f,
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        public static SKPaint TextCorpse { get; } = new()
        {
            Color = SKColors.Silver,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint PaintMeds { get; } = new()
        {
            Color = SKColors.LightSalmon,
            StrokeWidth = 0.25f,
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        public static SKPaint TextMeds { get; } = new()
        {
            Color = SKColors.LightSalmon,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint PaintFood { get; } = new()
        {
            Color = SKColors.CornflowerBlue,
            StrokeWidth = 0.25f,
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        public static SKPaint TextFood { get; } = new()
        {
            Color = SKColors.CornflowerBlue,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint PaintBackpacks { get; } = new()
        {
            Color = SKColor.Parse("00b02c"),
            StrokeWidth = 0.25f,
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        public static SKPaint TextBackpacks { get; } = new()
        {
            Color = SKColor.Parse("00b02c"),
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint QuestHelperPaint { get; } = new()
        {
            Color = SKColors.DeepPink,
            StrokeWidth = 0.25f,
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };
        public static SKPaint QuestHelperText { get; } = new()
        {
            Color = SKColors.DeepPink,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint PaintQuestItem { get; } = new()
        {
            Color = SKColors.YellowGreen,
            StrokeWidth = 0.25f,
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        public static SKPaint TextQuestItem { get; } = new()
        {
            Color = SKColors.YellowGreen,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint PaintWishlistItem { get; } = new()
        {
            Color = SKColors.Red,
            StrokeWidth = 0.25f,
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        public static SKPaint TextWishlistItem { get; } = new()
        {
            Color = SKColors.Red,
            IsStroke = false,
            IsAntialias = true,
        };

        #endregion

        #region Render/Misc Paints

        public static SKPaint PaintTransparentBacker { get; } = new()
        {
            Color = SKColors.Black.WithAlpha(0xBE), // Transparent backer
            StrokeWidth = 0.25f,
            Style = SKPaintStyle.Fill
        };

        public static SKPaint TextRadarStatus { get; } = new()
        {
            Color = SKColors.Red,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint TextStatusSmall { get; } = new SKPaint
        {
            Color = SKColors.Red,
            IsStroke = false,
            IsAntialias = true,
        };

        public static SKPaint PaintExplosives { get; } = new()
        {
            Color = SKColors.OrangeRed,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };

        public static SKPaint PaintExfilOpen { get; } = new()
        {
            Color = SKColors.LimeGreen,
            StrokeWidth = 0.25f,
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        public static SKPaint PaintExfilTransit { get; } = new()
        {
            Color = SKColors.Orange,
            StrokeWidth = 0.25f,
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        public static SKPaint PaintExfilPending { get; } = new()
        {
            Color = SKColors.Yellow,
            StrokeWidth = 0.25f,
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        public static SKPaint PaintExfilClosed { get; } = new()
        {
            Color = SKColors.Red,
            StrokeWidth = 0.25f,
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        public static SKPaint PaintExfilInactive { get; } = new()
        {
            Color = SKColors.Gray,
            StrokeWidth = 0.25f,
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        public static SKPaint TextOutline { get; } = new()
        {
            IsAntialias = true,
            Color = SKColors.Black,
            IsStroke = true,
            StrokeWidth = 2f,
            Style = SKPaintStyle.Stroke,
        };

        /// <summary>
        /// Only utilize this paint on the Radar UI Thread. StrokeWidth is modified prior to each draw call.
        /// *NOT* Thread safe to use!
        /// </summary>
        public static SKPaint ShapeOutline { get; } = new()
        {
            Color = SKColors.Black,
            /*StrokeWidth = ??,*/ // Compute before use
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };

        #endregion

        #region ESP Widget Paints

        public static SKPaint PaintESPWidgetCrosshair { get; } = new()
        {
            Color = SKColors.White,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };

        public static SKPaint PaintESPWidgetLocalPlayer { get; } = new()
        {
            Color = SKColors.Green,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke
        };

        public static SKPaint PaintESPWidgetPMC { get; } = new()
        {
            Color = SKColors.Red,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke
        };

        public static SKPaint PaintESPWidgetWatchlist { get; } = new()
        {
            Color = SKColors.HotPink,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke
        };

        public static SKPaint PaintESPWidgetStreamer { get; } = new()
        {
            Color = SKColors.MediumPurple,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke
        };

        public static SKPaint PaintESPWidgetTeammate { get; } = new()
        {
            Color = SKColors.LimeGreen,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke
        };

        public static SKPaint PaintESPWidgetBoss { get; } = new()
        {
            Color = SKColors.Fuchsia,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke
        };

        public static SKPaint PaintESPWidgetScav { get; } = new()
        {
            Color = SKColors.Yellow,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke
        };

        public static SKPaint PaintESPWidgetRaider { get; } = new()
        {
            Color = SKColor.Parse("ffc70f"),
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke
        };

        public static SKPaint PaintESPWidgetPScav { get; } = new()
        {
            Color = SKColors.White,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke
        };

        public static SKPaint PaintESPWidgetFocused { get; } = new()
        {
            Color = SKColors.Coral,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke
        };

        public static SKPaint PaintESPWidgetLoot { get; } = new()
        {
            Color = SKColors.WhiteSmoke,
            StrokeWidth = 0.75f,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        public static SKPaint TextESPWidgetLoot { get; } = new()
        {
            Color = SKColors.WhiteSmoke,
            IsStroke = false,
            IsAntialias = true
        };

        #endregion

        #region Player Info Widget Paints

        public static SKPaint TextPlayersOverlay { get; } = new()
        {
            Color = SKColors.White,
            IsStroke = false,
            IsAntialias = true
        };

        #endregion

    }
}
