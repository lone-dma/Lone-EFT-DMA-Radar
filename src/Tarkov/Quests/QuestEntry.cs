/*
 * EFT DMA Radar Lite
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
