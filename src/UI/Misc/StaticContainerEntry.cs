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

using LoneEftDmaRadar.Web.TarkovDev;

namespace LoneEftDmaRadar.UI.Misc
{
    public sealed class StaticContainerEntry
    {
        public StaticContainerEntry(TarkovMarketItem container)
        {
            Name = container.ShortName;
            Id = container.BsgId;
            _isTracked = Program.Config.Containers.Selected.ContainsKey(container.BsgId);
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
                        Program.Config.Containers.Selected.TryAdd(Id, 0);
                    else
                        Program.Config.Containers.Selected.TryRemove(Id, out _);
                }
            }
        }

        public override bool Equals(object obj) => obj switch
        {
            StaticContainerEntry other => string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase),
            string id => string.Equals(Id, id, StringComparison.OrdinalIgnoreCase),
            _ => false
        };

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Id);
    }
}