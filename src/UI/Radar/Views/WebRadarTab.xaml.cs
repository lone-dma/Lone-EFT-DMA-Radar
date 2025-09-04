using eft_dma_radar.UI.Radar.ViewModels;
using System.Windows.Controls;

namespace eft_dma_radar.UI.Radar.Views
{
    /// <summary>
    /// Interaction logic for WebRadarTab.xaml
    /// </summary>
    public partial class WebRadarTab : UserControl
    {
        public WebRadarViewModel ViewModel { get; }
        public WebRadarTab()
        {
            InitializeComponent();
            DataContext = ViewModel = new WebRadarViewModel();
        }
    }
}
