/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
namespace LoneEftDmaRadar.Tarkov.World.Quests
{
    /// <summary>
    /// Represents a quest entry with enable/disable functionality.
    /// </summary>
    public sealed class QuestEntry
    {
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
                if (value)
                    Program.Config.QuestHelper.BlacklistedQuests.TryRemove(Id, out _);
                else
                    Program.Config.QuestHelper.BlacklistedQuests.TryAdd(Id, 0);
            }
        }

        public QuestEntry(string id)
        {
            Id = id;
            Name = TarkovDataManager.TaskData.TryGetValue(id, out var task)
                ? task.Name ?? id
                : id;
            _isEnabled = !Program.Config.QuestHelper.BlacklistedQuests.ContainsKey(id);
        }

        public override string ToString() => Name;
    }
}
