using eft_dma_radar.UI.Radar.ViewModels;
using System.Windows.Controls;

namespace eft_dma_radar.UI.Radar.Views
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
