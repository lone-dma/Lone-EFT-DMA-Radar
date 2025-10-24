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

using LoneEftDmaRadar.Tarkov.GameWorld.Player;
using MessagePack;

namespace LoneEftDmaRadar.Web.WebRadar.Data
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
