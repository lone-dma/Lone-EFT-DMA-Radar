/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
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
