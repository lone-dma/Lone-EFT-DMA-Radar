namespace LoneEftDmaRadar.Web.WebRadar.Data
{
    public sealed class WebRadarUpdate
    {
        /// <summary>
        /// Update version (used for ordering).
        /// </summary>
        [JsonPropertyName("version")]
        public ulong Version { get; set; } = 0;
        /// <summary>
        /// True if In-Game, otherwise False.
        /// </summary>
        [JsonPropertyName("inGame")]
        public bool InGame { get; set; } = false;
        /// <summary>
        /// Contains the Map ID of the current map.
        /// </summary>
        [JsonPropertyName("mapId")]
        public string MapID { get; set; } = null;
        /// <summary>
        /// All Players currently on the map.
        /// </summary>
        [JsonPropertyName("players")]
        public IEnumerable<WebRadarPlayer> Players { get; set; } = null;
    }
}
