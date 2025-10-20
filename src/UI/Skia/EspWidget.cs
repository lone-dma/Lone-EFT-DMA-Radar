/*
 * Lone EFT DMA Radar
 * Brought to you by Lone (Lone DMA)
 * 
MIT License

Copyright (c) 2025 Lone DMA

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 *
*/

using LoneEftDmaRadar.Tarkov.GameWorld;
using LoneEftDmaRadar.Tarkov.Loot;
using LoneEftDmaRadar.Tarkov.Player;
using SkiaSharp.Views.WPF;

namespace LoneEftDmaRadar.UI.Skia
{
    public sealed class EspWidget : AbstractSKWidget
    {
        private SKBitmap _espBitmap;
        private SKCanvas _espCanvas;

        // Constants
        private const float LOOT_RENDER_DISTANCE = 10f;
        private const float CONTAINER_RENDER_DISTANCE = 10f;

        public EspWidget(SKGLElement parent, SKRect location, bool minimized, float scale)
            : base(parent, "ESP",
                new SKPoint(location.Left, location.Top),
                new SKSize(location.Width, location.Height),
                scale)
        {
            AllocateSurface((int)location.Width, (int)location.Height);
            Minimized = minimized;
        }

        private static LocalPlayer LocalPlayer => Memory.LocalPlayer;
        private static IReadOnlyCollection<AbstractPlayer> AllPlayers => Memory.Players;
        private static bool InRaid => Memory.InRaid;
        private static IEnumerable<LootItem> Loot => Memory.Loot?.FilteredLoot;
        private static IEnumerable<StaticLootContainer> Containers => Memory.Loot?.StaticContainers;

        public override void Draw(SKCanvas canvas)
        {
            base.Draw(canvas);
            if (Minimized)
                return;

            RenderESPWidget(canvas, ClientRectangle);
        }

        private void RenderESPWidget(SKCanvas targetCanvas, SKRect dest)
        {
            EnsureSurface(Size);

            _espCanvas.Clear(SKColors.Transparent);

            try
            {
                if (!InRaid)
                    return;

                if (LocalPlayer is not LocalPlayer localPlayer)
                    return;

                // Precompute scale factors once per frame
                var viewport = CameraManager.Viewport;
                float scaleX = _espBitmap.Width / (float)viewport.Width;
                float scaleY = _espBitmap.Height / (float)viewport.Height;

                if (App.Config.Loot.Enabled)
                {
                    DrawLoot(localPlayer, scaleX, scaleY);
                    if (App.Config.Containers.Enabled)
                        DrawContainers(localPlayer, scaleX, scaleY);
                }

                DrawPlayers(scaleX, scaleY);
                DrawCrosshair();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CRITICAL ESP WIDGET RENDER ERROR: {ex}");
            }

            _espCanvas.Flush();
            targetCanvas.DrawBitmap(_espBitmap, dest, SKPaints.PaintBitmap);
        }

        private void DrawLoot(LocalPlayer localPlayer, float scaleX, float scaleY)
        {
            if (Loot is not IEnumerable<LootItem> loot)
                return;

            float boxHalf = 4f * ScaleFactor;
            var lpPos = localPlayer.Position;

            foreach (var item in loot)
            {
                // Distance squared test first
                var itemPos = item.Position;
                var dist = Vector3.Distance(lpPos, itemPos);
                if (dist > LOOT_RENDER_DISTANCE)
                    continue;

                if (!CameraManager.WorldToScreen(ref itemPos, out var screen))
                    continue;

                var adj = ScalePoint(screen, scaleX, scaleY);
                DrawBoxAndLabel(adj, boxHalf, $"{item.GetUILabel(true)} ({dist:n1}m)", SKPaints.PaintESPWidgetLoot, SKPaints.TextESPWidgetLoot);
            }
        }

        private void DrawContainers(LocalPlayer localPlayer, float scaleX, float scaleY)
        {
            if (Containers is not IEnumerable<StaticLootContainer> containers)
                return;

            float boxHalf = 4f * ScaleFactor;
            var lpPos = localPlayer.Position;
            bool hideSearched = App.Config.Containers.HideSearched;

            foreach (var container in containers)
            {
                if (!(MainWindow.Instance?.Settings?.ViewModel?.ContainerIsTracked(container.ID ?? "NULL") ?? false))
                    continue;

                if (hideSearched && container.Searched)
                    continue;

                var cPos = container.Position;
                var dist = Vector3.Distance(lpPos, cPos);
                if (dist > CONTAINER_RENDER_DISTANCE)
                    continue;

                if (!CameraManager.WorldToScreen(ref cPos, out var screen))
                    continue;

                var adj = ScalePoint(screen, scaleX, scaleY);
                DrawBoxAndLabel(adj, boxHalf, $"{container.Name} ({dist:n1}m)", SKPaints.PaintESPWidgetLoot, SKPaints.TextESPWidgetLoot);
            }
        }

