using eft_dma_radar.UI.Misc;
using eft_dma_radar.UI.Skia.Maps;
using System.Windows.Input;

namespace eft_dma_radar.UI.Radar.ViewModels
{
    public sealed class MapSetupHelperViewModel : INotifyPropertyChanged
    {
        private float _x, _y, _scale;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool _isVisible;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible == value) return;
                _isVisible = value;
                SetCurrentMapValues();
                OnPropertyChanged();
            }
        }

        private string _coords = "coords";
        public string Coords
        {
            get => _coords;
            set
            {
                if (_coords == value) return;
                _coords = value;
                OnPropertyChanged();
            }
        }

        public float X
        {
            get => _x;
            set
            {
                if (_x == value) return;
                _x = value;
                OnPropertyChanged();
            }
        }

        public float Y
        {
            get => _y;
            set
            {
                if (_y == value) return;
                _y = value;
                OnPropertyChanged();
            }
        }

        public float Scale
        {
            get => _scale;
            set
            {
                if (_scale == value) return;
                _scale = value;
                OnPropertyChanged();
            }
        }

        public ICommand ApplyCommand { get; }

        public MapSetupHelperViewModel()
        {

            ApplyCommand = new SimpleCommand(OnApply);
        }

        private void SetCurrentMapValues()
        {
            if (EftMapManager.Map?.Config is EftMapConfig currentMap)
            {
                X = currentMap.X;
                Y = currentMap.Y;
                Scale = currentMap.Scale;
            }
        }

        private void OnApply()
        {
            if (EftMapManager.Map?.Config is EftMapConfig currentMap)
            {
                currentMap.X = _x;
                currentMap.Y = _y;
                currentMap.Scale = _scale;
            }
            else
            {
                MessageBox.Show(MainWindow.Instance, "No Map Loaded! Unable to apply.");
            }
        }
    }
}
