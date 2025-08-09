namespace eft_dma_radar.UI.Hotkeys
{
    /// <summary>
    /// Interaction logic for HotkeyManagerWindow.xaml
    /// </summary>
    public partial class HotkeyManagerWindow : Window
    {
        public HotkeyManagerViewModel ViewModel { get; }
        public HotkeyManagerWindow()
        {
            InitializeComponent();
            DataContext = ViewModel = new HotkeyManagerViewModel(this);
        }
    }
}
