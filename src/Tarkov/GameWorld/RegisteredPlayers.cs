/*
 * EFT DMA Radar Lite
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

using Collections.Pooled;
using EftDmaRadarLite.Tarkov.Player;
using EftDmaRadarLite.Unity.Collections;

namespace EftDmaRadarLite.Tarkov.GameWorld
{
    public sealed class RegisteredPlayers : IReadOnlyCollection<PlayerBase>
    {
        #region Fields/Properties/Constructor

        public static implicit operator ulong(RegisteredPlayers x) => x.Base;
        private ulong Base { get; }
        private readonly LocalGameWorld _game;
        private readonly ConcurrentDictionary<ulong, PlayerBase> _players = new();

        /// <summary>
        /// LocalPlayer Instance.
        /// </summary>
        public LocalPlayer LocalPlayer { get; }

        /// <summary>
        /// RegisteredPlayers List Constructor.
        /// </summary>
        public RegisteredPlayers(ulong baseAddr, LocalGameWorld game)
        {
            Base = baseAddr;
            _game = game;
            var mainPlayer = Memory.ReadPtr(_game + Offsets.ClientLocalGameWorld.MainPlayer, false);
            var localPlayer = new LocalPlayer(mainPlayer);
            _players[localPlayer] = LocalPlayer = localPlayer;
        }

        #endregion

        /// <summary>
        /// Updates the ConcurrentDictionary of 'Players'
        /// </summary>
        public void Refresh()
        {
            try
            {
                using var playersList = UnityList<ulong>.Create(this, false); // Realtime Read
                using var registered = playersList.Where(x => x != 0x0).ToPooledSet();
                /// Allocate New Players
                foreach (var playerBase in registered)
                {
                    if (playerBase == LocalPlayer) // Skip LocalPlayer, already allocated
                        continue;
                    if (!_players.ContainsKey(playerBase)) // Add New Player
                    {
                        PlayerBase.Allocate(_players, playerBase);
                    }
                }
                /// Update Existing Players incl LocalPlayer
                UpdateExistingPlayers(registered);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CRITICAL ERROR - RegisteredPlayers Loop FAILED: {ex}");
            }
        }

        /// <summary>
        /// Returns the Player Count currently in the Registered Players List.
        /// </summary>
        /// <returns>Count of players.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public int GetPlayerCount()
        {
            var count = Memory.ReadValue<int>(this + UnityList<byte>.CountOffset, false);
            if (count < 0 || count > 256)
                throw new ArgumentOutOfRangeException(nameof(count));
            return count;
        }

        /// <summary>
        /// Scans the existing player list and updates Players as needed.
        /// </summary>
        private void UpdateExistingPlayers(ISet<ulong> registered)
        {
            var allPlayers = _players.Values;
            if (allPlayers.Count == 0)
                return;
            using var map = Memory.GetScatterMap();
            var round1 = map.AddRound(false);
            int i = 0;
            foreach (var player in allPlayers)
            {
                player.OnRegRefresh(round1[i++], registered);
            }
            map.Execute();
        }

        /// <summary>
        /// Checks if there is an existing BTR player in the Players Dictionary, and if not, it is allocated and swapped.
        /// </summary>
        /// <param name="btrPlayerBase">Player Base Addr for BTR Operator.</param>
        public void TryAllocateBTR(ulong btrView, ulong btrPlayerBase)
        {
            if (_players.TryGetValue(btrPlayerBase, out var existing) && existing is not BtrOperator)
            {
                var btr = new BtrOperator(btrView, btrPlayerBase);
                _players[btrPlayerBase] = btr;
                Debug.WriteLine("BTR Allocated!");
            }
        }

        #region IReadOnlyCollection
        public int Count => _players.Values.Count;
        public IEnumerator<PlayerBase> GetEnumerator() =>
            _players.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}
