using EftDmaRadarLite.UI.Loot;
using EftDmaRadarLite.DMA;
using EftDmaRadarLite.UI.ColorPicker;
using EftDmaRadarLite.UI.Data;
using System.Collections.ObjectModel;
using EftDmaRadarLite.Tarkov.Data.ProfileApi;
using EftDmaRadarLite.Twitch;
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
        public ObservableCollection<PlayerWatchlistEntry> PlayerWatchlist { get; private set; } = new();

        /// <summary>
        /// Loot Filters Config.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("lootFilters")]
        public LootFilterConfig LootFilters { get; private set; } = new();

        /// <summary>
        /// Contains cache data between program sessions.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("cache")]
        public PersistentCache Cache { get; private set; } = new();

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
                return JsonSerializer.Deserialize<EftDmaConfig>(json);
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
            // 1) Write JSON to .tmp with WriteThrough so data hits disk
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = false });
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

            // 2) Atomic replace: .tmp → config, backing up old config to .bak
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

            // 3) Clean up stale backup if you want
            //if (_backupFile.Exists)
            //    _backupFile.Delete();
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
        [JsonPropertyName("priority")]
        public uint Priority { get; set; } = uint.MaxValue;
        /// <summary>
        /// True if this provider is enabled, otherwise False.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// Number of requests per minute to this provider.
        /// </summary>
        [JsonPropertyName("requestsPerMinute")]
        public int RequestsPerMinute { get; set; } = 40;
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

    /// <summary>
    /// Caches runtime data between sessions.
    /// </summary>
    public sealed class PersistentCache
    {
        [JsonPropertyName("profileService")]
        [JsonInclude]
        [JsonConverter(typeof(CaseInsensitiveConcurrentDictionaryConverter<CachedProfileData>))]

        public ConcurrentDictionary<string, CachedProfileData> ProfileService { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        [JsonPropertyName("twitchService")]
        [JsonInclude]
        [JsonConverter(typeof(CaseInsensitiveConcurrentDictionaryConverter<CachedTwitchEntry>))]
        public ConcurrentDictionary<string, CachedTwitchEntry> TwitchService { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}