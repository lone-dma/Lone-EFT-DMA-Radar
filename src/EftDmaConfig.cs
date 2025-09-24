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

using EftDmaRadarLite.UI.Loot;
using EftDmaRadarLite.DMA;
using EftDmaRadarLite.UI.ColorPicker;
using EftDmaRadarLite.UI.Data;
using System.Collections.ObjectModel;
using EftDmaRadarLite.Misc.JSON;
using EftDmaRadarLite.Misc;
using EftDmaRadarLite.Unity;

namespace EftDmaRadarLite
{
    /// <summary>
    /// Global Program Configuration (Config.json)
    /// </summary>
    public sealed class EftDmaConfig
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() 
        { 
            WriteIndented = true
        };
        /// <summary>
        /// Public Constructor required for deserialization.
        /// DO NOT CALL - USE LOAD().
        /// </summary>
        public EftDmaConfig() { }

        /// <summary>
        /// DMA Config
        /// </summary>
        [JsonPropertyName("dma")]
        [JsonInclude]
        public DMAConfig DMA { get; private set; } = new();

        /// <summary>
        /// Profile API Config.
        /// </summary>
        [JsonPropertyName("profileApi")]
        [JsonInclude]
        public ProfileApiConfig ProfileApi { get; private set; } = new();

        /// <summary>
        /// Twitch API Config (for streamer lookup).
        /// </summary>
        [JsonPropertyName("twitchApi")]
        [JsonInclude]
        public TwitchApiConfig TwitchApi { get; private set; } = new();

        /// <summary>
        /// UI/Radar Config
        /// </summary>
        [JsonPropertyName("ui")]
        [JsonInclude]
        public UIConfig UI { get; private set; } = new();

        /// <summary>
        /// Web Radar Config
        /// </summary>
        [JsonPropertyName("webRadar")]
        [JsonInclude]
        public WebRadarConfig WebRadar { get; private set; } = new();

        /// <summary>
        /// Loot Config
        /// </summary>
        [JsonPropertyName("loot")]
        [JsonInclude]
        public LootConfig Loot { get; private set; } = new LootConfig();

        /// <summary>
        /// Containers configuration.
        /// </summary>
        [JsonPropertyName("containers")]
        [JsonInclude]
        public ContainersConfig Containers { get; private set; } = new();

        /// <summary>
        /// Quest Helper Cfg
        /// </summary>
        [JsonPropertyName("questHelperCfg")]
        [JsonInclude]
        public QuestHelperConfig QuestHelper { get; private set; } = new();

        /// <summary>
        /// Hotkeys Dictionary for Radar.
        /// </summary>
        [JsonPropertyName("hotkeys")]
        [JsonInclude]
        public ConcurrentDictionary<UnityKeyCode, string> Hotkeys { get; private set; } = new(); // Default entries

        /// <summary>
        /// All defined Radar Colors.
        /// </summary>
        [JsonPropertyName("radarColors")]
        [JsonConverter(typeof(ColorDictionaryConverter))]
        [JsonInclude]
        public ConcurrentDictionary<ColorPickerOption, string> RadarColors { get; private set; } = new();

        /// <summary>
        /// Widgets Configuration.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("espWidget")]
        public EspWidgetConfig EspWidget { get; private set; } = new();

        /// <summary>
        /// Widgets Configuration.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("infoWidget")]
        public InfoWidgetConfig InfoWidget { get; private set; } = new();

