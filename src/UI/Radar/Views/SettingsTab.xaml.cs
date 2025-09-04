using eft_dma_radar.UI.Radar.ViewModels;
using System.Windows.Controls;

namespace eft_dma_radar.UI.Radar.Views
{
    /// <summary>
    /// Interaction logic for SettingsTab.xaml
    /// </summary>
    public partial class SettingsTab : UserControl
    {
        public SettingsViewModel ViewModel { get; }
        public SettingsTab()
        {
            InitializeComponent();
            DataContext = ViewModel = new SettingsViewModel(this);
        }
    }
}
