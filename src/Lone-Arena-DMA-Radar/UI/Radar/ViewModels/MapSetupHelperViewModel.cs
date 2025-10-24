/*
 * Lone EFT DMA Radar
 * Brought to you by Lone (Lone DMA)
 * 
MIT License

Copyright (c) 2025 Lone DMA

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 *
*/

using LoneArenaDmaRadar.UI.Misc;
using LoneArenaDmaRadar.UI.Radar.Maps;
using System.Windows.Input;

namespace LoneArenaDmaRadar.UI.Radar.ViewModels
{
    public sealed class MapSetupHelperViewModel : INotifyPropertyChanged
    {
        private string _x, _y, _scale;

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

        public string X
        {
            get => _x;
            set
            {
                if (_x == value) return;
                _x = value;
                OnPropertyChanged();
            }
        }

        public string Y
        {
            get => _y;
            set
            {
                if (_y == value) return;
                _y = value;
                OnPropertyChanged();
            }
        }

        public string Scale
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
                X = currentMap.X.ToString();
                Y = currentMap.Y.ToString();
                Scale = currentMap.Scale.ToString();
            }
        }

        private void OnApply()
        {
            if (EftMapManager.Map?.Config is EftMapConfig currentMap &&
                float.TryParse(_x, out float x) &&
                float.TryParse(_y, out float y) &&
                float.TryParse(_scale, out float scale))
            {
                currentMap.X = x;
                currentMap.Y = y;
                currentMap.Scale = scale;
            }
            else
            {
                MessageBox.Show(MainWindow.Instance, "No Map Loaded! Unable to apply.");
            }
        }
    }
}
