using MessagePack;

namespace eft_dma_radar.WebRadar.Data
{
    [MessagePackObject]
    public sealed class WebRadarUpdate
    {
        /// <summary>
        /// Update version (used for ordering).
        /// </summary>
        [Key(0)]
        public ulong Version { get; set; } = 0;
        /// <summary>
        /// True if In-Game, otherwise False.
        /// </summary>
        [Key(1)]
        public bool InGame { get; set; } = false;
        /// <summary>
        /// Contains the Map ID of the current map.
        /// </summary>
        [Key(2)]
        public string MapID { get; set; } = null;
        /// <summary>
        /// All Players currently on the map.
        /// </summary>
        [Key(3)]
        public IEnumerable<WebRadarPlayer> Players { get; set; } = null;
    }
}
