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
using EftDmaRadarLite.UI.Skia;
using EftDmaRadarLite.UI.Skia.Maps;

namespace EftDmaRadarLite.Tarkov.Loot
{
    public sealed class StaticLootContainer : LootContainer
    {
        private readonly ulong _interactiveClass;
        public override string Name { get; } = "Container";
        public override string ID { get; }

        /// <summary>
        /// True if the container has been searched by LocalPlayer or another Networked Entity.
        /// </summary>
        public bool Searched { get; private set; }

        public StaticLootContainer(string containerId, ulong interactiveClass) : base()
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId, nameof(containerId));
            ID = containerId;
            _interactiveClass = interactiveClass;
            RefreshSearchedStatus();
            if (EftDataManager.AllContainers.TryGetValue(containerId, out var container))
            {
                Name = container.ShortName ?? "Container";
            }
        }

        /// <summary>
        /// Refresh the Searched status of this container.
        /// </summary>
        public void RefreshSearchedStatus()
        {
            try
            {
                this.Searched = Memory.ReadValue<ulong>(_interactiveClass + Offsets.LootableContainer.InteractingPlayer) != 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading Searched status for container {ID}: {ex.Message}");
            }
        }

        public override void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            if (!Position.WithinDistance(localPlayer.Position, App.Config.Containers.DrawDistance))
                return;
            var heightDiff = Position.Y - localPlayer.Position.Y;
            var point = Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
            MouseoverPosition = new Vector2(point.X, point.Y);
            SKPaints.ShapeOutline.StrokeWidth = 2f;
            if (heightDiff > 1.45) // loot is above player
            {
                using var path = point.GetUpArrow(4);
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, SKPaints.PaintContainerLoot);
            }
            else if (heightDiff < -1.45) // loot is below player
            {
                using var path = point.GetDownArrow(4);
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, SKPaints.PaintContainerLoot);
            }
            else // loot is level with player
            {
                var size = 4 * App.Config.UI.UIScale;
                canvas.DrawCircle(point, size, SKPaints.ShapeOutline);
                canvas.DrawCircle(point, size, SKPaints.PaintContainerLoot);
            }
        }

        public override void DrawMouseover(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            using var lines = new PooledList<string>
            {
                Name
            };
            Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams).DrawMouseoverText(canvas, lines);
        }
    }
}
