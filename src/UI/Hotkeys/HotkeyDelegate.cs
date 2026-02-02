/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
namespace LoneEftDmaRadar.UI.Hotkeys
{
    /// <summary>
    /// Represents a method that is called when a hotkey is activated or deactivated.
    /// </summary>
    /// <param name="isKeyDown">A value indicating whether the hotkey is currently active. <see langword="true"/> if the hotkey is pressed;
    /// otherwise, <see langword="false"/>.</param>
    public delegate void HotkeyDelegate(bool isKeyDown);
}

