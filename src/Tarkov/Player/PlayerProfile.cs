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

using EftDmaRadarLite.Tarkov.Data;
using EftDmaRadarLite.Tarkov.Data.ProfileApi;
using EftDmaRadarLite.Tarkov.Data.ProfileApi.Schema;
using EftDmaRadarLite.Twitch;

namespace EftDmaRadarLite.Tarkov.Player
{
    public sealed class PlayerProfile : INotifyPropertyChanged
    {
        private readonly ObservedPlayer _player;
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public PlayerProfile(ObservedPlayer player, string accountId)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            AccountID = accountId ?? throw new ArgumentNullException(nameof(accountId));
            if (player.IsHuman)
            {
                if (App.Config.Cache.ProfileService.TryGetValue(player.AccountID, out var cachedProfile))
                {
                    Data = cachedProfile.Data;
                }
                else
                {
                    EFTProfileService.RegisterProfile(this);
                }
            }
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
                Level = StaticGameData.XPTable
                    .Where(x => x.Key > xp.Value)
                    .Select(x => x.Value)
                    .FirstOrDefault() - 1;

            // --- Member Category ---
            var info = Data?.Info;
            if (info is not null)
                MemberCategory = (Enums.EMemberCategory)info.MemberCategory;

            // --- Account Type ("UH", "EOD", or "--") ---
            var mc = MemberCategory ?? Enums.EMemberCategory.Default;
            if ((mc & Enums.EMemberCategory.Unheard) == Enums.EMemberCategory.Unheard)
                Acct = "UH";
            else if ((mc & Enums.EMemberCategory.UniqueId) == Enums.EMemberCategory.UniqueId)
                Acct = "EOD";
            else
                Acct = "--";
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

        #endregion
    }
}
