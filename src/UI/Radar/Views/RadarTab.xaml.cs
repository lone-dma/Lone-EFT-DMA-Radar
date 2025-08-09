using eft_dma_radar.UI.Radar.ViewModels;
using SkiaSharp.Views.WPF;
using System.Windows.Controls;

namespace eft_dma_radar.UI.Radar.Views
{
    /// <summary>
    /// Interaction logic for RadarTab.xaml
    /// </summary>
    public sealed partial class RadarTab : UserControl
    {
        public RadarViewModel ViewModel { get; }

        public RadarTab()
        {
            InitializeComponent();
            DataContext = ViewModel = new RadarViewModel(this);
        }
    }
}
