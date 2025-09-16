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
