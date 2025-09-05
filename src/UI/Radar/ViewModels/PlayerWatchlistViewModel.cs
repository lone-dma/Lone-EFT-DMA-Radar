using System.Collections.ObjectModel;
using EftDmaRadarLite.UI.Data;
using EftDmaRadarLite.UI.Radar.Views;

namespace EftDmaRadarLite.UI.Radar.ViewModels
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
                    playerWatchlist.ViewModel?.Entries.Add(entry);
                });
            }
        }
    }
}
