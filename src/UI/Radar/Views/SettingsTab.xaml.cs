using EftDmaRadarLite.UI.Radar.ViewModels;
using System.Windows.Controls;

namespace EftDmaRadarLite.UI.Radar.Views
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
