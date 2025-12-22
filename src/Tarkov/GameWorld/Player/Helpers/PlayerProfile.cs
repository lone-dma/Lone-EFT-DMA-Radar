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

using LoneEftDmaRadar.Web.ProfileApi;
using LoneEftDmaRadar.Web.Twitch;
using System.Collections.Frozen;

namespace LoneEftDmaRadar.Tarkov.GameWorld.Player.Helpers
{
    public sealed class PlayerProfile
    {
        private static readonly FrozenDictionary<string, HighAchiev> _highAchievements = new Dictionary<string, HighAchiev>(StringComparer.OrdinalIgnoreCase)
        {
            /// Very hard tasks, only veterans will have these (1k+ hrs)
            ["68d3ff840531ed76e808866c"] = new("No Limit to Perfection", 2),    // Prestige 6
            ["68d3fe84757f8967ec09099b"] = new("Five Plus", 2),                 // Prestige 5
            ["68e8f02ff3a1196d1a05f2cb"] = new("Survivor", 2),                  // Escaped From Tarkov
            ["68e8f042b8efa2bbeb009d89"] = new("Fallen", 2),                    // Escaped From Tarkov
            ["68e8f04eb841bc8ac305350a"] = new("Debtor", 2),                    // Escaped From Tarkov
            ["68e8f0575eb7e5ce5000ba0a"] = new("Savior", 2),                    // Escaped From Tarkov
            ["6529097eccf6aa5f8737b3d0"] = new("Snowball", 2),
            ["6514143d59647d2cb3213c93"] = new("Master of ULTRA", 2),
            ["6514174fb1c08b0feb216d73"] = new("Chris's Heir", 2),
            /// Hard-ish tasks but do-able for dedicated players
            ["6514184ec31fcb0e163577d2"] = new("Killer7", 1),
            ["676091c0f457869a94017a23"] = new("Prestigious", 1),
            ["6514321bec10ff011f17ccac"] = new("Firefly", 1),
            ["651415feb49e3253755f4b68"] = new("Long Live The King!", 1),
            ["664f1f8768508d74604bf556"] = new("The Kappa Path", 1)
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        private readonly ObservedPlayer _player;

        public bool IsStandard => !IsEOD && !IsUnheard;
        public bool IsEOD => MemberCategory is Enums.EMemberCategory mc && (mc & Enums.EMemberCategory.UniqueId) == Enums.EMemberCategory.UniqueId;
        public bool IsUnheard => MemberCategory is Enums.EMemberCategory mc && (mc & Enums.EMemberCategory.Unheard) == Enums.EMemberCategory.Unheard;

        public PlayerProfile(ObservedPlayer player, string accountId)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            AccountID = accountId ?? throw new ArgumentNullException(nameof(accountId));
        }

        /// <summary>
        /// Pulls data from EFTProfileService into our properties.
        /// </summary>
        private void RefreshProfile()
        {
            // --- Nickname ---
            var nick = Data?.Info?.Nickname;
            if (!string.IsNullOrEmpty(nick))
                Name = nick;

            var stats = Data?.PmcStats;
            // --- Overall KD ---
            var items = stats?.Counters?.OverallCounters?.Items;
            if (items is not null)
            {
                var kills = items.FirstOrDefault(x => x.Key?.Contains("Kills") == true)?.Value;
                var deaths = items.FirstOrDefault(x => x.Key?.Contains("Deaths") == true)?.Value;
                if (kills is int k && deaths is int d)
                    Overall_KD = d == 0 ? k : k / (float)d;
            }

            // --- Raid Count ---
            var sessions = stats?.Counters?.OverallCounters?.Items?
                .FirstOrDefault(x => x.Key?.Contains("Sessions") == true)?.Value;
            if (sessions is int s)
                RaidCount = s;

            // --- Survival Rate ---
            // first, capture survived count
            var surv = Data?.PmcStats?.Counters?.OverallCounters?.Items?
                .FirstOrDefault(x => x.Key?.Contains("Survived") == true)?.Value;
            if (surv is int sc)
                SurvivedCount = sc;
            // then compute percentage
            var rc = RaidCount ?? 0;
            SurvivedRate = rc == 0
                ? 0f
                : SurvivedCount.GetValueOrDefault() / (float)rc * 100f;

            // --- Hours Played ---
            if (Data?.PmcStats?.Counters?.TotalInGameTime is int totalTime)
            {
                Hours = (int)Math.Round(totalTime / 3600f); // Seconds to hours
            }

            // --- Level ---
            if (Data?.Info?.Experience is int xp)
            {
                Level = TarkovDataManager.XPTable
                    .Where(x => x.Key > xp)
                    .Select(x => x.Value)
                    .FirstOrDefault() - 1;
            }

            // --- Member Category ---
            var info = Data?.Info;
            if (info is not null)
                MemberCategory = (Enums.EMemberCategory)info.MemberCategory;

            // --- Account Type ("UH", "EOD", or "--") ---
            if (IsUnheard)
                Acct = "UH";
            else if (IsEOD)
                Acct = "EOD";
            else
                Acct = "--";

            // --- Achievement Level ---
            int achievLevel = 0;
            var highAchievList = new List<string>();
            if (Data?.Achievements is Dictionary<string, long> playerAchievs && playerAchievs.Count > 0)
            {
                foreach (var kvp in _highAchievements)
                {
                    if (playerAchievs.ContainsKey(kvp.Key))
                    {
                        if (kvp.Value.Level > achievLevel)
                            achievLevel = kvp.Value.Level;

                        string prefix = kvp.Value.Level switch
                        {
                            2 => "++",
                            1 => "+",
                            _ => ""
                        };
                        highAchievList.Add($"{prefix}{kvp.Value.Name}");
                    }
                }
            }
            AchievLevel = achievLevel;
            HighAchievs = highAchievList;
        }

