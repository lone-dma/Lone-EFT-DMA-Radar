namespace EftDmaRadarLite.UI.ColorPicker
{
    /// <summary>
    /// Interaction logic for ColorPickerWindow.xaml
    /// </summary>
    public partial class ColorPickerWindow : Window
    {
        public ColorPickerViewModel ViewModel { get; }
        public ColorPickerWindow()
        {
            InitializeComponent();
            DataContext = ViewModel = new ColorPickerViewModel(this);
        }
    }
}
