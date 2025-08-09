using eft_dma_radar.Tarkov.Player;
using eft_dma_radar.Tarkov.GameWorld;
using eft_dma_radar.Tarkov.Loot;
using eft_dma_radar.UI.Skia;
using SkiaSharp.Views.WPF;

namespace eft_dma_radar.UI.Radar
{
    public sealed class EspWidget : SKWidgetControl
    {
        private SKBitmap _espBitmap;
        private SKCanvas _espCanvas;

        public EspWidget(SKGLElement parent, SKRect location, bool minimized, float scale)
            : base(parent, "ESP", new SKPoint(location.Left, location.Top), new SKSize(location.Width, location.Height),
                scale)
        {
            _espBitmap = new SKBitmap((int)location.Width, (int)location.Height, SKImageInfo.PlatformColorType,
                SKAlphaType.Premul);
            _espCanvas = new SKCanvas(_espBitmap);
            Minimized = minimized;
        }

        /// <summary>
        /// LocalPlayer (who is running Radar) 'Player' object.
        /// Returns the player the Current Window belongs to.
        /// </summary>
        private static LocalPlayer LocalPlayer =>
            Memory.LocalPlayer;

        /// <summary>
        /// All Players in Local Game World (including dead/exfil'd) 'Player' collection.
        /// </summary>
        private static IReadOnlyCollection<PlayerBase> AllPlayers => Memory.Players;

        /// <summary>
        /// Radar has found Local Game World, and a Raid Instance is active.
        /// </summary>
        private static bool InRaid => Memory.InRaid;

        /// <summary>
        /// Contains all filtered loot in Local Game World.
        /// </summary>
        private static IEnumerable<LootItem> Loot => Memory.Loot?.FilteredLoot;

        /// <summary>
        /// All Static Containers on the map.
        /// </summary>
        private static IEnumerable<StaticLootContainer> Containers => Memory.Loot?.StaticLootContainers;

        public override void Draw(SKCanvas canvas)
        {
            base.Draw(canvas);
            if (!Minimized)
                RenderESPWidget(canvas, ClientRectangle);
        }

        /// <summary>
        /// Perform Aimview (Mini-ESP) Rendering.
        /// </summary>
        private void RenderESPWidget(SKCanvas parent, SKRect dest)
        {
            var size = Size;
            if (_espBitmap is null || _espCanvas is null ||
                _espBitmap.Width != size.Width || _espBitmap.Height != size.Height)
            {
                _espCanvas?.Dispose();
                _espCanvas = null;
                _espBitmap?.Dispose();
                _espBitmap = null;
                _espBitmap = new SKBitmap((int)size.Width, (int)size.Height, SKImageInfo.PlatformColorType,
                    SKAlphaType.Premul);
                _espCanvas = new SKCanvas(_espBitmap);
            }

            _espCanvas.Clear(SKColors.Transparent);
            try
            {
                var inRaid = InRaid; // cache bool
                var localPlayer = LocalPlayer; // cache ref to current player
                if (inRaid && localPlayer is not null)
                {
                    if (App.Config.Loot.Enabled)
                    {
                        float boxHalf = 4f * ScaleFactor;
                        var loot = Loot;
                        if (loot is not null)
                        {
                            foreach (var item in loot)
                            {
                                var dist = Vector3.Distance(localPlayer.Position, item.Position);
                                if (dist >= 10f)
                                    continue;
                                if (!CameraManager.WorldToScreen(ref item.Position, out var itemScrPos))
                                    continue;
                                var adjPos = ScaleESPPoint(itemScrPos);
                                var boxPt = new SKRect(adjPos.X - boxHalf, adjPos.Y + boxHalf,
                                    adjPos.X + boxHalf, adjPos.Y - boxHalf);
                                var textPt = new SKPoint(adjPos.X,
                                    adjPos.Y + 12.5f * ScaleFactor);
                                _espCanvas.DrawRect(boxPt, SKPaints.PaintESPWidgetLoot);
                                var label = item.GetUILabel(true) + $" ({dist.ToString("n1")}m)";

                                _espCanvas.DrawText(
                                    label,
                                    textPt,
                                    SKTextAlign.Left,
                                    SKFonts.EspWidgetFont,
                                    SKPaints.TextESPWidgetLoot);

                            }
                        }
                        if (App.Config.Containers.Enabled)
                        {
                            var containers = Containers;
                            if (containers is not null)
                            {
                                foreach (var container in containers)
                                {
                                    if (MainWindow.Instance?.Settings?.ViewModel?.ContainerIsTracked(container.ID ?? "NULL") ?? false)
                                    {
                                        if (App.Config.Containers.HideSearched && container.Searched)
                                        {
                                            continue;
                                        }
                                        var dist = Vector3.Distance(localPlayer.Position, container.Position);
                                        if (dist >= 10f)
                                            continue;
                                        if (!CameraManager.WorldToScreen(ref container.Position, out var containerScrPos))
                                            continue;
                                        var adjPos = ScaleESPPoint(containerScrPos);
                                        var boxPt = new SKRect(adjPos.X - boxHalf, adjPos.Y + boxHalf,
                                            adjPos.X + boxHalf, adjPos.Y - boxHalf);
                                        var textPt = new SKPoint(adjPos.X,
                                            adjPos.Y + 12.5f * ScaleFactor);
                                        _espCanvas.DrawRect(boxPt, SKPaints.PaintESPWidgetLoot);
                                        var label = $"{container.Name} ({dist.ToString("n1")}m)";

                                        _espCanvas.DrawText(
                                            label,
                                            textPt,
                                            SKTextAlign.Left,
                                            SKFonts.EspWidgetFont,
                                            SKPaints.TextESPWidgetLoot);

                                    }
                                }
                            }
                        }
                    }

                    var allPlayers = AllPlayers?
                        .Where(x => x.IsActive && x.IsAlive &&
                                    x is not Tarkov.Player.LocalPlayer);
                    if (allPlayers is not null)
                    {
                        var scaleX = _espBitmap.Width / (float)CameraManager.Viewport.Width;
                        var scaleY = _espBitmap.Height / (float)CameraManager.Viewport.Height;
                        foreach (var player in allPlayers)
                            if (player.Skeleton.UpdateESPWidgetBuffer(scaleX, scaleY))
                                _espCanvas.DrawPoints(SKPointMode.Lines, Skeleton.ESPWidgetBuffer, GetPaint(player));
                    }

                    var bounds = _espBitmap.Info.Rect;
                    /// Draw Crosshair
                    float centerX = bounds.Left + bounds.Width / 2;
                    float centerY = bounds.Top + bounds.Height / 2;

                    _espCanvas.DrawLine(bounds.Left, centerY, bounds.Right, centerY, SKPaints.PaintESPWidgetCrosshair);
                    _espCanvas.DrawLine(centerX, bounds.Top, centerX, bounds.Bottom, SKPaints.PaintESPWidgetCrosshair);
                }
            }
            catch (Exception ex) // Log rendering errors
            {
                var error = $"CRITICAL ESP WIDGET RENDER ERROR: {ex}";
                Debug.WriteLine(error);
            }

            _espCanvas.Flush();
            parent.DrawBitmap(_espBitmap, dest, SKPaints.PaintBitmap);
        }

