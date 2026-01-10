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
        // Panel-local state
        private static Vector3 _editingColor = Vector3.One;
        private static ColorPickerOption? _selectedOption;
        private static string _hexInput = "#FFFFFF";

        private static EftDmaConfig Config { get; } = Program.Config;

        /// <summary>
        /// Whether the color picker panel is open.
        /// </summary>
        public static bool IsOpen { get; set; }

        /// <summary>
        /// Initialize colors from config. Call once at startup.
        /// </summary>
        public static void Initialize()
        {
            // Add default colors for any missing entries
            foreach (var defaultColor in GetDefaultColors())
                Config.RadarColors.TryAdd(defaultColor.Key, defaultColor.Value);

            // Apply all colors from config
            SetAllColors(Config.RadarColors);
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
                [ColorPickerOption.QuestHelperZones] = SKColors.DeepPink.ToString(),
                [ColorPickerOption.MapPing] = SKColors.Turquoise.ToString(),
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
            bool isOpen = IsOpen;
            if (!ImGui.Begin("Color Picker", ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                IsOpen = isOpen;
                ImGui.End();
                return;
            }
            IsOpen = isOpen;

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
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Select a UI element to customize its color");

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
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Enter color as hex code (e.g., #FF0000)");

                ImGui.Spacing();

                if (ImGui.Button("Apply"))
                {
                    ApplyColor(_selectedOption.Value, _editingColor);
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Apply the selected color");
                ImGui.SameLine();
                if (ImGui.Button("Reset to Default"))
                {
                    _editingColor = GetDefaultColor(_selectedOption.Value);
                    _hexInput = ColorToHex(_editingColor);
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Reset to the default color");
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
                ColorPickerOption.MapPing => "Map Ping",
                _ => option.ToString()
            };
        }

        private static Vector3 GetCurrentColor(ColorPickerOption option)
        {
            if (Config.RadarColors.TryGetValue(option, out var hex))
            {
                if (TryParseHex(hex, out var color))
                    return color;
            }
            return GetDefaultColor(option);
        }

        private static Vector3 GetDefaultColor(ColorPickerOption option)
        {
            SKColor color = option switch
            {
                ColorPickerOption.LocalPlayer => SKColors.Green,
                ColorPickerOption.FriendlyPlayer => SKColors.LimeGreen,
                ColorPickerOption.PMCPlayer => SKColors.Red,
                ColorPickerOption.ScavPlayer => SKColors.Yellow,
                ColorPickerOption.HumanScavPlayer => SKColors.White,
                ColorPickerOption.BossPlayer => SKColors.Fuchsia,
                ColorPickerOption.RaiderPlayer => SKColor.Parse("ffc70f"),
                ColorPickerOption.FocusedPlayer => SKColors.Coral,
                ColorPickerOption.RegularLoot => SKColors.WhiteSmoke,
                ColorPickerOption.ValuableLoot => SKColors.Turquoise,
                ColorPickerOption.WishlistLoot => SKColors.Lime,
                ColorPickerOption.ContainerLoot => SKColor.Parse("FFFFCC"),
                ColorPickerOption.MedsFilterLoot => SKColors.LightSalmon,
                ColorPickerOption.FoodFilterLoot => SKColors.CornflowerBlue,
                ColorPickerOption.BackpacksFilterLoot => SKColor.Parse("00b02c"),
                ColorPickerOption.Corpse => SKColors.Silver,
                ColorPickerOption.DeathMarker => SKColors.Black,
                ColorPickerOption.QuestHelperItems => SKColors.YellowGreen,
                ColorPickerOption.QuestHelperZones => SKColors.DeepPink,
                ColorPickerOption.Explosives => SKColors.OrangeRed,
                ColorPickerOption.MapPing => SKColors.Turquoise,
                _ => SKColors.White
            };

            return new Vector3(color.Red / 255f, color.Green / 255f, color.Blue / 255f);
        }

        private static void ApplyColor(ColorPickerOption option, Vector3 color)
        {
            string hex = ColorToHex(color);
            Config.RadarColors[option] = hex;

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
                case ColorPickerOption.FocusedPlayer:
                    SKPaints.PaintFocused.Color = skColor;
                    SKPaints.TextFocused.Color = skColor;
                    SKPaints.PaintAimviewWidgetFocused.Color = skColor;
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
                case ColorPickerOption.MapPing:
                    SKPaints.PaintMapPing.Color = skColor;
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
