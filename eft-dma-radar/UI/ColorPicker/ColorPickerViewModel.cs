using eft_dma_radar.UI.Misc;
using eft_dma_radar.UI.Skia;
using SkiaSharp.Views.WPF;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;

namespace eft_dma_radar.UI.ColorPicker
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
                [ColorPickerOption.QuestLoot] = SKColors.YellowGreen.ToString(),
                [ColorPickerOption.StaticQuestItemsAndZones] = SKColors.DeepPink.ToString(),
                [ColorPickerOption.Corpse] = SKColors.Silver.ToString(),
                [ColorPickerOption.MedsFilterLoot] = SKColors.LightSalmon.ToString(),
                [ColorPickerOption.FoodFilterLoot] = SKColors.CornflowerBlue.ToString(),
                [ColorPickerOption.BackpacksFilterLoot] = SKColor.Parse("00b02c").ToString(),
                [ColorPickerOption.Explosives] = SKColors.OrangeRed.ToString(),
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
                            SKPaints.PaintESPWidgetLocalPlayer.Color = skColor;
                            break;
                        case ColorPickerOption.FriendlyPlayer:
                            SKPaints.PaintTeammate.Color = skColor;
                            SKPaints.TextTeammate.Color = skColor;
                            SKPaints.PaintESPWidgetTeammate.Color = skColor;
                            break;
                        case ColorPickerOption.PMCPlayer:
                            SKPaints.PaintPMC.Color = skColor;
                            SKPaints.TextPMC.Color = skColor;
                            SKPaints.PaintESPWidgetPMC.Color = skColor;
                            break;
                        case ColorPickerOption.WatchlistPlayer:
                            SKPaints.PaintWatchlist.Color = skColor;
                            SKPaints.TextWatchlist.Color = skColor;
                            SKPaints.PaintESPWidgetWatchlist.Color = skColor;
                            break;
                        case ColorPickerOption.StreamerPlayer:
                            SKPaints.PaintStreamer.Color = skColor;
                            SKPaints.TextStreamer.Color = skColor;
                            SKPaints.PaintESPWidgetStreamer.Color = skColor;
                            break;
                        case ColorPickerOption.HumanScavPlayer:
                            SKPaints.PaintPScav.Color = skColor;
                            SKPaints.TextPScav.Color = skColor;
                            SKPaints.PaintESPWidgetPScav.Color = skColor;
                            break;
                        case ColorPickerOption.ScavPlayer:
                            SKPaints.PaintScav.Color = skColor;
                            SKPaints.TextScav.Color = skColor;
                            SKPaints.PaintESPWidgetScav.Color = skColor;
                            break;
                        case ColorPickerOption.RaiderPlayer:
                            SKPaints.PaintRaider.Color = skColor;
                            SKPaints.TextRaider.Color = skColor;
                            SKPaints.PaintESPWidgetRaider.Color = skColor;
                            break;
                        case ColorPickerOption.BossPlayer:
                            SKPaints.PaintBoss.Color = skColor;
                            SKPaints.TextBoss.Color = skColor;
                            SKPaints.PaintESPWidgetBoss.Color = skColor;
                            break;
                        case ColorPickerOption.FocusedPlayer:
                            SKPaints.PaintFocused.Color = skColor;
                            SKPaints.TextFocused.Color = skColor;
                            SKPaints.PaintESPWidgetFocused.Color = skColor;
                            break;
                        case ColorPickerOption.DeathMarker:
                            SKPaints.PaintDeathMarker.Color = skColor;
                            break;
                        case ColorPickerOption.RegularLoot:
                            SKPaints.PaintLoot.Color = skColor;
                            SKPaints.TextLoot.Color = skColor;
                            SKPaints.PaintESPWidgetLoot.Color = skColor;
                            SKPaints.TextESPWidgetLoot.Color = skColor;
                            break;
                        case ColorPickerOption.ValuableLoot:
                            SKPaints.PaintImportantLoot.Color = skColor;
                            SKPaints.TextImportantLoot.Color = skColor;
                            break;
                        case ColorPickerOption.WishlistLoot:
                            SKPaints.PaintWishlistItem.Color = skColor;
                            SKPaints.TextWishlistItem.Color = skColor;
                            break;
                        case ColorPickerOption.QuestLoot:
                            SKPaints.PaintQuestItem.Color = skColor;
                            SKPaints.TextQuestItem.Color = skColor;
                            break;
                        case ColorPickerOption.StaticQuestItemsAndZones:
                            SKPaints.QuestHelperPaint.Color = skColor;
                            SKPaints.QuestHelperText.Color = skColor;
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
