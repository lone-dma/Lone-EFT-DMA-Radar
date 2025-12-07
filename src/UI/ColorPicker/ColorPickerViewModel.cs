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

using LoneEftDmaRadar.UI.Misc;
using LoneEftDmaRadar.UI.Skia;
using SkiaSharp.Views.WPF;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;

namespace LoneEftDmaRadar.UI.ColorPicker
{
    public sealed class ColorPickerViewModel : INotifyPropertyChanged
    {
        private readonly ColorPickerWindow _parent;

        public ColorPickerViewModel(ColorPickerWindow parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            Options = new ObservableCollection<ColorPickerOption>(Enum.GetValues<ColorPickerOption>()
                .Cast<ColorPickerOption>()
                .ToList());
            SelectedOption = Options.FirstOrDefault();
            CloseCommand = new SimpleCommand(OnClose);
        }

        public ObservableCollection<ColorPickerOption> Options { get; }

        ColorPickerOption _selectedOption;
        public ColorPickerOption SelectedOption
        {
            get => _selectedOption;
            set
            {
                if (_selectedOption.Equals(value)) return;
                _selectedOption = value;
                OnPropertyChanged(nameof(SelectedOption));

                if (App.Config.RadarColors.TryGetValue(value, out var hex) && SKColor.TryParse(hex, out var skColor))
                {
                    SelectedMediaColor = skColor.ToColor();
                }
            }
        }

        Color _selectedMediaColor;
        public Color SelectedMediaColor
        {
            get => _selectedMediaColor;
            set
            {
                if (_selectedMediaColor.Equals(value)) return;
                _selectedMediaColor = value;
                if (App.Config.RadarColors.ContainsKey(SelectedOption))
                {
                    App.Config.RadarColors[SelectedOption] = value.ToSKColor().ToString();
                }
                OnPropertyChanged(nameof(SelectedMediaColor));
            }
        }

        public ICommand CloseCommand { get; }
        void OnClose()
        {
            // validate all
            foreach (var kv in App.Config.RadarColors)
            {
                _ = SKColor.Parse(kv.Value); // throws if invalid
            }
            SetColors(App.Config.RadarColors);
            _parent.DialogResult = true;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged(string n)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        #endregion

        #region Static Interfaces

        /// <summary>
        /// Load all ESP Color Config. Run once at start of application.
        /// </summary>
        static ColorPickerViewModel()
        {
            foreach (var defaultColor in GetDefaultColors())
                App.Config.RadarColors.TryAdd(defaultColor.Key, defaultColor.Value);
            SetColors(App.Config.RadarColors);
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
                [ColorPickerOption.WishlistLoot] = SKColors.Red.ToString(),
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
        /// Save all ESP Color Changes.
        /// </summary>
        internal static void SetColors(IReadOnlyDictionary<ColorPickerOption, string> colors)
        {
            try
            {
                foreach (var color in colors)
                {
                    if (!SKColor.TryParse(color.Value, out var skColor))
                        continue;
                    switch (color.Key)
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
                        case ColorPickerOption.HumanScavPlayer:
                            SKPaints.PaintPScav.Color = skColor;
                            SKPaints.TextPScav.Color = skColor;
                            SKPaints.PaintAimviewWidgetPScav.Color = skColor;
                            SKPaints.TextPlayersOverlayPScav.Color = skColor.AdjustBrightness(0.5f);
                            break;
                        case ColorPickerOption.ScavPlayer:
                            SKPaints.PaintScav.Color = skColor;
                            SKPaints.TextScav.Color = skColor;
                            SKPaints.PaintAimviewWidgetScav.Color = skColor;
                            break;
                        case ColorPickerOption.RaiderPlayer:
                            SKPaints.PaintRaider.Color = skColor;
                            SKPaints.TextRaider.Color = skColor;
                            SKPaints.PaintAimviewWidgetRaider.Color = skColor;
                            break;
                        case ColorPickerOption.BossPlayer:
                            SKPaints.PaintBoss.Color = skColor;
                            SKPaints.TextBoss.Color = skColor;
                            SKPaints.PaintAimviewWidgetBoss.Color = skColor;
                            break;
                        case ColorPickerOption.FocusedPlayer:
                            SKPaints.PaintFocused.Color = skColor;
                            SKPaints.TextFocused.Color = skColor;
                            SKPaints.PaintAimviewWidgetFocused.Color = skColor;
                            SKPaints.TextPlayersOverlayFocused.Color = skColor.AdjustBrightness(0.5f);
                            break;
                        case ColorPickerOption.DeathMarker:
                            SKPaints.PaintDeathMarker.Color = skColor;
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
                        case ColorPickerOption.Explosives:
                            SKPaints.PaintExplosives.Color = skColor;
                            break;
                        case ColorPickerOption.ContainerLoot:
                            SKPaints.PaintContainerLoot.Color = skColor;
                            break;
                        case ColorPickerOption.QuestHelperZones:
                            SKPaints.PaintQuestZone.Color = skColor;
                            break;
                        case ColorPickerOption.QuestHelperItems:
                            SKPaints.PaintQuestItem.Color = skColor;
                            SKPaints.TextQuestItem.Color = skColor;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("ERROR Setting Radar Colors", ex);
            }
        }

        #endregion
    }
}
