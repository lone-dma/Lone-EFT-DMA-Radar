/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
namespace LoneEftDmaRadar.UI.Hotkeys.Internal
{
    public sealed class HotkeyModeListItem
    {
        public string Name { get; }
        public EMode Mode { get; }
        public HotkeyModeListItem(EMode mode)
        {
            Name = mode.ToString();
            Mode = mode;
        }
        public override string ToString() => Name;


        public enum EMode
        {
            /// <summary>
            /// Continuous Hold the hotkey.
            /// </summary>
            Hold = 1,
            /// <summary>
            /// Toggle hotkey on/off.
            /// </summary>
            Toggle = 2
        }
    }
}

