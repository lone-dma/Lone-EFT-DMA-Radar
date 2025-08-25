using eft_dma_radar.Tarkov.Data;
using eft_dma_radar.Tarkov.Loot;
using eft_dma_radar.Unity.Collections;

namespace eft_dma_radar.Tarkov.Player.Plugins
{
    public sealed class HandsManager
    {
        private readonly PlayerBase _parent;

        private string _ammo;
        private string _thermal;
        private LootItem _cachedItem;
        private ulong _cached;
        /// <summary>
        /// Item in hands currently (Short Name).
        /// Also contains ammo/thermal info.
        /// </summary>
        public string CurrentItem
        {
            get
            {
                string at = $"{_ammo} {_thermal}".Trim();
                var item = _cachedItem?.ShortName;
                if (item is null) return "--";
                if (at != string.Empty)
                    return $"{item} ({at})";
                return item;
            }
        }

        public HandsManager(PlayerBase player)
        {
            _parent = player;
        }

        /// <summary>
        /// Check if item in player's hands has changed.
        /// </summary>
        public void Refresh()
        {
            try
            {
                var handsController = Memory.ReadPtr(_parent.HandsControllerAddr); // or FirearmController
                var itemBase = Memory.ReadPtr(handsController +
                    (_parent is ClientPlayer ?
                    Offsets.ItemHandsController.Item : Offsets.ObservedHandsController.ItemInHands));
                if (itemBase != _cached)
                {
                    _cachedItem = null;
                    _ammo = null;
                    _thermal = null;
                    var itemTemplate = Memory.ReadPtr(itemBase + Offsets.LootItem.Template);
                    var itemIDPtr = Memory.ReadValue<Types.MongoID>(itemTemplate + Offsets.ItemTemplate._id);
                    var itemID = Memory.ReadUnityString(itemIDPtr.StringID);
                    if (EftDataManager.AllItems.TryGetValue(itemID, out var heldItem)) // Item exists in DB
                    {
                        _cachedItem = new LootItem(heldItem);
                        if (heldItem?.IsWeapon ?? false)
                        {
                            bool hasThermal = _parent.Gear?.Loot?.Any(x =>
                                x.ID.Equals("5a1eaa87fcdbcb001865f75e", StringComparison.OrdinalIgnoreCase) || // REAP-IR
                                x.ID.Equals("5d1b5e94d7ad1a2b865a96b0", StringComparison.OrdinalIgnoreCase) || // FLIR
                                x.ID.Equals("6478641c19d732620e045e17", StringComparison.OrdinalIgnoreCase) || // ECHO
                                x.ID.Equals("63fc44e2429a8a166c7f61e6", StringComparison.OrdinalIgnoreCase))   // ZEUS
                                ?? false;
                            _thermal = hasThermal ?
                                "Thermal" : null;
                        }
                    }
                    else // Item doesn't exist in DB , use name from game memory
                    {
                        var itemNamePtr = Memory.ReadPtr(itemTemplate + Offsets.ItemTemplate.ShortName);
                        var itemName = Memory.ReadUnityString(itemNamePtr)?.Trim();
                        if (string.IsNullOrEmpty(itemName))
                            itemName = "Item";
                        _cachedItem = new("NULL", itemName);
                    }
                    _cached = itemBase;
                }
                if (_cachedItem?.IsWeapon ?? false)
                {
                    try
                    {
                        var chambers = Memory.ReadPtr(itemBase + Offsets.LootItemWeapon.Chambers);
                        var slotPtr = Memory.ReadPtr(chambers + UnityList<byte>.ArrStartOffset + 0 * 0x8); // One in the chamber ;)
                        var slotItem = Memory.ReadPtr(slotPtr + Offsets.Slot.ContainedItem);
                        var ammoTemplate = Memory.ReadPtr(slotItem + Offsets.LootItem.Template);
                        var ammoIDPtr = Memory.ReadValue<Types.MongoID>(ammoTemplate + Offsets.ItemTemplate._id);
                        var ammoID = Memory.ReadUnityString(ammoIDPtr.StringID);
                        if (EftDataManager.AllItems.TryGetValue(ammoID, out var ammo))
                            _ammo = ammo?.ShortName;
                    }
                    catch { }
                }
            }
            catch
            {
                _cached = 0x0;
            }
        }
    }
}
