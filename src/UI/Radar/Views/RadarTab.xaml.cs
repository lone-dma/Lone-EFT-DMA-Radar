using EftDmaRadarLite.UI.Radar.ViewModels;
using SkiaSharp.Views.WPF;
using System.Windows.Controls;

namespace EftDmaRadarLite.UI.Radar.Views
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
