/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using LoneEftDmaRadar.Misc;
using LoneEftDmaRadar.Tarkov.Unity;
using LoneEftDmaRadar.Tarkov.World.Player;
using LoneEftDmaRadar.UI.Loot;
using LoneEftDmaRadar.UI.Maps;
using LoneEftDmaRadar.UI.Skia;
using LoneEftDmaRadar.Web.TarkovDev;

namespace LoneEftDmaRadar.Tarkov.World.Loot
{
    public class LootItem : IMouseoverEntity, IMapEntity, IWorldEntity
    {
        private static EftDmaConfig Config { get; } = Program.Config;
        private readonly TarkovMarketItem _item;

        public LootItem(TarkovMarketItem item, Vector3 position)
        {
            ArgumentNullException.ThrowIfNull(item, nameof(item));
            _item = item;
            _position = position;
        }

        public LootItem(string id, string name, Vector3 position)
        {
            ArgumentNullException.ThrowIfNull(id, nameof(id));
            ArgumentNullException.ThrowIfNull(name, nameof(name));
            _item = new TarkovMarketItem
            {
                Name = name,
                ShortName = name,
                FleaPrice = -1,
                TraderPrice = -1,
                BsgId = id
            };
            _position = position;
        }

        /// <summary>
        /// Item's BSG ID.
        /// </summary>
        public virtual string ID => _item.BsgId;

        /// <summary>
        /// Item's Long Name.
        /// </summary>
        public virtual string Name => _item.Name;

        /// <summary>
        /// Item's Short Name.
        /// </summary>
        public string ShortName => _item.ShortName;

        /// <summary>
        /// Item's Price (In roubles).
        /// </summary>
        public int Price
        {
            get
            {
                long price;
                if (Config.Loot.PricePerSlot)
                {
                    if (Config.Loot.PriceMode is LootPriceMode.FleaMarket)
                        price = (long)((float)_item.FleaPrice / GridCount);
                    else
                        price = (long)((float)_item.TraderPrice / GridCount);
                }
                else
                {
                    if (Config.Loot.PriceMode is LootPriceMode.FleaMarket)
                        price = _item.FleaPrice;
                    else
                        price = _item.TraderPrice;
                }
                if (price <= 0)
                    price = Math.Max(_item.FleaPrice, _item.TraderPrice);
                return (int)price;
            }
        }


        /// <summary>
        /// Number of grid spaces this item takes up.
        /// </summary>
        public int GridCount => _item.Slots == 0 ? 1 : _item.Slots;

        /// <summary>
        /// True if the item is important via the UI.
        /// </summary>
        public bool Important => _item?.Important ?? false;

        /// <summary>
        /// True if the item is blacklisted via the UI.
        /// </summary>
        public bool Blacklisted => _item?.Blacklisted ?? false;
        public bool IsWishlisted => _item.IsWishlisted;
        public bool IsMeds => _item.IsMed;
        public bool IsFood => _item.IsFood;
        public bool IsBackpack => _item.IsBackpack;
        public bool IsWeapon => _item.IsWeapon;
        public bool IsCurrency => _item.IsCurrency;
        public bool IsQuestHelperItem => _item.IsQuestHelperItem;
        public bool IsQuestItem { get; init; }

        /// <summary>
        /// Checks if an item is important via several means.
        /// </summary>
        public bool IsImportant => _item.IsImportant;

        /// <summary>
        /// Checks if an item exceeds regular loot price threshold.
        /// </summary>
        public bool IsRegularLoot
        {
            get
            {
                if (Blacklisted)
                    return false;
                return Price >= Program.Config.Loot.MinValue;
            }
        }

        /// <summary>
        /// Checks if an item exceeds valuable loot price threshold.
        /// </summary>
        public bool IsValuableLoot
        {
            get
            {
                if (Blacklisted)
                    return false;
                return Price >= Program.Config.Loot.MinValueValuable;
            }
        }

        private readonly Vector3 _position; // FilteredLoot doesn't move, readonly ok
        public ref readonly Vector3 Position => ref _position;
        public Vector2 MouseoverPosition { get; set; }