        /// <summary>
        /// Focuses the player if they meet "suspicious" criteria.
        /// Can be clicked off by the user.
        /// </summary>
        private void FocusIfSus()
        {
            if (_player.Type is not PlayerType.PMC or PlayerType.PScav)
                return;
            float kd = Overall_KD ?? 5f; // Default to average KD
            int hrs = Hours ?? 0;
            float sr = SurvivedRate ?? 50f; // Default to average survival rate
            int achievLevel = AchievLevel;
            if (kd >= 15f) // Excessive KD (or they are just really good and we should watch them anyway!)
            {
                _player.IsFocused = true;
            }
            else if (hrs < 30 && IsStandard) // Low hrs played on std account, could be a brand new cheater account
            {
                _player.IsFocused = true;
            }
            else if (sr >= 65f) // Very high survival rate
            {
                _player.IsFocused = true;
            }
            else if (kd >= 10f && sr < 35f) // Possible KD Dropping
            {
                _player.IsFocused = true;
            }
            else if (hrs >= 1000 && sr < 25f) // Possible KD Dropping
            {
                _player.IsFocused = true;
            }
            else if (achievLevel >= 2 && hrs < 1000) // Very High achievement level but not enough hrs to have legitimately earned them
            {
                _player.IsFocused = true;
            }
            else if (achievLevel >= 1 && hrs < 100) // High achievement level but not enough hrs to have legitimately earned them
            {
                _player.IsFocused = true;
            }
        }

        private void RefreshMemberCategory(Enums.EMemberCategory memberCategory)
        {
            try
            {
                string alert = null;
                if ((memberCategory & Enums.EMemberCategory.Developer) == Enums.EMemberCategory.Developer)
                {
                    alert = "Developer Account";
                    Type = PlayerType.SpecialPlayer;
                }
                else if ((memberCategory & Enums.EMemberCategory.Sherpa) == Enums.EMemberCategory.Sherpa)
                {
                    alert = "Sherpa Account";
                    Type = PlayerType.SpecialPlayer;
                }
                else if ((memberCategory & Enums.EMemberCategory.Emissary) == Enums.EMemberCategory.Emissary)
                {
                    alert = "Emissary Account";
                    Type = PlayerType.SpecialPlayer;
                }
                _player.UpdateAlerts(alert);
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"ERROR updating Member Category for '{Name}': {ex}");
            }
        }

        /// <summary>
        /// Runs the Twitch Lookup for the Player's Nickname.
        /// </summary>
        private async Task RunTwitchLookupAsync(string nickname)
        {
            string twitchLogin = await TwitchService.LookupAsync(nickname);
            if (twitchLogin is not null)
            {
                TwitchChannelURL = $"https://twitch.tv/{twitchLogin}";
                _player.UpdateAlerts($"TTV @ {TwitchChannelURL}");
                if (Type != PlayerType.SpecialPlayer)
                    Type = PlayerType.Streamer; // Flag streamers
            }
        }

        #region Properties

        private ProfileApiTypes.ProfileData _data;
        public ProfileApiTypes.ProfileData Data
        {
            get => _data;
            set
            {
                if (_data == value) return;
                _data = value;
                RefreshProfile();
                if (Program.Config.UI.MarkSusPlayers)
                    FocusIfSus();
            }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                if (_player.IsHuman)
                    _ = Task.Run(() => RunTwitchLookupAsync(value));
            }
        }

        public PlayerType Type { get; set; }
        public string AccountID { get; private set; }
        public int GroupID { get; set; } = -1;
        public Enums.EPlayerSide PlayerSide { get; set; }
        public string Alerts { get; set; }
        public string TwitchChannelURL { get; set; }
        public float? Overall_KD { get; private set; }
        public int? RaidCount { get; private set; }
        private int? SurvivedCount { get; set; }
        public float? SurvivedRate { get; private set; }
        public int? Hours { get; private set; }
        public int? Level { get; private set; }

        private Enums.EMemberCategory? _memberCategory;
        public Enums.EMemberCategory? MemberCategory
        {
            get => _memberCategory;
            private set
            {
                if (_memberCategory == value) return;
                if (value is Enums.EMemberCategory cat)
                {
                    _memberCategory = cat;
                    RefreshMemberCategory(cat);
                }
            }
        }

        public string Acct { get; private set; } = "--";
        public int AchievLevel { get; set; }
        public IReadOnlyList<string> HighAchievs { get; private set; }

        /// <summary>
        /// A representation of a high-level achievement.
        /// </summary>
        /// <param name="Name">Achievement name.</param>
        /// <param name="Level">Achievement level.</param>
        private record HighAchiev(string Name, int Level);

        #endregion
    }
}
