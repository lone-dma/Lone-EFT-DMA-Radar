using eft_dma_radar.Tarkov.Player;
using eft_dma_radar.UI.Skia;
using eft_dma_radar.Misc;
using eft_dma_radar.UI.Skia.Maps;
using eft_dma_radar.Tarkov.Data;

namespace eft_dma_radar.Tarkov.Loot
{
    public sealed class StaticLootContainer : LootContainer
    {
        private static readonly IReadOnlyList<LootItem> _defaultLoot = new List<LootItem>(1);

        public override string Name { get; } = "Container";
        public override string ID { get; }

        /// <summary>
        /// True if the container has been searched by LocalPlayer or another Networked Entity.
        /// </summary>
        public bool Searched { get; }

        public StaticLootContainer(string containerId, bool opened) : base(_defaultLoot)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId, nameof(containerId));
            ID = containerId;
            Searched = opened;
            if (EftDataManager.AllContainers.TryGetValue(containerId, out var container))
            {
                Name = container.ShortName ?? "Container";
            }
        }

        public override void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            var dist = Vector3.Distance(localPlayer.Position, Position);

            if (dist > App.Config.Containers.DrawDistance)
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
            var lines = new List<string>
            {
                Name
            };
            Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams).DrawMouseoverText(canvas, lines);
        }
    }
}
