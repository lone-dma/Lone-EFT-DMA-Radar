/*
 * Lone EFT DMA Radar
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
using LoneEftDmaRadar.Tarkov.Mono.Collections;
using LoneEftDmaRadar.Tarkov.Unity;
using LoneEftDmaRadar.Tarkov.Unity.Structures;
using LoneEftDmaRadar.UI.Loot;

namespace LoneEftDmaRadar.Tarkov.GameWorld.Loot.Helpers
{
    public sealed class LootManager
    {
        #region Fields/Properties/Constructor

        private readonly ulong _lgw;
        private readonly Lock _filterSync = new();
        private readonly ConcurrentDictionary<ulong, LootItem> _loot = new();

        /// <summary>
        /// All loot (with filter applied).
        /// </summary>
        public IReadOnlyList<LootItem> FilteredLoot { get; private set; }

        /// <summary>
        /// All Static Containers on the map.
        /// </summary>
        public IEnumerable<StaticLootContainer> StaticContainers => _loot.Values.OfType<StaticLootContainer>();

        public LootManager(ulong localGameWorld)
        {
            _lgw = localGameWorld;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Force a filter refresh.
        /// Thread Safe.
        /// </summary>
        public void RefreshFilter()
        {
            if (_filterSync.TryEnter())
            {
                try
                {
                    var filter = LootFilter.Create();
                    FilteredLoot = _loot.Values?
                        .Where(x => filter(x))
                        .OrderByDescending(x => x.Important)
                        .ThenByDescending(x => x?.Price ?? 0)
                        .ToList();
                }
                catch { }
                finally
                {
                    _filterSync.Exit();
                }
            }
        }

        /// <summary>
        /// Refreshes loot, only call from a memory thread (Non-GUI).
        /// </summary>
        public void Refresh(CancellationToken ct)
        {
            try
            {
                GetLoot(ct);
                RefreshFilter();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CRITICAL ERROR - Failed to refresh loot: {ex}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates referenced Loot List with fresh values.
        /// </summary>
        private void GetLoot(CancellationToken ct)
        {
            var lootListAddr = Memory.ReadPtr(_lgw + Offsets.ClientLocalGameWorld.LootList);
            using var lootList = MonoList<ulong>.Create(
                addr: lootListAddr,
                useCache: true);
            // Remove any loot no longer present
            using var lootListHs = lootList.ToPooledSet();
            foreach (var existing in _loot.Keys)
            {
                if (!lootListHs.Contains(existing))
                {
                    _ = _loot.TryRemove(existing, out _);
                }
            }
            // Proceed to get new loot
            using var map = Memory.CreateScatterMap();
            var round1 = map.AddRound();
            var round2 = map.AddRound();
            var round3 = map.AddRound();
            var round4 = map.AddRound();
            foreach (var lootBase in lootList)
            {
                ct.ThrowIfCancellationRequested();
                if (_loot.ContainsKey(lootBase))
                {
                    continue; // Already processed this loot item once before
                }
                round1.PrepareReadPtr(lootBase + ObjectClass.MonoBehaviourOffset); // UnityComponent
                round1.PrepareReadPtr(lootBase + ObjectClass.To_NamePtr[0]); // C1
                round1.Completed += (sender, s1) =>
                {
                    if (s1.ReadPtr(lootBase + ObjectClass.MonoBehaviourOffset, out var monoBehaviour) &&
                        s1.ReadPtr(lootBase + ObjectClass.To_NamePtr[0], out var c1))
                    {
                        round2.PrepareReadPtr(monoBehaviour + UnitySDK.UnityOffsets.Component_ObjectClassOffset); // InteractiveClass
                        round2.PrepareReadPtr(monoBehaviour + UnitySDK.UnityOffsets.Component_GameObjectOffset); // GameObject
                        round2.PrepareReadPtr(c1 + ObjectClass.To_NamePtr[1]); // C2
                        round2.Completed += (sender, s2) =>
                        {
                            if (s2.ReadPtr(monoBehaviour + UnitySDK.UnityOffsets.Component_ObjectClassOffset, out var interactiveClass) &&
                                s2.ReadPtr(monoBehaviour + UnitySDK.UnityOffsets.Component_GameObjectOffset, out var gameObject) &&
                                s2.ReadPtr(c1 + ObjectClass.To_NamePtr[1], out var classNamePtr))
                            {
                                round3.PrepareRead(classNamePtr, 64); // ClassName
                                round3.PrepareReadPtr(gameObject + UnitySDK.UnityOffsets.GameObject_ComponentsOffset); // Components
                                round3.PrepareReadPtr(gameObject + UnitySDK.UnityOffsets.GameObject_NameOffset); // PGameObjectName
                                round3.Completed += (sender, s3) =>
                                {
                                    if (s3.ReadString(classNamePtr, 64, Encoding.UTF8) is string className &&
                                        s3.ReadPtr(gameObject + UnitySDK.UnityOffsets.GameObject_ComponentsOffset, out var components)
                                        && s3.ReadPtr(gameObject + UnitySDK.UnityOffsets.GameObject_NameOffset, out var pGameObjectName))
                                    {
                                        round4.PrepareRead(pGameObjectName, 64); // ObjectName
                                        round4.PrepareReadPtr(components + 0x8); // T1
                                        round4.Completed += (sender, s4) =>
                                        {
                                            if (
                                                s4.ReadString(pGameObjectName, 64, Encoding.UTF8) is string objectName &&
                                                s4.ReadPtr(components + 0x8, out var transformInternal))
                                            {
                                                map.Completed += (sender, _) => // Store this as callback, let scatter reads all finish first (benchmarked faster)
                                                {
                                                    ct.ThrowIfCancellationRequested();
                                                    try
                                                    {
                                                        var @params = new LootIndexParams
                                                        {
                                                            ItemBase = lootBase,
                                                            InteractiveClass = interactiveClass,
                                                            ObjectName = objectName,
                                                            TransformInternal = transformInternal,
                                                            ClassName = className
                                                        };
                                                        ProcessLootIndex(ref @params);
                                                    }
                                                    catch
                                                    {
                                                    }
                                                };
                                            }
                                        };
                                    }
                                };
                            }
                        };
                    }
                };
            }
            map.Execute(); // execute scatter read
        }

        /// <summary>
        /// Process a single loot index.
        /// </summary>
        private void ProcessLootIndex(ref LootIndexParams p)
        {
            var isCorpse = p.ClassName.Contains("Corpse", StringComparison.OrdinalIgnoreCase);
            var isLooseLoot = p.ClassName.Equals("ObservedLootItem", StringComparison.OrdinalIgnoreCase);
            var isContainer = p.ClassName.Equals("LootableContainer", StringComparison.OrdinalIgnoreCase);
            var interactiveClass = p.InteractiveClass;

            if (p.ObjectName.Contains("script", StringComparison.OrdinalIgnoreCase))
            {
                // skip these
            }
            else
            {
                // Get Item Position
                var pos = new UnityTransform(p.TransformInternal, true).UpdatePosition();
                if (isCorpse)
                {
                    var corpse = new LootCorpse(pos);
                    _ = _loot.TryAdd(p.ItemBase, corpse);
                }
                if (isContainer)
                {
                    try
                    {
                        if (p.ObjectName.Equals("loot_collider", StringComparison.OrdinalIgnoreCase))
                        {
                            _ = _loot.TryAdd(p.ItemBase, new LootAirdrop(pos));
                        }
                        else
                        {
                            var itemOwner = Memory.ReadPtr(interactiveClass + Offsets.LootableContainer.ItemOwner);
                            var ownerItemBase = Memory.ReadPtr(itemOwner + Offsets.LootableContainerItemOwner.RootItem);
                            var ownerItemTemplate = Memory.ReadPtr(ownerItemBase + Offsets.LootItem.Template);
                            var ownerItemMongoId = Memory.ReadValue<MongoID>(ownerItemTemplate + Offsets.ItemTemplate._id);
                            var ownerItemId = ownerItemMongoId.ReadString();
                            _ = _loot.TryAdd(p.ItemBase, new StaticLootContainer(ownerItemId, pos));
                        }
                    }
                    catch
                    {
                    }
                }
                else if (isLooseLoot)
                {
                    var item = Memory.ReadPtr(interactiveClass + Offsets.InteractiveLootItem.Item); //EFT.InventoryLogic.Item
                    var itemTemplate = Memory.ReadPtr(item + Offsets.LootItem.Template); //EFT.InventoryLogic.ItemTemplate
                    var isQuestItem = Memory.ReadValue<bool>(itemTemplate + Offsets.ItemTemplate.QuestItem);

                    if (!isQuestItem)
                    {
                        //If NOT a quest item. Quest items are like the quest related things you need to find like the pocket watch or Jaeger's Letter etc. We want to ignore these quest items.
                        var mongoId = Memory.ReadValue<MongoID>(itemTemplate + Offsets.ItemTemplate._id);
                        var id = mongoId.ReadString();
                        if (TarkovDataManager.AllItems.TryGetValue(id, out var entry))
                        {
                            _ = _loot.TryAdd(p.ItemBase, new LootItem(entry, pos));
                        }
                    }
                }
            }
        }

        private readonly struct LootIndexParams
        {
            public ulong ItemBase { get; init; }
            public ulong InteractiveClass { get; init; }
            public string ObjectName { get; init; }
            public ulong TransformInternal { get; init; }
            public string ClassName { get; init; }
        }

        #endregion

    }
}