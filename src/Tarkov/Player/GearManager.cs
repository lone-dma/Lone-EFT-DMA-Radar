using Collections.Pooled;
using eft_dma_radar.Misc;
using eft_dma_radar.Tarkov.Data;
using eft_dma_radar.Tarkov.Loot;
using eft_dma_radar.Unity.Collections;
using System.Collections.Frozen;

namespace eft_dma_radar.Tarkov.Player
{
    public sealed class GearManager
    {
        private static readonly FrozenSet<string> _skipSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SecuredContainer", "Dogtag", "Compass", "ArmBand"
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        private readonly bool _isPMC;

        public GearManager(PlayerBase player, bool isPMC = false)
        {
            _isPMC = isPMC;
            var slotDict = new Dictionary<string, ulong>(StringComparer.OrdinalIgnoreCase);
            var inventorycontroller = Memory.ReadPtr(player.InventoryControllerAddr);
            var inventory = Memory.ReadPtr(inventorycontroller + Offsets.InventoryController.Inventory);
            var equipment = Memory.ReadPtr(inventory + Offsets.Inventory.Equipment);
            var slots = Memory.ReadPtr(equipment + Offsets.Equipment.Slots);
            using var slotsArray = new UnityArray<ulong>(slots, true);

            foreach (var slotPtr in slotsArray)
            {
                var namePtr = Memory.ReadPtr(slotPtr + Offsets.Slot.ID);
                var name = Memory.ReadUnityString(namePtr);
                if (_skipSlots.Contains(name))
                    continue;
                slotDict.TryAdd(name, slotPtr);
            }

            Slots = slotDict;
            Refresh();
        }

        private IReadOnlyDictionary<string, ulong> Slots { get; }

        /// <summary>
        /// Player's contained gear/loot.
        /// </summary>
        public IReadOnlyList<LootItem> Loot { get; private set; }

        /// <summary>
        /// True if Quest Items are contained in this loot pool.
        /// </summary>
        public bool HasQuestItems => Loot?.Any(x => x.IsQuestCondition) ?? false;

        /// <summary>
        /// Value of this player's Gear/Loot.
        /// </summary>
        public int Value { get; private set; }

        public void Refresh()
        {
            using var loot = new PooledList<LootItem>();
            foreach (var slot in Slots)
            {
                try
                {
                    if (_isPMC && slot.Key == "Scabbard")
                    {
                        continue; // skip pmc scabbard
                    }
                    var containedItem = Memory.ReadPtr(slot.Value + Offsets.Slot.ContainedItem);
                    var inventorytemplate = Memory.ReadPtr(containedItem + Offsets.LootItem.Template);
                    var idPtr = Memory.ReadValue<Types.MongoID>(inventorytemplate + Offsets.ItemTemplate._id);
                    var id = Memory.ReadUnityString(idPtr.StringID);
                    if (EftDataManager.AllItems.TryGetValue(id, out var entry1))
                        loot.Add(new LootItem(entry1));

                    if (EftDataManager.AllItems.TryGetValue(id, out var entry2))
                    {
                        if (slot.Key == "FirstPrimaryWeapon" || slot.Key == "SecondPrimaryWeapon") // Only interested in weapons / helmets
                        {
                            RecursePlayerGearSlots(containedItem, loot);
                        }
                    }
                }
                catch { } // Skip over empty slots
            }
            Loot = loot.OrderLoot().ToList();
            Value = loot.Sum(x => x.Price); // Get value of player's loot/gear
        }

        /// <summary>
        /// Checks a 'Primary' weapon for Ammo Type, and Thermal Scope.
        /// </summary>
        private static void RecursePlayerGearSlots(ulong lootItemBase, IList<LootItem> loot)
        {
            try
            {
                var parentSlots = Memory.ReadPtr(lootItemBase + Offsets.LootItemMod.Slots);
                using var slotsArray = new UnityArray<ulong>(parentSlots, true);
                using var slotDict = new PooledDictionary<string, ulong>(StringComparer.OrdinalIgnoreCase);

                foreach (var slotPtr in slotsArray)
                {
                    var namePtr = Memory.ReadPtr(slotPtr + Offsets.Slot.ID);
                    var name = Memory.ReadUnityString(namePtr);
                    slotDict.TryAdd(name, slotPtr);
                }

                foreach (var slotName in slotDict.Keys)
                {
                    try
                    {
                        if (slotDict.TryGetValue(slotName, out var slot))
                        {
                            var containedItem = Memory.ReadPtr(slot + Offsets.Slot.ContainedItem);
                            var inventorytemplate = Memory.ReadPtr(containedItem + Offsets.LootItem.Template);
                            var idPtr = Memory.ReadValue<Types.MongoID>(inventorytemplate + Offsets.ItemTemplate._id);
                            var id = Memory.ReadUnityString(idPtr.StringID);
                            if (EftDataManager.AllItems.TryGetValue(id, out var entry))
                                loot.Add(new LootItem(entry)); // Add to loot, get weapon attachment values
                            RecursePlayerGearSlots(containedItem, loot);
                        }
                    }
                    catch { } // Skip over empty slots
                }
            }
            catch { }
        }
    }
}