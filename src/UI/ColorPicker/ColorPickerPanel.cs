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

using ImGuiNET;
using LoneEftDmaRadar.UI.Skia;

namespace LoneEftDmaRadar.UI.ColorPicker
{
    /// <summary>
    /// Color Picker Panel for the ImGui-based Radar.
    /// </summary>
    internal static class ColorPickerPanel
    {
        private static readonly RadarUIState _state = RadarUIState.Instance;
        private static Vector3 _editingColor = Vector3.One;
        private static ColorPickerOption? _selectedOption;
        private static string _hexInput = "#FFFFFF";

        /// <summary>
        /// Initialize colors from config. Call once at startup.
        /// </summary>
        public static void Initialize()
        {
            // Add default colors for any missing entries
            foreach (var defaultColor in GetDefaultColors())
                Program.Config.RadarColors.TryAdd(defaultColor.Key, defaultColor.Value);

            // Apply all colors from config
            SetAllColors(Program.Config.RadarColors);
        }

        /// <summary>
        /// Returns all default color combinations for Radar.
        /// </summary>
        private static Dictionary<ColorPickerOption, string> GetDefaultColors()
        {
            return new()
            {
                [ColorPickerOption.LocalPlayer] = SKColors.Green.ToString(),
                [ColorPickerOption.FriendlyPlayer] = SKColors.LimeGreen.ToString(),
                [ColorPickerOption.PMCPlayer] = SKColors.Red.ToString(),
                [ColorPickerOption.WatchlistPlayer] = SKColors.HotPink.ToString(),
                [ColorPickerOption.StreamerPlayer] = SKColors.MediumPurple.ToString(),
                [ColorPickerOption.HumanScavPlayer] = SKColors.White.ToString(),
                [ColorPickerOption.ScavPlayer] = SKColors.Yellow.ToString(),
                [ColorPickerOption.RaiderPlayer] = SKColor.Parse("ffc70f").ToString(),
                [ColorPickerOption.BossPlayer] = SKColors.Fuchsia.ToString(),
                [ColorPickerOption.FocusedPlayer] = SKColors.Coral.ToString(),
                [ColorPickerOption.DeathMarker] = SKColors.Black.ToString(),
                [ColorPickerOption.RegularLoot] = SKColors.WhiteSmoke.ToString(),
                [ColorPickerOption.ValuableLoot] = SKColors.Turquoise.ToString(),
                [ColorPickerOption.WishlistLoot] = SKColors.Lime.ToString(),
                [ColorPickerOption.ContainerLoot] = SKColor.Parse("FFFFCC").ToString(),
                [ColorPickerOption.QuestHelperItems] = SKColors.YellowGreen.ToString(),
                [ColorPickerOption.Corpse] = SKColors.Silver.ToString(),
                [ColorPickerOption.MedsFilterLoot] = SKColors.LightSalmon.ToString(),
                [ColorPickerOption.FoodFilterLoot] = SKColors.CornflowerBlue.ToString(),
                [ColorPickerOption.BackpacksFilterLoot] = SKColor.Parse("00b02c").ToString(),
                [ColorPickerOption.Explosives] = SKColors.OrangeRed.ToString(),
                [ColorPickerOption.QuestHelperZones] = SKColors.DeepPink.ToString()
            };
        }

        /// <summary>
        /// Apply all colors from the config dictionary.
        /// </summary>
        private static void SetAllColors(IReadOnlyDictionary<ColorPickerOption, string> colors)
        {
            foreach (var color in colors)
            {
                if (!SKColor.TryParse(color.Value, out var skColor))
                    continue;

                ApplyColorToSKPaints(color.Key, skColor);
            }
        }

