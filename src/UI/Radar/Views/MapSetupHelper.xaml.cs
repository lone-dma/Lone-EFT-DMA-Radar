using EftDmaRadarLite.UI.Radar.ViewModels;
using System.Windows.Controls;

namespace EftDmaRadarLite.UI.Radar.Views
{
    /// <summary>
    /// Interaction logic for MapSetupHelper.xaml
    /// </summary>
    public partial class MapSetupHelper : UserControl
    {
        public MapSetupHelperViewModel ViewModel { get; }
        public MapSetupHelper()
        {
            InitializeComponent();
            DataContext = ViewModel = new MapSetupHelperViewModel();
        }
    }
}
