/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using LoneEftDmaRadar.Misc;
using LoneEftDmaRadar.Tarkov.World.Player;
using LoneEftDmaRadar.UI.Maps;
using LoneEftDmaRadar.UI.Skia;
using LoneEftDmaRadar.Web.TarkovDev;

namespace LoneEftDmaRadar.Tarkov.World.Loot
{
    public sealed class StaticLootContainer : LootItem
    {
        private static readonly TarkovMarketItem _default = new();
        public override string Name { get; } = "Container";
        public override string ID { get; }

        /// <summary>
        /// True if the container has been searched by LocalPlayer or another Networked Entity.
        /// </summary>
        public bool Searched { get; private set; }

        public StaticLootContainer(string containerId, Vector3 position) : base(_default, position)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId, nameof(containerId));
            ID = containerId;
            if (TarkovDataManager.AllContainers.TryGetValue(containerId, out var container))
            {
                Name = container.ShortName ?? "Container";
            }
        }

        public override string GetUILabel() => this.Name;

        public override void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            if (Position.WithinDistance(localPlayer.Position, Program.Config.Containers.DrawDistance))
            {
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
                    const float size = 4f;
                    canvas.DrawCircle(point, size, SKPaints.ShapeOutline);
                    canvas.DrawCircle(point, size, SKPaints.PaintContainerLoot);
                }
            }
        }

        public override void DrawMouseover(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams).DrawMouseoverText(canvas, Name);
        }
    }
}

