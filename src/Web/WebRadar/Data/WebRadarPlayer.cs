/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using LoneEftDmaRadar.Tarkov.World.Player;

namespace LoneEftDmaRadar.Web.WebRadar.Data
{
    public struct WebRadarPlayer
    {
        /// <summary>
        /// Player Name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        /// <summary>
        /// Player Type (PMC, Scav,etc.)
        /// </summary>
        [JsonPropertyName("type")]
        public WebPlayerType Type { get; set; }
        /// <summary>
        /// True if player is active, otherwise False.
        /// </summary>
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
        /// <summary>
        /// True if player is alive, otherwise False.
        /// </summary>
        [JsonPropertyName("isAlive")]
        public bool IsAlive { get; set; }
        /// <summary>
        /// Unity World Position.
        /// </summary>
        [JsonPropertyName("position")]
        public Vector3 Position { get; set; }
        /// <summary>
        /// Unity World Rotation.
        /// </summary>
        [JsonPropertyName("rotation")]
        public Vector2 Rotation { get; set; }

        /// <summary>
        /// Create a WebRadarPlayer from a Full Player Object.
        /// </summary>
        /// <param name="player">Full EFT Player Object.</param>
        /// <returns>Compact WebRadarPlayer object.</returns>
        public static WebRadarPlayer Create(AbstractPlayer player)
        {
            WebPlayerType type = player is LocalPlayer ?
                WebPlayerType.LocalPlayer : player.IsFriendly ?
                WebPlayerType.Teammate : player.IsHuman ?
                player.IsScav ?
                WebPlayerType.PlayerScav : WebPlayerType.Player : WebPlayerType.Bot;
            return new WebRadarPlayer
            {
                Name = player.Name,
                Type = type,
                IsActive = player.IsActive,
                IsAlive = player.IsAlive,
                Position = player.Position,
                Rotation = player.Rotation
            };
        }
    }
}