        /// <summary>
        /// Scales larger Screen Coordinates to smaller Aimview Coordinates.
        /// </summary>
        /// <param name="original">Original W2S Screen Coords.</param>
        /// <returns>Adjusted Aimview Screen Coords.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SKPoint ScaleESPPoint(SKPoint original)
        {
            var scaleX = _espBitmap.Width / (float)CameraManager.Viewport.Width;
            var scaleY = _espBitmap.Height / (float)CameraManager.Viewport.Height;

            var newX = original.X * scaleX;
            var newY = original.Y * scaleY;

            return new SKPoint(newX, newY);
        }

        public override void SetScaleFactor(float newScale)
        {

            base.SetScaleFactor(newScale);
            SKPaints.PaintESPWidgetCrosshair.StrokeWidth = 1 * newScale;
            SKPaints.PaintESPWidgetLocalPlayer.StrokeWidth = 1 * newScale;
            SKPaints.PaintESPWidgetPMC.StrokeWidth = 1 * newScale;
            SKPaints.PaintESPWidgetWatchlist.StrokeWidth = 1 * newScale;
            SKPaints.PaintESPWidgetStreamer.StrokeWidth = 1 * newScale;
            SKPaints.PaintESPWidgetTeammate.StrokeWidth = 1 * newScale;
            SKPaints.PaintESPWidgetBoss.StrokeWidth = 1 * newScale;
            SKPaints.PaintESPWidgetScav.StrokeWidth = 1 * newScale;
            SKPaints.PaintESPWidgetRaider.StrokeWidth = 1 * newScale;
            SKPaints.PaintESPWidgetPScav.StrokeWidth = 1 * newScale;
            SKPaints.PaintESPWidgetFocused.StrokeWidth = 1 * newScale;
            SKPaints.PaintESPWidgetLoot.StrokeWidth = 0.75f * newScale;
        }

        public override void Dispose()
        {
            _espBitmap?.Dispose();
            _espCanvas?.Dispose();
            base.Dispose();
        }

        /// <summary>
        /// Gets Aimview drawing paintbrush based on Player Type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static SKPaint GetPaint(PlayerBase player)
        {
            if (player.IsFocused)
                return SKPaints.PaintESPWidgetFocused;
            if (player is LocalPlayer)
                return SKPaints.PaintESPWidgetLocalPlayer;
            switch (player.Type)
            {
                case PlayerType.Teammate:
                    return SKPaints.PaintESPWidgetTeammate;
                case PlayerType.PMC:
                    return SKPaints.PaintESPWidgetPMC;
                case PlayerType.AIScav:
                    return SKPaints.PaintESPWidgetScav;
                case PlayerType.AIRaider:
                    return SKPaints.PaintESPWidgetRaider;
                case PlayerType.AIBoss:
                    return SKPaints.PaintESPWidgetBoss;
                case PlayerType.PScav:
                    return SKPaints.PaintESPWidgetPScav;
                case PlayerType.SpecialPlayer:
                    return SKPaints.PaintESPWidgetWatchlist;
                case PlayerType.Streamer:
                    return SKPaints.PaintESPWidgetStreamer;
                default:
                    return SKPaints.PaintESPWidgetPMC;
            }
        }
    }
}