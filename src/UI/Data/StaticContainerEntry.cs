using EftDmaRadarLite.Tarkov.Data.TarkovMarket;

namespace EftDmaRadarLite.UI.Data
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
            _isTracked = App.Config.Containers.Selected.Contains(container.BsgId);
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
                        App.Config.Containers.Selected.Add(Id);
                    }
                    else
                    {
                        App.Config.Containers.Selected.Remove(Id);
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
