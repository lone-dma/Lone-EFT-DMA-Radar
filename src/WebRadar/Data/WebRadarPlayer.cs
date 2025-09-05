using EftDmaRadarLite.Tarkov.Player;
using MessagePack;

namespace EftDmaRadarLite.WebRadar.Data
{
    [MessagePackObject]
    public readonly struct WebRadarPlayer
    {
        /// <summary>
        /// Player Name.
        /// </summary>
        [Key(0)]
        public readonly string Name { get; init; }
        /// <summary>
        /// Player Type (PMC, Scav,etc.)
        /// </summary>
        [Key(1)]
        public readonly WebPlayerType Type { get; init; }
        /// <summary>
        /// True if player is active, otherwise False.
        /// </summary>
        [Key(2)]
        public readonly bool IsActive { get; init; }
        /// <summary>
        /// True if player is alive, otherwise False.
        /// </summary>
        [Key(3)]
        public readonly bool IsAlive { get; init; }
        /// <summary>
        /// Unity World Position.
        /// </summary>
        [Key(4)]
        public readonly Vector3 Position { get; init; }
        /// <summary>
        /// Unity World Rotation.
        /// </summary>
        [Key(5)]
        public readonly Vector2 Rotation { get; init; }

        /// <summary>
        /// Create a WebRadarPlayer from a Full Player Object.
        /// </summary>
        /// <param name="player">Full EFT Player Object.</param>
        /// <returns>Compact WebRadarPlayer object.</returns>
        public static WebRadarPlayer Create(PlayerBase player)
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
