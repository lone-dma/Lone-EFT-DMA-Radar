using LoneEftDmaRadar.Tarkov.GameWorld.Loot;
using LoneEftDmaRadar.Tarkov.Unity.Collections;
using System.Collections.Frozen;

namespace LoneEftDmaRadar.Tarkov.GameWorld.Player.Helpers
{
    public sealed class PlayerEquipment
    {
        private static readonly FrozenSet<string> _skipSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SecuredContainer", "Dogtag", "Compass", "ArmBand", "Eyewear", "Pockets"
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ulong> _slots = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, LootItem> _items = new(StringComparer.OrdinalIgnoreCase);
        private readonly ObservedPlayer _player;
        private bool _inited;
        private int _cachedValue;

        /// <summary>
        /// Player's eqiuipped gear by slot.
        /// </summary>
        public IReadOnlyDictionary<string, LootItem> Items => _items;
        /// <summary>
        /// Player's total equipment flea price value.
        /// </summary>
        public int Value => _cachedValue;
        /// <summary>
        /// True if the player is carrying any important loot items.
        /// </summary>
        public bool CarryingImportantLoot => _items?.Values?.Any(item => item.IsImportant) ?? false;

        public PlayerEquipment(ObservedPlayer player)
        {
            _player = player;
            Task.Run(InitAsnyc); // Lazy init
        }

        private async Task InitAsnyc()
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var inventorycontroller = Memory.ReadPtr(_player.InventoryControllerAddr);
                    var inventory = Memory.ReadPtr(inventorycontroller + Offsets.InventoryController.Inventory);
                    var equipment = Memory.ReadPtr(inventory + Offsets.Inventory.Equipment);
                    var slotsPtr = Memory.ReadPtr(equipment + Offsets.InventoryEquipment._cachedSlots);
                    using var slotsArray = UnityArray<ulong>.Create(slotsPtr, true);
                    ArgumentOutOfRangeException.ThrowIfLessThan(slotsArray.Count, 1);

                    foreach (var slotPtr in slotsArray)
                    {
                        var namePtr = Memory.ReadPtr(slotPtr + Offsets.Slot.ID);
                        var name = Memory.ReadUnityString(namePtr);
                        if (_skipSlots.Contains(name))
                            continue;
                        _slots.TryAdd(name, slotPtr);
                    }

                    Refresh(checkInit: false);
                    _inited = true;
                    return;
                }
                catch (Exception ex)
                {
                    Logging.WriteLine($"Error initializing Player Equipment for '{_player.Name}': {ex}");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }
        }

        public void Refresh(bool checkInit = true)
        {
            if (checkInit && !_inited)
                return;
            long totalValue = 0;
            foreach (var slot in _slots)
            {
                try
                {
                    if (_player.IsPmc && slot.Key == "Scabbard")
                        continue;

                    var containedItem = Memory.ReadPtr(slot.Value + Offsets.Slot.ContainedItem);
                    var inventorytemplate = Memory.ReadPtr(containedItem + Offsets.LootItem.Template);
                    var mongoId = Memory.ReadValue<MongoID>(inventorytemplate + Offsets.ItemTemplate._id);
                    var id = mongoId.ReadString();
                    if (TarkovDataManager.AllItems.TryGetValue(id, out var item))
                    {
                        _items[slot.Key] = new(item, default);
                        totalValue += item.FleaPrice;
                    }
                    else
                    {
                        _items.TryRemove(slot.Key, out _);
                    }
                }
                catch
                {
                    _items.TryRemove(slot.Key, out _);
                }
            }
            _cachedValue = (int)totalValue;
        }

    }
}
