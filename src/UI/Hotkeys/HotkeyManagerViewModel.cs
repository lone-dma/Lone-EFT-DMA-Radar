using eft_dma_radar.Misc;
using eft_dma_radar.UI.Misc;
using eft_dma_radar.Unity;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace eft_dma_radar.UI.Hotkeys
{
    public sealed class HotkeyManagerViewModel : INotifyPropertyChanged
    {
        #region Static config loader

        // all possible UnityKeyCodes
        private static readonly IReadOnlyList<UnityKeyCode> _allKeys =
            Enum.GetValues<UnityKeyCode>()
                .Cast<UnityKeyCode>()
                .ToList();

        private static readonly ConcurrentDictionary<UnityKeyCode, HotkeyAction> _hotkeys = new();
        /// <summary>
        /// The live set of hotkeys (key → action)
        /// </summary>
        internal static IReadOnlyDictionary<UnityKeyCode, HotkeyAction> Hotkeys => _hotkeys;

        static HotkeyManagerViewModel()
        {
            foreach (var kvp in App.Config.Hotkeys)
            {
                var action = new HotkeyAction(kvp.Value);
                _hotkeys.TryAdd(kvp.Key, action);
            }
        }

        #endregion

        private readonly HotkeyManagerWindow _parent;
        public HotkeyManagerViewModel(HotkeyManagerWindow parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            // populate the two dropdowns:
            Controllers = new ObservableCollection<HotkeyActionController>(HotkeyAction.Controllers);
            AvailableKeys = new ObservableCollection<ComboHotkeyValue>(_allKeys.Select(code => new ComboHotkeyValue(code)));

            // seed the listbox from whatever was in config:
            foreach (var hotkey in _hotkeys)
            {
                HotkeyEntries.Add(new HotkeyListBoxEntry(hotkey.Key, hotkey.Value));
            }

            // wire up your commands
            AddCommand = new SimpleCommand(OnAdd);
            RemoveCommand = new SimpleCommand(OnRemove);
            CloseCommand = new SimpleCommand(OnClose);
        }

        // ── DATA ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// All registered action controllers (for "Actions" combo)
        /// </summary>
        public ObservableCollection<HotkeyActionController> Controllers { get; }

        /// <summary>
        /// All possible keys (for "Hotkeys" combo)
        /// </summary>
        public ObservableCollection<ComboHotkeyValue> AvailableKeys { get; }

        private HotkeyActionController _selectedAction;
        public HotkeyActionController SelectedAction
        {
            get => _selectedAction;
            set
            {
                if (_selectedAction == value) return;
                _selectedAction = value;
                OnPropertyChanged(nameof(SelectedAction));
            }
        }

        private ComboHotkeyValue _selectedKey;
        public ComboHotkeyValue SelectedKey
        {
            get => _selectedKey;
            set
            {
                if (_selectedKey == value) return;
                _selectedKey = value;
                OnPropertyChanged(nameof(SelectedKey));
            }
        }

        /// <summary>
        /// The listbox entries ("Action == Key")
        /// </summary>
        public ObservableCollection<HotkeyListBoxEntry> HotkeyEntries { get; } = new();

        private HotkeyListBoxEntry _selectedEntry;
        public HotkeyListBoxEntry SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (_selectedEntry == value) return;
                _selectedEntry = value;
                OnPropertyChanged(nameof(SelectedEntry));
            }
        }

        // ── COMMANDS ──────────────────────────────────────────────────────────────

        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand CloseCommand { get; }

        private void OnAdd()
        {
            if (SelectedAction is not HotkeyActionController actionController ||
                SelectedKey is not ComboHotkeyValue key)
                return;

            var action = new HotkeyAction(actionController.Name);
            // insert it
            if (_hotkeys.TryAdd(key.Code, action)) // No duplicates
            {
                HotkeyEntries.Add(new HotkeyListBoxEntry(key.Code, action));
                App.Config.Hotkeys[key.Code] = action.Name;
            }

            // clear the combos
            SelectedAction = null;
            SelectedKey = null;
        }

        private void OnRemove()
        {
            if (SelectedEntry is not HotkeyListBoxEntry entry) 
                return;
            HotkeyEntries.Remove(entry);
            _hotkeys.TryRemove(entry.Hotkey, out _);
            App.Config.Hotkeys.TryRemove(entry.Hotkey, out _);
            SelectedEntry = null;
        }

        private void OnClose()
        {
            _parent.DialogResult = true;
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────────

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
