using EftDmaRadarLite.UI.Radar.ViewModels;
using System.Windows.Controls;

namespace EftDmaRadarLite.UI.Radar.Views
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
