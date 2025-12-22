namespace LoneEftDmaRadar.Tarkov.GameWorld.Quests
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
                    App.Config.QuestHelper.BlacklistedQuests.TryRemove(Id, out _);
                }
                else
                {
                    App.Config.QuestHelper.BlacklistedQuests.TryAdd(Id, 0);
                }
                OnPropertyChanged(nameof(IsEnabled));
            }
        }
        public QuestEntry(string id)
        {
            Id = id;
            if (TarkovDataManager.TaskData.TryGetValue(id, out var task))
            {
                Name = task.Name ?? id;
            }
            else
            {
                Name = id;
            }
            _isEnabled = !App.Config.QuestHelper.BlacklistedQuests.ContainsKey(id);
        }

        public override string ToString() => Name;
    }
}