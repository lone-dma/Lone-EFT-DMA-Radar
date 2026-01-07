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

using LoneEftDmaRadar.Misc.JSON;
using LoneEftDmaRadar.Tarkov.World.Player;
using LoneEftDmaRadar.UI;
using LoneEftDmaRadar.UI.ColorPicker;
using LoneEftDmaRadar.UI.Loot;
using VmmSharpEx.Extensions.Input;

namespace LoneEftDmaRadar
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
        public DMAConfig DMA { get; set; } = new();

        /// <summary>
        /// UI/Radar Config
        /// </summary>
        [JsonPropertyName("ui")]
        public UIConfig UI { get; set; } = new();

        /// <summary>
        /// Misc Config
        /// </summary>
        [JsonPropertyName("misc")]
        public MiscConfig Misc { get; set; } = new();

        /// <summary>
        /// Web Radar Config
        /// </summary>
        [JsonPropertyName("webRadar")]
        public WebRadarConfig WebRadar { get; set; } = new();

        /// <summary>
        /// FilteredLoot Config
        /// </summary>
        [JsonPropertyName("loot")]
        public LootConfig Loot { get; set; } = new LootConfig();

        /// <summary>
        /// Containers configuration.
        /// </summary>
        [JsonPropertyName("containers")]
        public ContainersConfig Containers { get; set; } = new();

        /// <summary>
        /// Hotkeys Dictionary for Radar.
        /// </summary>
        [JsonPropertyName("hotkeys_v2")]
        public ConcurrentDictionary<Win32VirtualKey, string> Hotkeys { get; set; } = new(); // Default entries

        /// <summary>
        /// All defined Radar Colors.
        /// </summary>
        [JsonPropertyName("radarColors")]
        [JsonConverter(typeof(ColorDictionaryConverter))]
        public ConcurrentDictionary<ColorPickerOption, string> RadarColors { get; set; } = new();

        /// <summary>
        /// Widgets Configuration.
        /// </summary>
        [JsonPropertyName("aimviewWidget")]
        public AimviewWidgetConfig AimviewWidget { get; set; } = new();

        /// <summary>
        /// Widgets Configuration.
        /// </summary>
        [JsonPropertyName("infoWidget")]
        public InfoWidgetConfig InfoWidget { get; set; } = new();

        /// <summary>
        /// Quest Helper Cfg
        /// </summary>
        [JsonPropertyName("questHelper")]
        public QuestHelperConfig QuestHelper { get; set; } = new();

        /// <summary>
        /// FilteredLoot Filters Config.
        /// </summary>
        [JsonPropertyName("lootFilters")]
        public LootFilterConfig LootFilters { get; set; } = new();

        /// <summary>
        /// Persistent Cache Access.
        /// </summary>
        [JsonPropertyName("cache")]
        public PersistentCache Cache { get; set; } = new();

        #region Config Interface

        /// <summary>
        /// Filename of this Config File (not full path).
        /// </summary>
        [JsonIgnore]
        internal const string Filename = "Config-EFT.json";

        [JsonIgnore]
        private static readonly Lock _syncRoot = new();

        [JsonIgnore]
        private static readonly FileInfo _configFile = new(Path.Combine(Program.ConfigPath.FullName, Filename));

        [JsonIgnore]
        private static readonly FileInfo _tempFile = new(Path.Combine(Program.ConfigPath.FullName, Filename + ".tmp"));

        [JsonIgnore]
        private static readonly FileInfo _backupFile = new(Path.Combine(Program.ConfigPath.FullName, Filename + ".bak"));

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
                Program.ConfigPath.Create();
                if (_configFile.Exists)
                {
                    config = TryLoad(_tempFile) ??
                        TryLoad(_configFile) ??
                        TryLoad(_backupFile);

                    if (config is null)
                    {
                        var dlg = MessageBox.Show(
                            RadarWindow.Handle,
                            "Config File Corruption Detected! If you backed up your config, you may attempt to restore it.\n" +
                            "Press OK to Reset Config and continue startup, or CANCEL to terminate program.",
                            Program.Name,
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
                return JsonSerializer.Deserialize(json, AppJsonContext.Default.EftDmaConfig);
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
            var json = JsonSerializer.Serialize(config, AppJsonContext.Default.EftDmaConfig);
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
        /// Set FPS for the Radar Window (default: 60)
        /// </summary>
        [JsonPropertyName("fps")]
        public int FPS { get; set; } = 60;

        /// <summary>
        /// UI Scale Value (0.5-2.0 , default: 1.0)
        /// </summary>
        [JsonPropertyName("scale")]
        public float UIScale { get; set; } = 1.0f;

        /// <summary>
        /// Size of the Radar Window.
        /// </summary>
        [JsonPropertyName("windowSize")]
        public SKSize WindowSize { get; set; } = new(1280, 720);

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
        /// Show Hazards (mines,snipers,etc.) in the Radar UI.
        /// </summary>
        [JsonPropertyName("showHazards")]
        public bool ShowHazards { get; set; } = true;

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

        /// <summary>
        /// Show exfils on radar.
        /// </summary>
        [JsonPropertyName("showExfils")]
        public bool ShowExfils { get; set; } = true;
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
        /// Show FilteredLoot by "Price per Slot".
        /// </summary>
        [JsonPropertyName("pricePerSlot")]
        public bool PricePerSlot { get; set; }

        /// <summary>
        /// FilteredLoot Price Mode.
        /// </summary>
        [JsonPropertyName("priceMode")]
        public LootPriceMode PriceMode { get; set; } = LootPriceMode.FleaMarket;

        /// <summary>
        /// Show loot on the player's wishlist (manual only).
        /// </summary>
        [JsonPropertyName("showWishlist")]
        public bool ShowWishlist { get; set; } = false;

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
        public bool SelectAll { get; set; } = false;

        /// <summary>
        /// Selected containers to display.
        /// </summary>
        [JsonPropertyName("selected_v4")]
        public ConcurrentDictionary<string, byte> Selected { get; set; } = new();
    }

    /// <summary>
    /// FilteredLoot Filter Config.
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
        [JsonPropertyName("filters")]
        public ConcurrentDictionary<string, UserLootFilter> Filters { get; set; } = new() // Key is just a name, doesnt need to be case insensitive
        {
            ["default"] = new()
        };
    }

    public sealed class AimviewWidgetConfig
    {
        /// <summary>
        /// True if the Aimview Widget is enabled.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
    }


    public sealed class InfoWidgetConfig
    {
        /// <summary>
        /// True if the Info Widget is enabled.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
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
        [JsonPropertyName("blacklistedQuests")]
        public ConcurrentDictionary<string, byte> BlacklistedQuests { get; set; } = new();
    }

    /// <summary>
    /// Persistent Cache that stores data between sessions for the same Process ID.
    /// </summary>
    public sealed class PersistentCache
    {
        /// <summary>
        /// Process Id this cache is tied to.
        /// </summary>
        [JsonPropertyName("pid")]
        public uint PID { get; set; }

        /// <summary>
        /// Cache information per Raid.
        /// Key: GameWorld Virtual Address | Value: RaidCache instance
        /// </summary>
        [JsonPropertyName("raidCache")]
        public ConcurrentDictionary<ulong, RaidCache> RaidCache { get; set; } = new();
    }

    /// <summary>
    /// Persistent Cache for a single Raid.
    /// </summary>
    public sealed class RaidCache
    {
        /// <summary>
        /// Defines players that are assigned special AI roles.
        /// Key: Player Id | Value: Special AI Role
        /// </summary>
        [JsonPropertyName("specialAi")]
        public ConcurrentDictionary<int, AIRole> SpecialAi { get; set; } = new();
        /// <summary>
        /// Defines player groups.
        /// Key: Player Id | Value: Group Id
        /// </summary>
        [JsonPropertyName("groups")]
        public ConcurrentDictionary<int, int> Groups { get; set; } = new();
        /// <summary>
        /// Defines players that have been 'focused' by the user (Right-Click on radar icon).
        /// Key: Player Id | Value: no-op
        /// </summary>
        [JsonPropertyName("focused")]
        public ConcurrentDictionary<int, byte> Focused { get; set; } = new();
    }

    /// <summary>
    /// Misc Config.
    /// </summary>
    public sealed class MiscConfig
    {
        /// <summary>
        /// Enables the 'Auto Groups' feature that attempts to automatically group players together (best-effort).
        /// </summary>
        [JsonPropertyName("autoGroups")]
        public bool AutoGroups { get; set; } = true;
    }
}