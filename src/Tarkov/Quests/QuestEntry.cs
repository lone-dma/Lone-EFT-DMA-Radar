using EftDmaRadarLite.Tarkov.Data;

namespace EftDmaRadarLite.Tarkov.Quests
{
    /// <summary>
    /// One-Way Binding Only
    /// </summary>
    public sealed class QuestEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public string Id { get; }
        public string Name { get; }
        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value) return;
                _isEnabled = value;
                if (value) // Enabled
                {
                    App.Config.QuestHelper.BlacklistedQuests.Remove(Id);
                }
                else
                {
                    App.Config.QuestHelper.BlacklistedQuests.Add(Id);
                }
                OnPropertyChanged(nameof(IsEnabled));
            }
        }
        public QuestEntry(string id)
        {
            Id = id;
            if (EftDataManager.TaskData.TryGetValue(id, out var task))
            {
                Name = task.Name ?? id;
            }
            else
            {
                Name = id;
            }
            _isEnabled = !App.Config.QuestHelper.BlacklistedQuests.Contains(id);
        }

        public override string ToString() => Name;
    }
}
