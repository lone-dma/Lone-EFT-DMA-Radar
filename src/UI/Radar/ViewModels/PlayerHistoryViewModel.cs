using EftDmaRadarLite.Tarkov.Player;
using EftDmaRadarLite.UI.Data;
using EftDmaRadarLite.UI.Misc;
using EftDmaRadarLite.UI.Radar.Views;
using System.Collections.ObjectModel;

namespace EftDmaRadarLite.UI.Radar.ViewModels
{
    public sealed class PlayerHistoryViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Bound to the DataGrid's ItemsSource.
        /// </summary>
        public ObservableCollection<ObservedPlayer> Entries { get; } = new();

        private ObservedPlayer _selectedEntry;
        public ObservedPlayer SelectedEntry
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

        public void HandleDoubleClick()
        {
            if (SelectedEntry is ObservedPlayer entry)
            {
                var dialog = new InputBoxWindow($"Player '{entry.Name}'", "Enter watchlist reason below:");
                dialog.ShowDialog();
                if (dialog.DialogResult == true && dialog.InputText is string reason)
                {
                    var watchlistEntry = new PlayerWatchlistEntry
                    {
                        AcctID = entry.AccountID.Trim(),
                        Reason = reason
                    };
                    PlayerWatchlistViewModel.Add(watchlistEntry);
                    entry.UpdateAlerts(reason);
                }
            }
        }

        /// <summary>
        /// Static Helper Method
        /// </summary>
        /// <param name="player"></param>
        public static void Add(ObservedPlayer player)
        {
            if (MainWindow.Instance?.PlayerHistory is PlayerHistoryTab playerHistory)
            {
                playerHistory.Dispatcher.Invoke(() =>
                {
                    playerHistory.ViewModel?.Entries.Insert(0, player);
                });
            }
        }
    }
}
