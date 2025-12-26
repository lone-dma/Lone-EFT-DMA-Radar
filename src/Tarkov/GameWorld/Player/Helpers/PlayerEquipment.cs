using LoneEftDmaRadar.Tarkov.Unity.Collections;
using LoneEftDmaRadar.Web.TarkovDev;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace LoneEftDmaRadar.Tarkov.GameWorld.Player.Helpers
{
    public sealed class PlayerEquipment
    {
        private const string SECURED_CONTAINER_SLOT = "SecuredContainer";
        private static readonly FrozenSet<string> _skipSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Dogtag", "Compass", "ArmBand", "Eyewear", "Pockets"
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ulong> _slots = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, TarkovMarketItem> _items = new(StringComparer.OrdinalIgnoreCase);
        private readonly ObservedPlayer _player;
        private bool _inited;
        private int _cachedValue;
        private ulong _hands;

        /// <summary>
        /// Player's eqiuipped gear by slot.
        /// </summary>
        public IReadOnlyDictionary<string, TarkovMarketItem> Items => _items;
        /// <summary>
        /// Player's secured container item.
        /// </summary>
        [MaybeNull]
        public TarkovMarketItem SecuredContainer
        {
            get
            {
                _ = _items.TryGetValue(SECURED_CONTAINER_SLOT, out var item);
                return item;
            }
        }
        /// <summary>
        /// Player's item in hands.
        /// </summary>
        [MaybeNull]
        public TarkovMarketItem InHands { get; private set; }
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
            _ = Task.Run(InitAsnyc); // Lazy init
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
            GetEquipment(checkInit);
            GetHands();
        }

        private void GetEquipment(bool checkInit = true)
        {
            try
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
                            _items[slot.Key] = item;
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
            catch (Exception ex)
            {
                Logging.WriteLine($"Error refreshing Player Equipment for '{_player.Name}': {ex}");
            }
        }

        private void GetHands()
        {
            if (!_player.IsHuman) // Don't care about non-human players' hands
                return;
            try
            {
                var handsController = Memory.ReadPtr(_player.HandsControllerAddr); // or FirearmController
                var itemBase = Memory.ReadPtr(handsController + Offsets.ObservedPlayerHandsController._item);
                if (itemBase != _hands)
                {
                    InHands = null;
                    var itemTemplate = Memory.ReadPtr(itemBase + Offsets.LootItem.Template);
                    var itemMongoId = Memory.ReadValue<MongoID>(itemTemplate + Offsets.ItemTemplate._id);
                    var itemID = itemMongoId.ReadString();
                    if (TarkovDataManager.AllItems.TryGetValue(itemID, out var heldItem)) // Item exists in DB
                    {
                        InHands = heldItem;
                    }
                    else // Item doesn't exist in DB , use name from game memory
                    {
                        var itemNamePtr = Memory.ReadPtr(itemTemplate + Offsets.ItemTemplate.ShortName);
                        var itemName = Memory.ReadUnityString(itemNamePtr)?.Trim();
                        if (string.IsNullOrEmpty(itemName))
                            itemName = "Item";
                        InHands = new()
                        {
                            Name = itemName,
                            ShortName = itemName
                        };
                    }
                    _hands = itemBase;
                }
            }
            catch (Exception ex)
            {
                InHands = null;
                _hands = default;
                Logging.WriteLine($"Error refreshing Player Hands for '{_player.Name}': {ex}");
            }
        }
    }
}
