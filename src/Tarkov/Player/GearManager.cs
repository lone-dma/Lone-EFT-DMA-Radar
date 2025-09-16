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

using Collections.Pooled;
using EftDmaRadarLite.Misc;
using EftDmaRadarLite.Tarkov.Data;
using EftDmaRadarLite.Tarkov.Loot;
using EftDmaRadarLite.Unity.Collections;
using System.Collections.Frozen;

namespace EftDmaRadarLite.Tarkov.Player
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
            using var slotsArray = UnityArray<ulong>.Create(slots, true);

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
                using var slotsArray = UnityArray<ulong>.Create(parentSlots, true);
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