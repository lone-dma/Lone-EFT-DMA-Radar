/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
namespace LoneEftDmaRadar.Web.WebRadar.Data
{
    /// <summary>
    /// Defines Player Unit Type (Player,PMC,Scav,etc.)
    /// </summary>
    public enum WebPlayerType : int
    {
        /// <summary>
        /// Bot Player.
        /// </summary>
        Bot = 0,
        /// <summary>
        /// LocalPlayer running the Web Server.
        /// </summary>
        LocalPlayer = 1,
        /// <summary>
        /// Teammate of LocalPlayer.
        /// </summary>
        Teammate = 2,
        /// <summary>
        /// Human-Controlled Player.
        /// </summary>
        Player = 3,
        /// <summary>
        /// Human-Controlled Scav.
        /// </summary>
        PlayerScav = 4
    }
}

