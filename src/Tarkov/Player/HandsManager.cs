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
using EftDmaRadarLite.Tarkov.Loot;
using EftDmaRadarLite.Unity.Collections;

namespace EftDmaRadarLite.Tarkov.Player
{
    public sealed class HandsManager
    {
        private readonly PlayerBase _parent;
        private LootItem _cachedItem;
        private ulong _cached;
        private string CurrentItem => _cachedItem?.ShortName ?? "--";
        private string _thermal;
        private string _ammo;
        /// <summary>
        /// String for display in UI.
        /// </summary>
        public string DisplayString
        {
            get
            {
                string aux = $"{_thermal},{_ammo}".Trim(',');
                if (!string.IsNullOrEmpty(aux))
                    aux = $" ({aux})";
                int len = 16 - (aux?.Length ?? 0);
                return $"{CurrentItem.Substring(0, Math.Min(CurrentItem.Length, len))}{aux}";
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
                    string thermal = null;
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
                            thermal = hasThermal ?
                                "T+" : null;
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
                    _thermal = thermal;
                }
                if (_cachedItem?.IsWeapon ?? false)
                {
                    string ammo = null;
                    try
                    {
                        var chambers = Memory.ReadPtr(itemBase + Offsets.LootItemWeapon.Chambers);
                        var slotPtr = Memory.ReadPtr(chambers + UnityList<byte>.ArrStartOffset + 0 * 0x8); // One in the chamber ;)
                        var slotItem = Memory.ReadPtr(slotPtr + Offsets.Slot.ContainedItem);
                        var ammoTemplate = Memory.ReadPtr(slotItem + Offsets.LootItem.Template);
                        var ammoIDPtr = Memory.ReadValue<Types.MongoID>(ammoTemplate + Offsets.ItemTemplate._id);
                        var ammoID = Memory.ReadUnityString(ammoIDPtr.StringID);
                        if (EftDataManager.AllItems.TryGetValue(ammoID, out var ammoItem))
                            ammo = ammoItem?.ShortName;
                    }
                    catch { }
                    _ammo = ammo;
                }
            }
            catch
            {
                _cached = 0x0;
            }
        }
    }
}
