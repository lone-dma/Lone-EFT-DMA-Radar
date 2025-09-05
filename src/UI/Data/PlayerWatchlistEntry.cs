namespace EftDmaRadarLite.UI.Data
{
    /// <summary>
    /// JSON Wrapper for Player Watchlist.
    /// </summary>
    public sealed class PlayerWatchlistEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private string _acctId = string.Empty;
        /// <summary>
        /// Player's Account ID as obtained from Player History.
        /// </summary>
        [JsonPropertyName("acctID")]
        public string AcctID
        {
            get => _acctId;
            set
            {
                if (_acctId != value)
                {
                    _acctId = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _reason = string.Empty;
        /// <summary>
        /// Reason for adding player to Watchlist (ex: Cheater, streamer name,etc.)
        /// </summary>
        [JsonPropertyName("reason")]
        public string Reason
        {
            get => _reason;
            set
            {
                if (_reason != value)
                {
                    _reason = value;
                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// Timestamp when the entry was originally added.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; init; } = DateTime.Now;
    }
}