        /// <summary>
        /// Draw the color picker panel.
        /// </summary>
        public static void Draw()
        {
            bool isOpen = _state.IsColorPickerOpen;
            if (!ImGui.Begin("Color Picker", ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                _state.IsColorPickerOpen = isOpen;
                ImGui.End();
                return;
            }
            _state.IsColorPickerOpen = isOpen;

            ImGui.Text("Select a color option to edit:");

            // Color options list
            if (ImGui.BeginListBox("##ColorOptions", new Vector2(250, 300)))
            {
                foreach (var option in Enum.GetValues<ColorPickerOption>())
                {
                    string name = GetFriendlyName(option);
                    bool isSelected = _selectedOption == option;

                    // Show color preview
                    var currentColor = GetCurrentColor(option);
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(currentColor.X, currentColor.Y, currentColor.Z, 1f));

                    if (ImGui.Selectable($"● {name}", isSelected))
                    {
                        _selectedOption = option;
                        _editingColor = currentColor;
                        _hexInput = ColorToHex(currentColor);
                    }

                    ImGui.PopStyleColor();
                }
                ImGui.EndListBox();
            }

            ImGui.Separator();

            // Color editor
            if (_selectedOption.HasValue)
            {
                ImGui.Text($"Editing: {GetFriendlyName(_selectedOption.Value)}");

                // Color picker
                if (ImGui.ColorPicker3("##ColorPicker", ref _editingColor))
                {
                    _hexInput = ColorToHex(_editingColor);
                }

                // Hex input
                ImGui.SetNextItemWidth(100);
                if (ImGui.InputText("Hex", ref _hexInput, 10))
                {
                    if (TryParseHex(_hexInput, out var parsed))
                    {
                        _editingColor = parsed;
                    }
                }

                ImGui.Spacing();

                if (ImGui.Button("Apply"))
                {
                    ApplyColor(_selectedOption.Value, _editingColor);
                }
                ImGui.SameLine();
                if (ImGui.Button("Reset to Default"))
                {
                    _editingColor = GetDefaultColor(_selectedOption.Value);
                    _hexInput = ColorToHex(_editingColor);
                }
            }

            ImGui.End();
        }

        private static string GetFriendlyName(ColorPickerOption option)
        {
            return option switch
            {
                ColorPickerOption.LocalPlayer => "Local Player",
                ColorPickerOption.FriendlyPlayer => "Teammate",
                ColorPickerOption.PMCPlayer => "PMC (Enemy)",
                ColorPickerOption.ScavPlayer => "Scav",
                ColorPickerOption.HumanScavPlayer => "Player Scav",
                ColorPickerOption.BossPlayer => "Boss",
                ColorPickerOption.RaiderPlayer => "Raider/Guard",
                ColorPickerOption.WatchlistPlayer => "Watchlist",
                ColorPickerOption.StreamerPlayer => "Streamer",
                ColorPickerOption.FocusedPlayer => "Focused",
                ColorPickerOption.RegularLoot => "Regular Loot",
                ColorPickerOption.ValuableLoot => "Valuable Loot",
                ColorPickerOption.WishlistLoot => "Wishlist Loot",
                ColorPickerOption.ContainerLoot => "Container Loot",
                ColorPickerOption.MedsFilterLoot => "Meds",
                ColorPickerOption.FoodFilterLoot => "Food",
                ColorPickerOption.BackpacksFilterLoot => "Backpacks",
                ColorPickerOption.QuestHelperItems => "Quest Items",
                ColorPickerOption.QuestHelperZones => "Quest Zones",
                ColorPickerOption.Corpse => "Corpse",
                ColorPickerOption.DeathMarker => "Death Marker",
                ColorPickerOption.Explosives => "Explosives",
                _ => option.ToString()
            };
        }

        private static Vector3 GetCurrentColor(ColorPickerOption option)
        {
            if (Program.Config.RadarColors.TryGetValue(option, out var hex))
            {
                if (TryParseHex(hex, out var color))
                    return color;
            }
            return GetDefaultColor(option);
        }

