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
using System.Windows.Shapes;

namespace eft_dma_radar.UI.Misc
{
    /// <summary>
    /// Interaction logic for InputBoxWindow.xaml
    /// </summary>
    public sealed partial class InputBoxWindow : Window
    {
        public InputBoxViewModel ViewModel { get; }
        public string InputText => ViewModel.InputText;

        public InputBoxWindow(string title, string prompt)
        {
            InitializeComponent();
            DataContext = ViewModel = new InputBoxViewModel(title, prompt);

            ViewModel.CloseRequested += (s, e) =>
            {
                DialogResult = e.DialogResult;
                Close();
            };
        }
    }
}