        public virtual void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            var label = GetUILabel();
            var paints = GetPaints();
            var heightDiff = Position.Y - localPlayer.Position.Y;
            var point = Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
            MouseoverPosition = new Vector2(point.X, point.Y);
            SKPaints.ShapeOutline.StrokeWidth = 2f;
            if (heightDiff > 1.45) // loot is above player
            {
                using var path = point.GetUpArrow(5);
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, paints.Item1);
            }
            else if (heightDiff < -1.45) // loot is below player
            {
                using var path = point.GetDownArrow(5);
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, paints.Item1);
            }
            else // loot is level with player
            {
                const float size = 5f;
                canvas.DrawCircle(point, size, SKPaints.ShapeOutline);
                canvas.DrawCircle(point, size, paints.Item1);
            }

            point.Offset(7f, 3f);

            canvas.DrawText(
                label,
                point,
                SKTextAlign.Left,
                SKFonts.UIRegular,
                SKPaints.TextOutline); // Draw outline
            canvas.DrawText(
                label,
                point,
                SKTextAlign.Left,
                SKFonts.UIRegular,
                paints.Item2);

        }

        public virtual void DrawMouseover(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            return; // Regular loot has no extra mouseover info
        }

        /// <summary>
        /// Gets a UI Friendly Label.
        /// </summary>
        /// <returns>Item Label string cleaned up for UI usage.</returns>
        public virtual string GetUILabel()
        {
            string label = "";
            if (Price > 0 && !IsImportant)
                label += $"[{Utilities.FormatNumberKM(Price)}] ";
            label += ShortName;

            if (string.IsNullOrEmpty(label))
                label = "Item";
            return label;
        }

        private ValueTuple<SKPaint, SKPaint> GetPaints()
        {
            if (Program.Config.QuestHelper.Enabled && IsQuestHelperItem)
                return new(SKPaints.PaintQuestItem, SKPaints.TextQuestItem);
            if (Config.Loot.ShowWishlist && IsWishlisted)
                return new(SKPaints.PaintWishlistItem, SKPaints.TextWishlistItem);
            if (LootFilter.ShowBackpacks && IsBackpack)
                return new(SKPaints.PaintBackpacks, SKPaints.TextBackpacks);
            if (LootFilter.ShowMeds && IsMeds)
                return new(SKPaints.PaintMeds, SKPaints.TextMeds);
            if (LootFilter.ShowFood && IsFood)
                return new(SKPaints.PaintFood, SKPaints.TextFood);
            if (LootFilter.ShowQuestItems && IsQuestItem)
                return new(SKPaints.PaintQuestItem, SKPaints.TextQuestItem);
            string filterColor = _item.CustomFilter?.Color;

            if (!string.IsNullOrEmpty(filterColor))
            {
                var filterPaints = GetFilterPaints(filterColor);
                return new(filterPaints.Item1, filterPaints.Item2);
            }
            if (IsValuableLoot)
                return new(SKPaints.PaintImportantLoot, SKPaints.TextImportantLoot);
            return new(SKPaints.PaintLoot, SKPaints.TextLoot);
        }

        #region Custom Loot Paints
        private static readonly ConcurrentDictionary<string, Tuple<SKPaint, SKPaint>> _paints = new();

        /// <summary>
        /// Returns the Paints for this color value.
        /// </summary>
        /// <param name="color">Color rgba hex string.</param>
        /// <returns>Tuple of paints. Item1 = Paint, Item2 = Text. Item3 = ESP Paint, Item4 = ESP Text</returns>
        private static Tuple<SKPaint, SKPaint> GetFilterPaints(string color)
        {
            if (!SKColor.TryParse(color, out var skColor))
                return new Tuple<SKPaint, SKPaint>(SKPaints.PaintLoot, SKPaints.TextLoot);

            var result = _paints.AddOrUpdate(color,
                key =>
                {
                    var paint = new SKPaint
                    {
                        Color = skColor,
                        StrokeWidth = 3f,
                        Style = SKPaintStyle.Fill,
                        IsAntialias = true
                    };
                    var text = new SKPaint
                    {
                        Color = skColor,
                        IsStroke = false,
                        IsAntialias = true
                    };
                    return new Tuple<SKPaint, SKPaint>(paint, text);
                },
                (key, existingValue) =>
                {
                    existingValue.Item1.StrokeWidth = 3f;
                    return existingValue;
                });

            return result;
        }

        public static void ScaleLootPaints(float newScale)
        {
            foreach (var paint in _paints)
            {
                paint.Value.Item1.StrokeWidth = 3f;
            }
        }

        #endregion
    }
}
