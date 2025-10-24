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

namespace LoneEftDmaRadar.UI.Misc
{
    public class LoadingViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly PeriodicTimer _intervalTimer = new(TimeSpan.FromMilliseconds(233));
        private readonly LoadingWindow _parent;

        public LoadingViewModel(LoadingWindow parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        private double _progress;
        /// <summary>
        /// Progress value (0–100)
        /// </summary>
        public double Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged(nameof(Progress));
                }
            }
        }

        private string _statusText = "";
        /// <summary>
        /// Status message
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Call to update both progress bar and status text.
        /// </summary>
        public async Task UpdateProgressAsync(double percent, string status)
        {
            _parent.Dispatcher.Invoke(() =>
            {
                Progress = percent;
                StatusText = status;
            });
            await _intervalTimer.WaitForNextTickAsync();
        }

        public void Dispose()
        {
            _intervalTimer.Dispose();
        }
    }
}
