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

using Collections.Pooled;
using LoneEftDmaRadar.Misc;
using LoneEftDmaRadar.Tarkov.GameWorld.Player;
using LoneEftDmaRadar.UI.Radar.Maps;
using LoneEftDmaRadar.UI.Radar.ViewModels;
using LoneEftDmaRadar.UI.Skia;
using LoneEftDmaRadar.Web.TarkovDev.Data;

namespace LoneEftDmaRadar.Tarkov.GameWorld.Loot
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

        public override void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            if (App.Config.Loot.HideCorpses)
                return;
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
                var size = 5 * App.Config.UI.UIScale;
                canvas.DrawCircle(point, size, SKPaints.ShapeOutline);
                canvas.DrawCircle(point, size, SKPaints.PaintCorpse);
            }

            point.Offset(7 * App.Config.UI.UIScale, 3 * App.Config.UI.UIScale);

            canvas.DrawText(
                Name,
                point,
                SKTextAlign.Left,
                SKFonts.UIRegular,
                SKPaints.TextOutline); // Draw outline
            canvas.DrawText(
                Name,
                point,
                SKTextAlign.Left,
                SKFonts.UIRegular,
                SKPaints.TextCorpse);
        }

        public override void DrawMouseover(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            if (!RadarViewModel.LootCorpsesVisible)
                return;
            using var lines = new PooledList<string>();
            if (Player is AbstractPlayer player)
            {
                var name = App.Config.UI.HideNames && player.IsHuman ? "<Hidden>" : player.Name;
                lines.Add($"{player.Type.ToString()}:{name}");
                string g = null;
                if (player.GroupID != -1) g = $"G:{player.GroupID} ";
                if (g is not null) lines.Add(g);
                if (Player is ObservedPlayer obs) // show equipment info
                {
                    lines.Add($"Value: {Utilities.FormatNumberKM(obs.Equipment.Value)}");
                    foreach (var item in obs.Equipment.Items.OrderBy(e => e.Key))
                    {
                        lines.Add($"{item.Key.Substring(0, 5)}: {item.Value.ShortName}");
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