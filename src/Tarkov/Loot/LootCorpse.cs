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
using EftDmaRadarLite.Tarkov.Data;
using EftDmaRadarLite.Tarkov.Player;
using EftDmaRadarLite.Unity.Mono.Collections;
using System.Collections.Frozen;

namespace EftDmaRadarLite.Tarkov.Loot
{
    public sealed class LootCorpse : LootContainer
    {
        private static readonly FrozenSet<string> _skipSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SecuredContainer", "Dogtag", "Compass", "Eyewear", "ArmBand"
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        private readonly ulong _corpse;
        private DateTimeOffset _last = DateTimeOffset.MinValue;
        /// <summary>
        /// Corpse container's associated player object (if any).
        /// </summary>
        public PlayerBase Player { get; private set; }
        /// <summary>
        /// Name of the corpse.
        /// </summary>
        public override string Name => Player?.Name ?? "Body";

        /// <summary>
        /// Constructor.
        /// </summary>
        public LootCorpse(ulong corpseAddr) : base()
        {
            _corpse = corpseAddr;
        }

        /// <summary>
        /// Refresh the loot on this corpse. Only slots are shown not bag contents.
        /// </summary>
        /// <param name="deadPlayers"></param>
        public void Refresh(IReadOnlyList<PlayerBase> deadPlayers)
        {
            var now = DateTimeOffset.UtcNow;
            if (now - _last < TimeSpan.FromSeconds(5))
                return;
            Player ??= deadPlayers?.FirstOrDefault(x => x.Corpse == _corpse);
            using var scannedItems = new PooledSet<ulong>(capacity: 12);
            GetCorpseLoot(_corpse, scannedItems, Loot, Player?.IsPmc ?? true);
            foreach (var existingItem in Loot.Keys) // Remove old loot
            {
                if (!scannedItems.Contains(existingItem))
                    Loot.TryRemove(existingItem, out _);
            }
            Player?.LootObject ??= this;
            _last = now;
        }

        /// <summary>
        /// Gets all loot on a corpse.
        /// </summary>
        private static void GetCorpseLoot(ulong lootInteractiveClass, ISet<ulong> scannedItems, ConcurrentDictionary<ulong, LootItem> containerLoot, bool isPMC)
        {
            try
            {
                var itemBase = Memory.ReadPtr(lootInteractiveClass + Offsets.InteractiveLootItem.Item);
                var slots = Memory.ReadPtr(itemBase + Offsets.LootItemMod.Slots);
                GetItemsInSlots(slots, scannedItems, containerLoot, isPMC);
            }
            catch { }
        }

        /// <summary>
        /// Recurse slots for gear.
        /// </summary>
        private static void GetItemsInSlots(ulong slotsPtr, ISet<ulong> scannedItems, ConcurrentDictionary<ulong, LootItem> containerLoot, bool isPMC)
        {
            using var slotDict = new PooledDictionary<string, ulong>(StringComparer.OrdinalIgnoreCase);
            using var slots = MonoArray<ulong>.Create(slotsPtr, true);

            foreach (var slot in slots)
            {
                var namePtr = Memory.ReadPtr(slot + Offsets.Slot.ID);
                var name = Memory.ReadUnityString(namePtr);
                if (!_skipSlots.Contains(name))
                    slotDict.TryAdd(name, slot);
            }

            foreach (var slot in slotDict)
            {
                try
                {
                    if (isPMC && slot.Key == "Scabbard")
                        continue;
                    var containedItem = Memory.ReadPtr(slot.Value + Offsets.Slot.ContainedItem);
                    scannedItems.Add(containedItem);
                    var inventorytemplate = Memory.ReadPtr(containedItem + Offsets.LootItem.Template);
                    var idPtr = Memory.ReadValue<MongoID>(inventorytemplate + Offsets.ItemTemplate._id);
                    var id = Memory.ReadUnityString(idPtr.StringID);
                    if (EftDataManager.AllItems.TryGetValue(id, out var entry))
                    {
                        _ = containerLoot.GetOrAdd(
                            containedItem, 
                            _ => new LootItem(entry));
                    }
                }
                catch
                {
                }
            }
        }
    }
}