        private static Vector3 GetDefaultColor(ColorPickerOption option)
        {
            return option switch
            {
                ColorPickerOption.LocalPlayer => new Vector3(0, 0.5f, 0),
                ColorPickerOption.FriendlyPlayer => new Vector3(0.196f, 0.804f, 0.196f),
                ColorPickerOption.PMCPlayer => new Vector3(1, 0, 0),
                ColorPickerOption.ScavPlayer => new Vector3(1, 1, 0),
                ColorPickerOption.HumanScavPlayer => new Vector3(1, 1, 1),
                ColorPickerOption.BossPlayer => new Vector3(1, 0, 1),
                ColorPickerOption.RaiderPlayer => new Vector3(1, 0.78f, 0.06f),
                ColorPickerOption.WatchlistPlayer => new Vector3(1, 0.41f, 0.71f),
                ColorPickerOption.StreamerPlayer => new Vector3(0.58f, 0.44f, 0.86f),
                ColorPickerOption.FocusedPlayer => new Vector3(1, 0.5f, 0.31f),
                ColorPickerOption.RegularLoot => new Vector3(0.96f, 0.96f, 0.96f),
                ColorPickerOption.ValuableLoot => new Vector3(0.25f, 0.88f, 0.82f),
                ColorPickerOption.WishlistLoot => new Vector3(0, 1, 0),
                ColorPickerOption.ContainerLoot => new Vector3(1, 1, 0.8f),
                ColorPickerOption.MedsFilterLoot => new Vector3(1, 0.63f, 0.48f),
                ColorPickerOption.FoodFilterLoot => new Vector3(0.39f, 0.58f, 0.93f),
                ColorPickerOption.BackpacksFilterLoot => new Vector3(0, 0.69f, 0.17f),
                ColorPickerOption.Corpse => new Vector3(0.75f, 0.75f, 0.75f),
                ColorPickerOption.DeathMarker => new Vector3(0, 0, 0),
                ColorPickerOption.QuestHelperItems => new Vector3(0.6f, 0.8f, 0.2f),
                ColorPickerOption.QuestHelperZones => new Vector3(1, 0.08f, 0.58f),
                ColorPickerOption.Explosives => new Vector3(1, 0.27f, 0),
                _ => Vector3.One
            };
        }

        private static void ApplyColor(ColorPickerOption option, Vector3 color)
        {
            string hex = ColorToHex(color);
            Program.Config.RadarColors[option] = hex;

            // Apply to SKPaint
            var skColor = new SKColor(
                (byte)(color.X * 255),
                (byte)(color.Y * 255),
                (byte)(color.Z * 255));

            ApplyColorToSKPaints(option, skColor);
        }

