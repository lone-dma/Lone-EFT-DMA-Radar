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

using Collections.Pooled;
using LoneEftDmaRadar.Web.ProfileApi.Schema;
using LoneEftDmaRadar.Web.Twitch;

namespace LoneEftDmaRadar.Tarkov.GameWorld.Player.Helpers
{
    public sealed class PlayerProfile : INotifyPropertyChanged
    {
        private static readonly ConcurrentDictionary<string, int> _highAchievements = new(StringComparer.OrdinalIgnoreCase)
        {
            /// Very hard tasks, only veterans will have these (1k+ hours)
            ["68d3ff840531ed76e808866c"] = 2, // No Limit to Perfection
            ["68d3fe84757f8967ec09099b"] = 2, // Five Plus
            ["68e8f02ff3a1196d1a05f2cb"] = 2, // Survivor
            ["68e8f042b8efa2bbeb009d89"] = 2, // Fallen
            ["68e8f04eb841bc8ac305350a"] = 2, // Debtor
            ["68e8f0575eb7e5ce5000ba0a"] = 2, // Savior
            ["6529097eccf6aa5f8737b3d0"] = 2, // Snowball
            ["6514143d59647d2cb3213c93"] = 2, // Master of ULTRA
            ["6514174fb1c08b0feb216d73"] = 2, // Chris's Heir
            /// Hard-ish tasks but do-able for dedicated players
            ["6514184ec31fcb0e163577d2"] = 1, // Killer7
            ["676091c0f457869a94017a23"] = 1, // Prestigious
            ["6514321bec10ff011f17ccac"] = 1, // Firefly
            ["651415feb49e3253755f4b68"] = 1, // Long Live The King!
            ["664f1f8768508d74604bf556"] = 1  // The Kappa Path
        };

        private readonly ObservedPlayer _player;
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

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
            var totalTime = Data?.PmcStats?.Counters?.TotalInGameTime;
            if (totalTime.HasValue && totalTime.Value > 0)
                Hours = (int)Math.Round(totalTime.Value / 3600f);

            // --- Level ---
            var xp = Data?.Info?.Experience;
            if (xp.HasValue)
                Level = TarkovDataManager.XPTable
                    .Where(x => x.Key > xp.Value)
                    .Select(x => x.Value)
                    .FirstOrDefault() - 1;

            // --- Member Category ---
            var info = Data?.Info;
            if (info is not null)
                MemberCategory = (Enums.EMemberCategory)info.MemberCategory;

            // --- Account Type ("UH", "EOD", or "--") ---
            var mc = MemberCategory ?? Enums.EMemberCategory.Default;
            if (IsUnheard)
                Acct = "UH";
            else if (IsEOD)
                Acct = "EOD";
            else
                Acct = "--";

            // --- Achievement Level ---
            int achievLevel = 0;
            if (Data?.Achievements is Dictionary<string, long> achievs)
            {
                Debug.WriteLine($"Found {achievs.Count} achievements for player '{Name}'");
                using var matches = _highAchievements
                    .Where(kvp => achievs.ContainsKey(kvp.Key))
                    .Select(kvp => kvp.Value)
                    .ToPooledList();
                if (matches.Count > 0)
                {
                    achievLevel = matches.Max();
                }
            }
            AchievLevel = achievLevel;
        }

