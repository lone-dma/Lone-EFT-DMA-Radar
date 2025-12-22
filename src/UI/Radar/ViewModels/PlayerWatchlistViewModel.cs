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

using LoneEftDmaRadar.UI.Data;
using LoneEftDmaRadar.UI.Radar.Views;
using System.Collections.ObjectModel;

namespace LoneEftDmaRadar.UI.Radar.ViewModels
{
    public sealed class PlayerWatchlistViewModel : INotifyPropertyChanged
    {
        private readonly PlayerWatchlistTab _parent;
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        private readonly ConcurrentDictionary<string, PlayerWatchlistEntry> _watchlist = new(App.Config.PlayerWatchlist
            .GroupBy(p => p.AcctID, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                k => k.Key, v => v.First(),
                StringComparer.OrdinalIgnoreCase));
        /// <summary>
        /// Thread Safe Watchlist for Lookups.
        /// </summary>
        public IReadOnlyDictionary<string, PlayerWatchlistEntry> Watchlist => _watchlist;
        /// <summary>
        /// Entries for the Player Watchlist (Data Binding Only).
        /// </summary>
        public ObservableCollection<PlayerWatchlistEntry> Entries => App.Config.PlayerWatchlist;

        public PlayerWatchlistViewModel(PlayerWatchlistTab parent)
        {
            _parent = parent;
            Entries.CollectionChanged += Entries_CollectionChanged;
        }

        private void Entries_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add &&
                e.NewItems is not null)
            {
                foreach (PlayerWatchlistEntry entry in e.NewItems)
                {
                    _watchlist.TryAdd(entry.AcctID, entry);
                }
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove &&
                e.OldItems is not null)
            {
                foreach (PlayerWatchlistEntry entry in e.OldItems)
                {
                    _watchlist.TryRemove(entry.AcctID, out _);
                }
            }
        }

        private PlayerWatchlistEntry _selectedEntry;
        public PlayerWatchlistEntry SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (_selectedEntry != value)
                {
                    _selectedEntry = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Static helper.
        /// </summary>
        /// <param name="entry"></param>
        public static void Add(PlayerWatchlistEntry entry)
        {
            if (MainWindow.Instance?.PlayerWatchlist is PlayerWatchlistTab playerWatchlist)
            {
                playerWatchlist.Dispatcher.Invoke(() =>
                {
                    // Add the entry to the watchlist
                    if (playerWatchlist.ViewModel?.Entries is not null)
                    {
                        var existing = playerWatchlist.ViewModel.Entries.FirstOrDefault(x => string.Equals(x.AcctID, entry.AcctID, StringComparison.OrdinalIgnoreCase));
                        if (existing is not null)
                        {
                            existing.Reason = $"{entry.Reason} | {existing.Reason}";
                        }
                        else
                        {
                            playerWatchlist.ViewModel.Entries.Add(entry);
                        }
                    }
                });
            }
        }
    }
}
