/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using Collections.Pooled;
using LoneEftDmaRadar.Misc;
using LoneEftDmaRadar.Tarkov.World.Player;
using LoneEftDmaRadar.UI.Maps;
using LoneEftDmaRadar.UI.Skia;
using LoneEftDmaRadar.Web.TarkovDev;

namespace LoneEftDmaRadar.Tarkov.World.Loot
{
    public sealed class LootCorpse : LootItem
    {
        private static readonly TarkovMarketItem _default = new();
        private readonly ulong _corpse;
        /// <summary>
        /// Corpse container's associated player object (if any).
        /// </summary>
        public AbstractPlayer Player { get; private set; }
        /// <summary>
        /// Name of the corpse.
        /// </summary>
        public override string Name => Player?.Name ?? "Body";

        /// <summary>
        /// Constructor.
        /// </summary>
        public LootCorpse(ulong corpse, Vector3 position) : base(_default, position)
        {
            _corpse = corpse;
        }

        /// <summary>
        /// Sync the corpse's player reference from a list of dead players.
        /// </summary>
        /// <param name="deadPlayers"></param>
        public void Sync(IReadOnlyList<AbstractPlayer> deadPlayers)
        {
            Player ??= deadPlayers?.FirstOrDefault(x => x.Corpse == _corpse);
            Player?.LootObject ??= this;
        }

        public override string GetUILabel() => this.Name;

        public override void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            var heightDiff = Position.Y - localPlayer.Position.Y;
            var point = Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
            MouseoverPosition = new Vector2(point.X, point.Y);
            SKPaints.ShapeOutline.StrokeWidth = 2f;
            if (heightDiff > 1.45) // loot is above player
            {
                using var path = point.GetUpArrow(5);
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, SKPaints.PaintCorpse);
            }
            else if (heightDiff < -1.45) // loot is below player
            {
                using var path = point.GetDownArrow(5);
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, SKPaints.PaintCorpse);
            }
            else // loot is level with player
            {
                const float size = 5f;
                canvas.DrawCircle(point, size, SKPaints.ShapeOutline);
                canvas.DrawCircle(point, size, SKPaints.PaintCorpse);
            }

            point.Offset(7f, 3f);
            string important = (Player is ObservedPlayer observed && observed.Equipment.CarryingImportantLoot) ?
                "!!" : null; // Flag important loot
            string name = $"{important}{Name}";

            canvas.DrawText(
                name,
                point,
                SKTextAlign.Left,
                SKFonts.UIRegular,
                SKPaints.TextOutline); // Draw outline
            canvas.DrawText(
                name,
                point,
                SKTextAlign.Left,
                SKFonts.UIRegular,
                SKPaints.TextCorpse);
        }

        public override void DrawMouseover(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            using var lines = new PooledList<string>();
            if (Player is AbstractPlayer player)
            {
                lines.Add($"{player.Type.ToString()}:{player.Name}");
                if (Player is ObservedPlayer obs) // show equipment info
                {
                    lines.Add($"Value: {Utilities.FormatNumberKM(obs.Equipment.Value)}");
                    foreach (var item in obs.Equipment.Items.OrderBy(e => e.Key))
                    {
                        string important = item.Value.IsImportant ?
                            "!!" : null; // Flag important loot
                        lines.Add($"{important}{item.Key.Substring(0, 5)}: {item.Value.ShortName}");
                    }
                }
            }
            else
            {
                lines.Add(Name);
            }
            Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams).DrawMouseoverText(canvas, lines.Span);
        }
    }
}
