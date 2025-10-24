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

namespace LoneArenaDmaRadar.UI.Radar.ViewModels
{
    public sealed class RadarOverlayViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public RadarOverlayViewModel() { }

        // ─── Overlay visibility ────────────────────────────────────────────────
        private string _mapFreeButtonText = "Map Free";
        public string MapFreeButtonText
        {
            get => _mapFreeButtonText;
            set
            {
                if (_mapFreeButtonText == value) return;
                _mapFreeButtonText = value;
                OnPropertyChanged(nameof(MapFreeButtonText));
            }
        }
        private bool _isMapFreeEnabled;
        public bool IsMapFreeEnabled
        {
            get => _isMapFreeEnabled;
            set
            {
                if (_isMapFreeEnabled == value) return;
                _isMapFreeEnabled = value;
                if (_isMapFreeEnabled)
                {
                    MapFreeButtonText = "Map Follow";
                }
                else
                {
                    MapFreeButtonText = "Map Free";
                }
                OnPropertyChanged(nameof(IsMapFreeEnabled));
            }
        }
    }
}