        /// <summary>
        /// Player Watchlist Collection.
        /// ** ONLY USE FOR BINDING **
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("playerWatchlist")]
        public ObservableCollection<PlayerWatchlistEntry> PlayerWatchlist { get; private set; } = new()
        {
            new PlayerWatchlistEntry { AcctID = "2403694", Reason = "twitch/donpscelli_", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "152977", Reason = "twitch/HONEYxxo", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "835112", Reason = "twitch/lvndmark", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "376689", Reason = "twitch/summit1g", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "11387881", Reason = "twitch/tigz", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1049972", Reason = "twitch/hutchmf", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2989797", Reason = "twitch/viibiin", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "354136", Reason = "twitch/klean", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1717857", Reason = "twitch/jessekazam", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "3391828", Reason = "twitch/nyxia", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2438239", Reason = "twitch/xblazed", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "831617", Reason = "twitch/velion", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "3526004", Reason = "twitch/gingy", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "4637816", Reason = "twitch/trey24k", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1663669", Reason = "twitch/desmondpilak", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "763945", Reason = "twitch/aquafps", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2111653", Reason = "twitch/bakeezy", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2095752", Reason = "twitch/blueberrygabi", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2165308", Reason = "twitch/smittystone", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1153634", Reason = "twitch/2thy", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "3982736", Reason = "twitch/gl40labsrat", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "971133", Reason = "twitch/rengawr", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "4897027", Reason = "twitch/annemunition", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "9347828", Reason = "twitch/honeyxxo", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2058310", Reason = "twitch/moman", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "3424723", Reason = "twitch/binoia", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "5006226", Reason = "twitch/cooldee__", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "4609337", Reason = "twitch/ponch", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "927745", Reason = "twitch/goes", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "4764608", Reason = "twitch/tobytwofaced", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2043138", Reason = "twitch/kkersanovtv", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "8484894", Reason = "twitch/nogenerals", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1294950", Reason = "twitch/wildez", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1942597", Reason = "twitch/cwis0r", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2334119", Reason = "twitch/jaybaybay", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "6541088", Reason = "twitch/shoes", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "654070", Reason = "twitch/cryodrollic", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2250762", Reason = "twitch/mismagpie", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "3351793", Reason = "twitch/nohelmetchad", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "5158172", Reason = "twitch/undeadessence", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "9351502", Reason = "twitch/burgaofps", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "4168016", Reason = "twitch/endra", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "739353", Reason = "twitch/knueppelpaste", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1312997", Reason = "twitch/vonza", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2739217", Reason = "twitch/volayethor", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "3400742", Reason = "twitch/fudgexl", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2763053", Reason = "twitch/mzdunk", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2329796", Reason = "twitch/philbo", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1758499", Reason = "twitch/someman", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "859833", Reason = "twitch/baxbeast", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "766970", Reason = "twitch/genooo", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2773520", Reason = "twitch/skidohunter", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2554678", Reason = "twitch/rileyarmageddon", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "3998491", Reason = "twitch/kongstyle101", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "3569522", Reason = "twitch/realkraftyy", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "5550265", Reason = "twitch/tomrander", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2991546", Reason = "twitch/smol", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2673247", Reason = "twitch/shotsofvaca_", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1632126", Reason = "twitch/wenotrat", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2755056", Reason = "twitch/valarman", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "4825441", Reason = "twitch/doubledstroyer", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "5311265", Reason = "twitch/vazquez66", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "10799845", Reason = "twitch/ashnue", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "7225268", Reason = "twitch/crylixblooom", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1712951", Reason = "twitch/mvze_", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "4194405", Reason = "twitch/shwiftyfps", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "8336334", Reason = "twitch/swirrrly", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "885958", Reason = "twitch/switch360tv", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "5711540", Reason = "twitch/jewlee", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "6567825", Reason = "twitch/strongeo", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "926010", Reason = "twitch/toastracktv", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "851122", Reason = "twitch/cocaoo_", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "4034904", Reason = "twitch/verybadscav", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2277116", Reason = "twitch/imbobby__", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "3042051", Reason = "twitch/wardell", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2031346", Reason = "twitch/maza4kst", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "39632", Reason = "twitch/jimpanse", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "10480940", Reason = "twitch/chi_chaan", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "3515629", Reason = "twitch/daskicosin", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2207216", Reason = "twitch/logicalsolutions", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2971732", Reason = "twitch/myst1s", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2592389", Reason = "twitch/pixel8_ttv", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1827749", Reason = "twitch/applebr1nger", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "6170674", Reason = "twitch/wo1f_gg", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "3330252", Reason = "twitch/blinge1", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "4544185", Reason = "twitch/impatiya", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "5602537", Reason = "twitch/schmidttyb", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1126512", Reason = "twitch/torkie", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1526877", Reason = "twitch/trentau", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "3581557", Reason = "twitch/tqmo__", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "7706088", Reason = "twitch/gilltex", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1002256", Reason = "twitch/wondows", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "7674224", Reason = "twitch/cujoman", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1161451", Reason = "twitch/gerysenior", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "922156", Reason = "twitch/hadess31", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "11468155", Reason = "twitch/butecodofranco", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "11013668", Reason = "twitch/joeliain2310", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "11118058", Reason = "twitch/moonshinefps", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "3118179", Reason = "twitch/soultura86", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "8115752", Reason = "twitch/renalakec", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "7085963", Reason = "twitch/notoriouspdx", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "3047477", Reason = "twitch/strngerping", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "10959843", Reason = "twitch/ry784", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "5646257", Reason = "twitch/mushamaru_", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "3539914", Reason = "twitch/rguardian", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "5463289", Reason = "twitch/wabrat", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "839191", Reason = "twitch/notechniquetv", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "7104272", Reason = "twitch/fiathegemini", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "9827614", Reason = "twitch/codex011", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "5051655", Reason = "twitch/dkaye23", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "8788838", Reason = "twitch/mrbubblyttv", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2799174", Reason = "twitch/sweetyboom", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "5308968", Reason = "twitch/oggyshoggy", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "427222", Reason = "twitch/steeyo", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1481309", Reason = "twitch/anton", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "364768", Reason = "twitch/hayz", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "4411189", Reason = "twitch/hayz (hc)", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "5353635", Reason = "twitch/stankrat", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2614961", Reason = "twitch/oberst0m", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "6815534", Reason = "twitch/thatfriendlyguy", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "3441806", Reason = "twitch/JohnBBeta", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2238335", Reason = "twitch/zchum", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "8016990", Reason = "twitch/mistofhazmat", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "858816", Reason = "twitch/hipperpyah", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "380648", Reason = "twitch/sektenspinner", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "408825", Reason = "twitch/bubbinger", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2215415", Reason = "twitch/raggelton", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2693789", Reason = "twitch/zcritic", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "9283718", Reason = "twitch/triple_g", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "546813", Reason = "twitch/pepp", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "4432653", Reason = "twitch/hexloom", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "9826933", Reason = "twitch/satsuki_hotaru", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2699481", Reason = "twitch/headleyy", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2366827", Reason = "twitch/thomaspaste", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1699605", Reason = "twitch/taxfree_", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "5378845", Reason = "twitch/thePridgeTTV", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "564115", Reason = "twitch/ghostfreak66", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "5817655", Reason = "twitch/engineergod", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "479729", Reason = "twitch/WaitImCheating", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "860017", Reason = "twitch/Baddie", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "137994", Reason = "twitch/BaudT", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "11351038", Reason = "twitch/thruststv", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "8381705", Reason = "twitch/cubFPS", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2997948", Reason = "twitch/sheefgg", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "4011779", Reason = "twitch/bearki", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2011844", Reason = "twitch/jonk", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "5975690", Reason = "twitch/smojii", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "165994", Reason = "twitch/willerz", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "641616", Reason = "twitch/pestily", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1090448", Reason = "youtube/@DrLupo", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1448970", Reason = "twitch/glorious_e", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1080203", Reason = "twitch/hyperrattv", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1425172", Reason = "twitch/axel_tv", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "3928278", Reason = "twitch/aims", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "740807", Reason = "twitch/b_komhate", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "923361", Reason = "twitch/swid", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "790774", Reason = "twitch/thepoolshark", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "417327", Reason = "twitch/wishyvt", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "10769222", Reason = "twitch/oimatewtf", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "8711935", Reason = "twitch/snok3z", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "11404544", Reason = "twitch/suddenly_toast", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "9052642", Reason = "twitch/mogu_vtuber", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "5225682", Reason = "twitch/beibei69", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "1284057", Reason = "twitch/dobbykillstreak", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "11024278", Reason = "twitch/yago0795", Timestamp = DateTime.Now },
            new PlayerWatchlistEntry { AcctID = "2536192", Reason = "youtube/@Airwingmarine", Timestamp = DateTime.Now },
        };

        /// <summary>
        /// Loot Filters Config.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("lootFilters")]
        public LootFilterConfig LootFilters { get; private set; } = new();

        #region Config Interface

        /// <summary>
        /// Filename of this Config File (not full path).
        /// </summary>
        [JsonIgnore] 
        internal const string Filename = "Config-EFT.json";

        [JsonIgnore] 
        private static readonly Lock _syncRoot = new();

        [JsonIgnore]
        private static readonly FileInfo _configFile = new(Path.Combine(App.ConfigPath.FullName, Filename));

        [JsonIgnore]
        private static readonly FileInfo _tempFile = new(Path.Combine(App.ConfigPath.FullName, Filename + ".tmp"));

        [JsonIgnore]
        private static readonly FileInfo _backupFile = new(Path.Combine(App.ConfigPath.FullName, Filename + ".bak"));

        /// <summary>
        /// Loads the configuration from disk.
        /// Creates a new config if it does not exist.
        /// ** ONLY CALL ONCE PER MUTEX **
        /// </summary>
        /// <returns>Loaded Config.</returns>
        public static EftDmaConfig Load()
        {
            EftDmaConfig config;
            lock (_syncRoot)
            {
                App.ConfigPath.Create();
                if (_configFile.Exists)
                {
                    config = TryLoad(_tempFile) ??
                        TryLoad(_configFile) ?? 
                        TryLoad(_backupFile);

                    if (config is null)
                    {
                        var dlg = MessageBox.Show(
                            "Config File Corruption Detected! If you backed up your config, you may attempt to restore it.\n" +
                            "Press OK to Reset Config and continue startup, or CANCEL to terminate program.",
                            App.Name,
                            MessageBoxButton.OKCancel,
                            MessageBoxImage.Error);
                        if (dlg == MessageBoxResult.Cancel)
                            Environment.Exit(0); // Terminate program
                        config = new EftDmaConfig();
                        SaveInternal(config);
                    }
                }
                else
                {
                    config = new();
                    SaveInternal(config);
                }

                return config;
            }
        }

        private static EftDmaConfig TryLoad(FileInfo file)
        {
            try
            {
                if (!file.Exists) 
                    return null;
                string json = File.ReadAllText(file.FullName);
                return JsonSerializer.Deserialize<EftDmaConfig>(json, _jsonOptions);
            }
            catch
            {
                return null; // Ignore errors, return null to indicate failure
            }
        }

        /// <summary>
        /// Save the current configuration to disk.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public void Save()
        {
            lock (_syncRoot)
            {
                try
                {
                    SaveInternal(this);
                }
                catch (Exception ex)
                {
                    throw new IOException($"ERROR Saving Config: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Saves the current configuration to disk asynchronously.
        /// </summary>
        /// <returns></returns>
        public async Task SaveAsync() => await Task.Run(Save);

        private static void SaveInternal(EftDmaConfig config)
        {
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            using (var fs = new FileStream(
                _tempFile.FullName,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                options: FileOptions.WriteThrough))
            using (var sw = new StreamWriter(fs))
            {
                sw.Write(json);
                sw.Flush();
                fs.Flush(flushToDisk: true);
            }
            if (_configFile.Exists)
            {
                File.Replace(
                    sourceFileName: _tempFile.FullName,
                    destinationFileName: _configFile.FullName,
                    destinationBackupFileName: _backupFile.FullName,
                    ignoreMetadataErrors: true);
            }
            else
            {
                File.Copy(
                    sourceFileName: _tempFile.FullName, 
                    destFileName: _backupFile.FullName);
                File.Move(
                    sourceFileName: _tempFile.FullName, 
                    destFileName: _configFile.FullName);
            }
        }

        #endregion
    }

    public sealed class DMAConfig
    {
        /// <summary>
        /// FPGA Read Algorithm
        /// </summary>
        [JsonPropertyName("fpgaAlgo")]
        public FpgaAlgo FpgaAlgo { get; set; } = FpgaAlgo.Auto;

        /// <summary>
        /// Use a Memory Map for FPGA DMA Connection.
        /// </summary>
        [JsonPropertyName("enableMemMap")]
        public bool MemMapEnabled { get; set; } = true;
    }

    public sealed class UIConfig
    {
        /// <summary>
        /// UI Scale Value (0.5-2.0 , default: 1.0)
        /// </summary>
        [JsonPropertyName("scale")]
        public float UIScale { get; set; } = 1.0f;

        /// <summary>
        /// Size of the Radar Window.
        /// </summary>
        [JsonPropertyName("windowSize")]
        public Size WindowSize { get; set; } = new(1280, 720);

        /// <summary>
        /// Window is maximized.
        /// </summary>
        [JsonPropertyName("windowMaximized")]
        public bool WindowMaximized { get; set; }

        /// <summary>
        /// Last used 'Zoom' level.
        /// </summary>
        [JsonPropertyName("zoom")]
        public int Zoom { get; set; } = 100;

        /// <summary>
        /// Player/Teammates Aimline Length (Max: 1500)
        /// </summary>
        [JsonPropertyName("aimLineLength")]
        public int AimLineLength { get; set; } = 1500;

        /// <summary>
        /// Show Mines/Claymores in the Radar UI.
        /// </summary>
        [JsonPropertyName("showMines")]
        public bool ShowMines { get; set; } = true;

        /// <summary>
        /// Hides player names & extended player info in Radar GUI.
        /// </summary>
        [JsonPropertyName("hideNames")]
        public bool HideNames { get; set; }

        /// <summary>
        /// Connects grouped players together via a semi-transparent line.
        /// </summary>
        [JsonPropertyName("connectGroups")]
        public bool ConnectGroups { get; set; } = true;

        /// <summary>
        /// Max game distance to render targets in Aimview,
        /// and to display dynamic aimlines between two players.
        /// </summary>
        [JsonPropertyName("maxDistance")]
        public float MaxDistance { get; set; } = 350;
        /// <summary>
        /// True if teammate aimlines should be the same length as LocalPlayer.
        /// </summary>
        [JsonPropertyName("teammateAimlines")]
        public bool TeammateAimlines { get; set; }

        /// <summary>
        /// True if AI Aimlines should dynamically extend.
        /// </summary>
        [JsonPropertyName("aiAimlines")]
        public bool AIAimlines { get; set; } = true;
    }

    public sealed class LootConfig
    {
        /// <summary>
        /// Shows loot on map.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Shows bodies/corpses on map.
        /// </summary>
        [JsonPropertyName("hideCorpses")]
        public bool HideCorpses { get; set; }

        /// <summary>
        /// Minimum loot value (rubles) to display 'normal loot' on map.
        /// </summary>
        [JsonPropertyName("minValue")]
        public int MinValue { get; set; } = 50000;

        /// <summary>
        /// Minimum loot value (rubles) to display 'important loot' on map.
        /// </summary>
        [JsonPropertyName("minValueValuable")]
        public int MinValueValuable { get; set; } = 200000;

        /// <summary>
        /// Show Loot by "Price per Slot".
        /// </summary>
        [JsonPropertyName("pricePerSlot")]
        public bool PricePerSlot { get; set; }

        /// <summary>
        /// Loot Price Mode.
        /// </summary>
        [JsonPropertyName("priceMode")]
        public LootPriceMode PriceMode { get; set; } = LootPriceMode.FleaMarket;

        /// <summary>
        /// Show loot on the player's wishlist (manual only).
        /// </summary>
        [JsonPropertyName("showWishlist")]
        public bool ShowWishlist { get; set; }

    }

    public sealed class QuestHelperConfig
    {
        /// <summary>
        /// Enables Quest Helper
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Quests that are overridden/disabled.
        /// </summary>
        [JsonPropertyName("blacklistedQuests_v3")]
        [JsonInclude]
        [JsonConverter(typeof(CaseInsensitiveConcurrentHashSetConverter))]
        public ConcurrentHashSet<string> BlacklistedQuests { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class ContainersConfig
    {
        /// <summary>
        /// Shows static containers on map.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Maximum distance to draw static containers.
        /// </summary>
        [JsonPropertyName("drawDistance")]
        public float DrawDistance { get; set; } = 100f;

        /// <summary>
        /// Select all containers.
        /// </summary>
        [JsonPropertyName("selectAll")]
        public bool SelectAll { get; set; } = true;

        /// <summary>
        /// Hide containers searched by LocalPlayer.
        /// </summary>
        [JsonPropertyName("hideSearched")]
        public bool HideSearched { get; set; } = false;

        /// <summary>
        /// Selected containers to display.
        /// </summary>
        [JsonPropertyName("selected_v3")]
        [JsonInclude]
        [JsonConverter(typeof(CaseInsensitiveConcurrentHashSetConverter))]
        public ConcurrentHashSet<string> Selected { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Loot Filter Config.
    /// </summary>
    public sealed class LootFilterConfig
    {
        /// <summary>
        /// Currently selected filter.
        /// </summary>
        [JsonPropertyName("selected")]
        public string Selected { get; set; } = "default";
        /// <summary>
        /// Filter Entries.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("filters")]
        public ConcurrentDictionary<string, UserLootFilter> Filters { get; private set; } = new() // Key is just a name, doesnt need to be case insensitive
        {
            ["default"] = new()
        };
    }

    public sealed class EspWidgetConfig
    {
        /// <summary>
        /// True if the ESP Widget is enabled.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// True if the ESP Widget is minimized.
        /// </summary>
        [JsonPropertyName("minimized")] 
        public bool Minimized { get; set; } = false;

        /// <summary>
        /// Aimview Location
        /// </summary>
        [JsonPropertyName("location")]
        [JsonConverter(typeof(SKRectJsonConverter))]
        public SKRect Location { get; set; }

        /// <summary>
        /// Zoom factor for ESP Widget.
        /// </summary>
        [JsonPropertyName("zoom")]
        public float Zoom { get; set; } = 1f;

        /// <summary>
        /// Game PC Monitor Resolution Width
        /// </summary>
        [JsonPropertyName("monitorWidth")]
        public int MonitorWidth { get; set; } = 1920;

        /// <summary>
        /// Game PC Monitor Resolution Height
        /// </summary>
        [JsonPropertyName("monitorHeight")]
        public int MonitorHeight { get; set; } = 1080;
    }

    public sealed class InfoWidgetConfig
    {
        /// <summary>
        /// True if the Info Widget is enabled.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// True if the Info Widget is minimized.
        /// </summary>
        [JsonPropertyName("minimized")]
        public bool Minimized { get; set; } = false;

        /// <summary>
        /// ESP Widget Location
        /// </summary>
        [JsonPropertyName("location")]
        [JsonConverter(typeof(SKRectJsonConverter))]
        public SKRect Location { get; set; }
    }

    public sealed class ProfileApiConfig
    {
        [JsonPropertyName("tarkovDev")]
        [JsonInclude]
        public TarkovDevConfig TarkovDev { get; private set; } = new();
        [JsonPropertyName("eftApiTech")]
        [JsonInclude]
        public EftApiTechConfig EftApiTech { get; private set; } = new();
    }

    public sealed class TwitchApiConfig
    {
        [JsonPropertyName("clientId")]
        public string ClientId { get; set; } = null;
        [JsonPropertyName("clientSecret")]
        public string ClientSecret { get; set; } = null;
    }

    public sealed class TarkovDevConfig
    {
        /// <summary>
        /// Priority of this provider.
        /// </summary>
        [JsonPropertyName("priority_v2")]
        public uint Priority { get; set; } = 1000;
        /// <summary>
        /// True if this provider is enabled, otherwise False.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
    }

    public sealed class EftApiTechConfig
    {
        /// <summary>
        /// Priority of this provider.
        /// </summary>
        [JsonPropertyName("priority")]
        public uint Priority { get; set; } = 10;
        /// <summary>
        /// True if this provider is enabled, otherwise False.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = false;
        /// <summary>
        /// Number of requests per minute to this provider.
        /// </summary>
        [JsonPropertyName("requestsPerMinute")]
        public int RequestsPerMinute { get; set; } = 5;
        /// <summary>
        /// API Key for eft-api.tech
        /// </summary>
        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; } = null;
    }

    /// <summary>
    /// Configuration for Web Radar.
    /// </summary>
    public sealed class WebRadarConfig
    {
        /// <summary>
        /// True if UPnP should be enabled.
        /// </summary>
        [JsonPropertyName("upnp")]
        public bool UPnP { get; set; } = true;
        /// <summary>
        /// IP to bind to.
        /// </summary>
        [JsonPropertyName("host")]
        public string IP { get; set; } = "0.0.0.0";
        /// <summary>
        /// TCP Port to bind to.
        /// </summary>
        [JsonPropertyName("port")]
        public string Port { get; set; } = Random.Shared.Next(50000, 60000).ToString();
        /// <summary>
        /// Server Tick Rate (Hz).
        /// </summary>
        [JsonPropertyName("tickRate")]
        public string TickRate { get; set; } = "60";
    }
}