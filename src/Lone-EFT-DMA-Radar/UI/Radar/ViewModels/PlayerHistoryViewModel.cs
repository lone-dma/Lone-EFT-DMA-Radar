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

using LoneEftDmaRadar.Tarkov.GameWorld.Player;
using LoneEftDmaRadar.UI.Data;
using LoneEftDmaRadar.UI.Misc;
using LoneEftDmaRadar.UI.Radar.Views;
using System.Collections.ObjectModel;

namespace LoneEftDmaRadar.UI.Radar.ViewModels
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