        private void DrawPlayers(float scaleX, float scaleY)
        {
            var players = AllPlayers?
                .Where(p => p.IsActive && p.IsAlive && p is not LoneEftDmaRadar.Tarkov.Player.LocalPlayer);

            if (players is null)
                return;

            foreach (var player in players)
            {
                if (player.Skeleton.UpdateESPWidgetBuffer(scaleX, scaleY, out var buffer))
                {
                    _espCanvas.DrawPoints(SKPointMode.Lines, buffer, GetPaint(player));
                }
            }
        }

        private void DrawCrosshair()
        {
            var bounds = _espBitmap.Info.Rect;
            float centerX = bounds.MidX;
            float centerY = bounds.MidY;

            _espCanvas.DrawLine(bounds.Left, centerY, bounds.Right, centerY, SKPaints.PaintESPWidgetCrosshair);
            _espCanvas.DrawLine(centerX, bounds.Top, centerX, bounds.Bottom, SKPaints.PaintESPWidgetCrosshair);
        }

        private void DrawBoxAndLabel(SKPoint center, float half, string label, SKPaint boxPaint, SKPaint textPaint)
        {
            // NOTE: Original code used inverted Y values (top/bottom). Adjusted to conventional rect (top < bottom).
            var rect = new SKRect(center.X - half, center.Y - half, center.X + half, center.Y + half);
            var textPt = new SKPoint(center.X, center.Y + 12.5f * ScaleFactor);

            _espCanvas.DrawRect(rect, boxPaint);
            _espCanvas.DrawText(label, textPt, SKTextAlign.Left, SKFonts.EspWidgetFont, textPaint);
        }

        private void EnsureSurface(SKSize size)
        {
            if (_espBitmap != null &&
                _espCanvas != null &&
                _espBitmap.Width == (int)size.Width &&
                _espBitmap.Height == (int)size.Height)
                return;

            DisposeSurface();
            AllocateSurface((int)size.Width, (int)size.Height);
        }

        private void AllocateSurface(int width, int height)
        {
            if (width <= 0 || height <= 0)
                return;

            _espBitmap = new SKBitmap(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            _espCanvas = new SKCanvas(_espBitmap);
        }

        private void DisposeSurface()
        {
            _espCanvas?.Dispose();
            _espCanvas = null;
            _espBitmap?.Dispose();
            _espBitmap = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static SKPoint ScalePoint(SKPoint original, float scaleX, float scaleY) =>
            new SKPoint(original.X * scaleX, original.Y * scaleY);

        public override void SetScaleFactor(float newScale)
        {
            base.SetScaleFactor(newScale);
            // Consolidated strokes
            float std = 1f * newScale;
            SKPaints.PaintESPWidgetCrosshair.StrokeWidth = std;
            SKPaints.PaintESPWidgetLocalPlayer.StrokeWidth = std;
            SKPaints.PaintESPWidgetPMC.StrokeWidth = std;
            SKPaints.PaintESPWidgetWatchlist.StrokeWidth = std;
            SKPaints.PaintESPWidgetStreamer.StrokeWidth = std;
            SKPaints.PaintESPWidgetTeammate.StrokeWidth = std;
            SKPaints.PaintESPWidgetBoss.StrokeWidth = std;
            SKPaints.PaintESPWidgetScav.StrokeWidth = std;
            SKPaints.PaintESPWidgetRaider.StrokeWidth = std;
            SKPaints.PaintESPWidgetPScav.StrokeWidth = std;
            SKPaints.PaintESPWidgetFocused.StrokeWidth = std;
            SKPaints.PaintESPWidgetLoot.StrokeWidth = 0.75f * newScale;
        }

        public override void Dispose()
        {
            DisposeSurface();
            base.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static SKPaint GetPaint(AbstractPlayer player)
        {
            if (player.IsFocused)
                return SKPaints.PaintESPWidgetFocused;
            if (player is LocalPlayer)
                return SKPaints.PaintESPWidgetLocalPlayer;

            return player.Type switch
            {
                PlayerType.Teammate => SKPaints.PaintESPWidgetTeammate,
                PlayerType.PMC => SKPaints.PaintESPWidgetPMC,
                PlayerType.AIScav => SKPaints.PaintESPWidgetScav,
                PlayerType.AIRaider => SKPaints.PaintESPWidgetRaider,
                PlayerType.AIBoss => SKPaints.PaintESPWidgetBoss,
                PlayerType.PScav => SKPaints.PaintESPWidgetPScav,
                PlayerType.SpecialPlayer => SKPaints.PaintESPWidgetWatchlist,
                PlayerType.Streamer => SKPaints.PaintESPWidgetStreamer,
                _ => SKPaints.PaintESPWidgetPMC
            };
        }
    }
}