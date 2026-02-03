using eft_dma_radar.UI.Radar;
using eft_dma_radar.UI.Radar.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
