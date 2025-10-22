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

using LoneEftDmaRadar.Web.TarkovDev.Data;

namespace LoneEftDmaRadar.UI.Data
{
    public sealed class StaticContainerEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private StaticContainerEntry() { }

        public StaticContainerEntry(TarkovMarketItem container)
        {
            Name = container.ShortName;
            Id = container.BsgId;
            _isTracked = App.Config.Containers.Selected.ContainsKey(container.BsgId);
        }


        public string Id { get; }
        public string Name { get; }

        private bool _isTracked;
        public bool IsTracked
        {
            get => _isTracked;
            set
            {
                if (_isTracked != value)
                {
                    _isTracked = value;
                    if (_isTracked)
                    {
                        App.Config.Containers.Selected.TryAdd(Id, 0);
                    }
                    else
                    {
                        App.Config.Containers.Selected.TryRemove(Id, out _);
                    }
                    OnPropertyChanged(nameof(IsTracked));
                }
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is StaticContainerEntry other)
            {
                return string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase);
            }
            if (obj is string id)
            {
                return string.Equals(Id, id, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Id);
        }
    }
}