        /// <summary>
        /// Focuses the player if they meet "suspicious" criteria.
        /// Can be clicked off by the user.
        /// </summary>
        private void FocusIfSus()
        {
            if (_player.Type is not PlayerType.PMC or PlayerType.PScav)
                return;
            float overallKd = Overall_KD ?? 0f;
            int hours = Hours ?? 0;
            float sr = SurvivedRate ?? 0f;
            if (overallKd >= 15f) // Excessive KD (or they are just really good and we should watch them anyway!)
            {
                _player.IsFocused = true;
            }
            else if (hours < 30 &&
                    !IsEOD && !IsUnheard) // Low hours played on std account, could be a brand new cheater account
            {
                _player.IsFocused = true;
            }
            else if (sr >= 65f) // Very high survival rate
            {
                _player.IsFocused = true;
            }
            else if (overallKd >= 10f && sr < 35f) // Possible KD Dropping
            {
                _player.IsFocused = true;
            }
            else if (hours >= 1000 && sr < 25f) // Possible KD Dropping
            {
                _player.IsFocused = true;
            }
            else if (AchievLevel >= 2 && hours < 1000) // Very High achievement level but not enough hours to have legitimately earned them
            {
                _player.IsFocused = true;
            }
            else if (AchievLevel >= 1 && hours < 100) // High achievement level but not enough hours to have legitimately earned them
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
                Debug.WriteLine($"ERROR updating Member Category for '{Name}': {ex}");
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

        private ProfileData _data;
        public ProfileData Data
        {
            get => _data;
            set
            {
                if (_data == value) return;
                _data = value;
                RefreshProfile();
                if (App.Config.UI.MarkSusPlayers)
                {
                    FocusIfSus();
                }
                OnPropertyChanged(nameof(Data));
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
                {
                    _ = Task.Run(() => RunTwitchLookupAsync(value));
                }
                OnPropertyChanged(nameof(Name));
            }
        }

        private PlayerType _type;
        public PlayerType Type
        {
            get => _type;
            set
            {
                if (_type == value) return;
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        private string _accountID;
        public string AccountID
        {
            get => _accountID;
            private set
            {
                if (_accountID == value) return;
                _accountID = value;
                OnPropertyChanged(nameof(AccountID));
            }
        }

        private int _groupID = -1;
        public int GroupID
        {
            get => _groupID;
            set
            {
                if (_groupID == value) return;
                _groupID = value;
                OnPropertyChanged(nameof(GroupID));
            }
        }

        private Enums.EPlayerSide _playerSide;
        public Enums.EPlayerSide PlayerSide
        {
            get => _playerSide;
            set
            {
                if (_playerSide == value) return;
                _playerSide = value;
                OnPropertyChanged(nameof(PlayerSide));
            }
        }

        private string _alerts;
        public string Alerts
        {
            get => _alerts;
            set
            {
                if (_alerts == value) return;
                _alerts = value;
                OnPropertyChanged(nameof(Alerts));
            }
        }

        private string _twitchChannelURL;
        public string TwitchChannelURL
        {
            get => _twitchChannelURL;
            set
            {
                if (_twitchChannelURL == value) return;
                _twitchChannelURL = value;
                OnPropertyChanged(nameof(TwitchChannelURL));
            }
        }

        private float? _overallKD;
        public float? Overall_KD
        {
            get => _overallKD;
            private set
            {
                if (_overallKD == value) return;
                _overallKD = value;
                OnPropertyChanged(nameof(Overall_KD));
            }
        }

        private int? _raidCount;
        public int? RaidCount
        {
            get => _raidCount;
            private set
            {
                if (_raidCount == value) return;
                _raidCount = value;
                OnPropertyChanged(nameof(RaidCount));
            }
        }

        // SurvivedCount is internal—no public getter—but we need its backing field & setter
        private int? _survivedCount;
        private int? SurvivedCount
        {
            get => _survivedCount;
            set => _survivedCount = value;
        }

        private float? _survivedRate;
        public float? SurvivedRate
        {
            get => _survivedRate;
            private set
            {
                if (_survivedRate == value) return;
                _survivedRate = value;
                OnPropertyChanged(nameof(SurvivedRate));
            }
        }

        private int? _hours;
        public int? Hours
        {
            get => _hours;
            private set
            {
                if (_hours == value) return;
                _hours = value;
                OnPropertyChanged(nameof(Hours));
            }
        }

        private int? _level;
        public int? Level
        {
            get => _level;
            private set
            {
                if (_level == value) return;
                _level = value;
                OnPropertyChanged(nameof(Level));
            }
        }

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
                    OnPropertyChanged(nameof(MemberCategory));
                }
            }
        }

        private string _acct = "--";
        public string Acct
        {
            get => _acct;
            private set
            {
                if (_acct == value) return;
                _acct = value;
                OnPropertyChanged(nameof(Acct));
            }
        }

        private int _achievLevel = 0;
        public int AchievLevel
        {
            get => _achievLevel;
            set
            {
                if (_achievLevel == value) return;
                _achievLevel = value;
                OnPropertyChanged(nameof(AchievLevel));
            }
        }

        #endregion
    }
}
