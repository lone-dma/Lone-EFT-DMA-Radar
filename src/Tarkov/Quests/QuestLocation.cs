/*
 * EFT DMA Radar Lite
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

using Collections.Pooled;
using EftDmaRadarLite.Misc;
using EftDmaRadarLite.Tarkov.Data;
using EftDmaRadarLite.Tarkov.Player;
using EftDmaRadarLite.UI.Radar;
using EftDmaRadarLite.UI.Skia;
using EftDmaRadarLite.UI.Skia.Maps;
using EftDmaRadarLite.Unity;

namespace EftDmaRadarLite.Tarkov.Quests
{
    /// <summary>
    /// Wraps a Mouseoverable Quest Location marker onto the Map GUI.
    /// </summary>
    public sealed class QuestLocation : IWorldEntity, IMapEntity, IMouseoverEntity
    {
        /// <summary>
        /// Name of this quest.
        /// </summary>
        public string Name { get; }

        public QuestLocation(string questID, string target, Vector3 position)
        {
            if (EftDataManager.TaskData.TryGetValue(questID, out var q))
                Name = q.Name;
            else
                Name = target;
            Position = position;
        }

        public void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            var point = Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
            MouseoverPosition = new Vector2(point.X, point.Y);
            var heightDiff = Position.Y - localPlayer.Position.Y;
            SKPaints.ShapeOutline.StrokeWidth = 2f;
            if (heightDiff > 1.45) // marker is above player
            {
                using var path = point.GetUpArrow();
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, SKPaints.QuestHelperPaint);
            }
            else if (heightDiff < -1.45) // marker is below player
            {
                using var path = point.GetDownArrow();
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, SKPaints.QuestHelperPaint);
            }
            else // marker is level with player
            {
                var squareSize = 8 * App.Config.UI.UIScale;
                canvas.DrawRect(point.X, point.Y,
                    squareSize, squareSize, SKPaints.ShapeOutline);
                canvas.DrawRect(point.X, point.Y,
                    squareSize, squareSize, SKPaints.QuestHelperPaint);
            }
        }

        public Vector2 MouseoverPosition { get; set; }

        public void DrawMouseover(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            using var lines = new PooledList<string>() { Name };
            Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams).DrawMouseoverText(canvas, lines);
        }

        private Vector3 _position;
        public ref Vector3 Position => ref _position;
    }
}
