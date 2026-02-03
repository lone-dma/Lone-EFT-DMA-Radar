using eft_dma_radar.UI.Radar.ViewModels;
using System.Windows.Controls;

namespace eft_dma_radar.UI.Radar.Views
{
    /// <summary>
    /// Interaction logic for PlayerWatchlistTab.xaml
    /// </summary>
    public partial class PlayerWatchlistTab : UserControl
    {
        public PlayerWatchlistViewModel ViewModel { get; }
        public PlayerWatchlistTab()
        {
            InitializeComponent();
            DataContext = ViewModel = new PlayerWatchlistViewModel(this);
        }
    }
}