        private static void ApplyColorToSKPaints(ColorPickerOption option, SKColor skColor)
        {
            switch (option)
            {
                case ColorPickerOption.LocalPlayer:
                    SKPaints.PaintLocalPlayer.Color = skColor;
                    SKPaints.TextLocalPlayer.Color = skColor;
                    SKPaints.PaintAimviewWidgetLocalPlayer.Color = skColor;
                    break;
                case ColorPickerOption.FriendlyPlayer:
                    SKPaints.PaintTeammate.Color = skColor;
                    SKPaints.TextTeammate.Color = skColor;
                    SKPaints.PaintAimviewWidgetTeammate.Color = skColor;
                    break;
                case ColorPickerOption.PMCPlayer:
                    SKPaints.PaintPMC.Color = skColor;
                    SKPaints.TextPMC.Color = skColor;
                    SKPaints.PaintAimviewWidgetPMC.Color = skColor;
                    SKPaints.TextPlayersOverlayPMC.Color = skColor.AdjustBrightness(0.5f);
                    break;
                case ColorPickerOption.ScavPlayer:
                    SKPaints.PaintScav.Color = skColor;
                    SKPaints.TextScav.Color = skColor;
                    SKPaints.PaintAimviewWidgetScav.Color = skColor;
                    break;
                case ColorPickerOption.HumanScavPlayer:
                    SKPaints.PaintPScav.Color = skColor;
                    SKPaints.TextPScav.Color = skColor;
                    SKPaints.PaintAimviewWidgetPScav.Color = skColor;
                    SKPaints.TextPlayersOverlayPScav.Color = skColor.AdjustBrightness(0.5f);
                    break;
                case ColorPickerOption.BossPlayer:
                    SKPaints.PaintBoss.Color = skColor;
                    SKPaints.TextBoss.Color = skColor;
                    SKPaints.PaintAimviewWidgetBoss.Color = skColor;
                    break;
                case ColorPickerOption.RaiderPlayer:
                    SKPaints.PaintRaider.Color = skColor;
                    SKPaints.TextRaider.Color = skColor;
                    SKPaints.PaintAimviewWidgetRaider.Color = skColor;
                    break;
                case ColorPickerOption.WatchlistPlayer:
                    SKPaints.PaintWatchlist.Color = skColor;
                    SKPaints.TextWatchlist.Color = skColor;
                    SKPaints.PaintAimviewWidgetWatchlist.Color = skColor;
                    SKPaints.TextPlayersOverlaySpecial.Color = skColor.AdjustBrightness(0.5f);
                    break;
                case ColorPickerOption.StreamerPlayer:
                    SKPaints.PaintStreamer.Color = skColor;
                    SKPaints.TextStreamer.Color = skColor;
                    SKPaints.PaintAimviewWidgetStreamer.Color = skColor;
                    SKPaints.TextPlayersOverlayStreamer.Color = skColor.AdjustBrightness(0.5f);
                    break;
                case ColorPickerOption.FocusedPlayer:
                    SKPaints.PaintFocused.Color = skColor;
                    SKPaints.TextFocused.Color = skColor;
                    SKPaints.PaintAimviewWidgetFocused.Color = skColor;
                    SKPaints.TextPlayersOverlayFocused.Color = skColor.AdjustBrightness(0.5f);
                    break;
                case ColorPickerOption.RegularLoot:
                    SKPaints.PaintLoot.Color = skColor;
                    SKPaints.TextLoot.Color = skColor;
                    break;
                case ColorPickerOption.ValuableLoot:
                    SKPaints.PaintImportantLoot.Color = skColor;
                    SKPaints.TextImportantLoot.Color = skColor;
                    break;
                case ColorPickerOption.WishlistLoot:
                    SKPaints.PaintWishlistItem.Color = skColor;
                    SKPaints.TextWishlistItem.Color = skColor;
                    break;
                case ColorPickerOption.Corpse:
                    SKPaints.PaintCorpse.Color = skColor;
                    SKPaints.TextCorpse.Color = skColor;
                    break;
                case ColorPickerOption.MedsFilterLoot:
                    SKPaints.PaintMeds.Color = skColor;
                    SKPaints.TextMeds.Color = skColor;
                    break;
                case ColorPickerOption.FoodFilterLoot:
                    SKPaints.PaintFood.Color = skColor;
                    SKPaints.TextFood.Color = skColor;
                    break;
                case ColorPickerOption.BackpacksFilterLoot:
                    SKPaints.PaintBackpacks.Color = skColor;
                    SKPaints.TextBackpacks.Color = skColor;
                    break;
                case ColorPickerOption.QuestHelperItems:
                    SKPaints.PaintQuestItem.Color = skColor;
                    SKPaints.TextQuestItem.Color = skColor;
                    break;
                case ColorPickerOption.QuestHelperZones:
                    SKPaints.PaintQuestZone.Color = skColor;
                    break;
                case ColorPickerOption.Explosives:
                    SKPaints.PaintExplosives.Color = skColor;
                    break;
                case ColorPickerOption.DeathMarker:
                    SKPaints.PaintDeathMarker.Color = skColor;
                    break;
                case ColorPickerOption.ContainerLoot:
                    SKPaints.PaintContainerLoot.Color = skColor;
                    break;
            }
        }

        private static string ColorToHex(Vector3 color)
        {
            int r = (int)(color.X * 255);
            int g = (int)(color.Y * 255);
            int b = (int)(color.Z * 255);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        private static bool TryParseHex(string hex, out Vector3 color)
        {
            color = Vector3.One;
            if (string.IsNullOrEmpty(hex))
                return false;

            hex = hex.TrimStart('#');
            if (hex.Length != 6)
                return false;

            try
            {
                int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                color = new Vector3(r / 255f, g / 255f, b / 255f);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
