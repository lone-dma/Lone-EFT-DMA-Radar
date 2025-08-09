using eft_dma_radar.UI.Radar.ViewModels;
using System.Windows.Controls;

namespace eft_dma_radar.UI.Radar.Views
{
    /// <summary>
    /// Interaction logic for RadarOverlay.xaml
    /// </summary>
    public partial class RadarOverlay : UserControl
    {
        public RadarOverlayViewModel ViewModel { get; }
        public RadarOverlay()
        {
            InitializeComponent();
            DataContext = ViewModel = new RadarOverlayViewModel();
        }
    }
}
