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
using EftDmaRadarLite.Tarkov.Player;
using EftDmaRadarLite.UI.Loot;
using EftDmaRadarLite.UI.Radar.ViewModels;
using EftDmaRadarLite.Unity;
using EftDmaRadarLite.Unity.Collections;
using System.Collections.Frozen;
using VmmSharpEx;

namespace EftDmaRadarLite.Tarkov.Loot
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
                        .OrderByDescending(x => x.Important || (App.Config.QuestHelper.Enabled && x.IsQuestCondition))
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
                if (MainWindow.Instance?.Settings?.ViewModel is SettingsViewModel vm &&
                    vm.StaticContainersHideSearched)
                {
                    foreach (var container in StaticContainers)
                    {
                        container.RefreshSearchedStatus();
                    }
                }
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
            using var lootList = UnityList<ulong>.Create(
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
            using var deadPlayers = Memory.Players?
                .Where(x => x.Corpse is not null)?.ToPooledList();
            using var map = Memory.CreateScatterMap();
            var round1 = map.AddRound();
            var round2 = map.AddRound();
            var round3 = map.AddRound();
            var round4 = map.AddRound();
            for (int ix = 0; ix < lootList.Count; ix++)
            {
                int i = ix;
                ct.ThrowIfCancellationRequested();
                var lootBase = lootList[i];
                if (_loot.ContainsKey(lootBase))
                {
                    continue; // Already processed this loot item once before
                }
                round1[i].AddValueEntry<VmmPointer>(0, lootBase + ObjectClass.MonoBehaviourOffset); // MonoBehaviour
                round1[i].AddValueEntry<VmmPointer>(1, lootBase + ObjectClass.To_NamePtr[0]); // C1
                round1[i].Completed += (sender, x1) =>
                {
                    if (x1.TryGetValue<VmmPointer>(0, out var monoBehaviour) && x1.TryGetValue<VmmPointer>(1, out var c1))
                    {
                        round2[i].AddValueEntry<VmmPointer>(2,
                            monoBehaviour + MonoBehaviour.ObjectClassOffset); // InteractiveClass
                        round2[i].AddValueEntry<VmmPointer>(3, monoBehaviour + MonoBehaviour.GameObjectOffset); // GameObject
                        round2[i].AddValueEntry<VmmPointer>(4, c1 + ObjectClass.To_NamePtr[1]); // C2
                        round2[i].Completed += (sender, x2) =>
                        {
                            if (x2.TryGetValue<VmmPointer>(2, out var interactiveClass) &&
                                x2.TryGetValue<VmmPointer>(3, out var gameObject) &&
                                x2.TryGetValue<VmmPointer>(4, out var c2))
                            {
                                round3[i].AddValueEntry<VmmPointer>(5, c2 + ObjectClass.To_NamePtr[2]); // ClassNamePtr
                                round3[i].AddValueEntry<VmmPointer>(6, gameObject + GameObject.ComponentsOffset); // Components
                                round3[i].AddValueEntry<VmmPointer>(7, gameObject + GameObject.NameOffset); // PGameObjectName
                                round3[i].Completed += (sender, x3) =>
                                {
                                    if (x3.TryGetValue<VmmPointer>(5, out var classNamePtr) &&
                                        x3.TryGetValue<VmmPointer>(6, out var components)
                                        && x3.TryGetValue<VmmPointer>(7, out var pGameObjectName))
                                    {
                                        round4[i].AddStringEntry(8, classNamePtr, 64, Encoding.UTF8); // ClassName
                                        round4[i].AddStringEntry(9, pGameObjectName, 64, Encoding.UTF8); // ObjectName
                                        round4[i].AddValueEntry<VmmPointer>(10,
                                            components + 0x8); // T1
                                        round4[i].Completed += (sender, x4) =>
                                        {
                                            if (x4.TryGetString(8, out var className) &&
                                                x4.TryGetString(9, out var objectName) &&
                                                x4.TryGetValue<VmmPointer>(10, out var transformInternal))
                                            {
                                                map.Completed += (sender, _) => // Store this as callback, let scatter reads all finish first (benchmarked faster)
                                                {
                                                    ct.ThrowIfCancellationRequested();
                                                    try
                                                    {
                                                        var @params = new LootIndexParams
                                                        {
                                                            DeadPlayers = deadPlayers,
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
            // Post Scatter Read - Refresh Corpses
            foreach (var corpse in _loot.Values.OfType<LootCorpse>())
            {
                corpse.Refresh(deadPlayers);
            }
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
                    var corpse = new LootCorpse(interactiveClass)
                    {
                        Position = pos
                    };
                    _ = _loot.TryAdd(p.ItemBase, corpse);
                }
                else if (isContainer)
                {
                    try
                    {
                        if (p.ObjectName.Equals("loot_collider", StringComparison.OrdinalIgnoreCase))
                        {
                            _ = _loot.TryAdd(p.ItemBase, new LootAirdrop
                            {
                                Position = pos
                            });
                        }
                        else
                        {
                            var itemOwner = Memory.ReadPtr(interactiveClass + Offsets.LootableContainer.ItemOwner);
                            var ownerItemBase = Memory.ReadPtr(itemOwner + Offsets.LootableContainerItemOwner.RootItem);
                            var ownerItemTemplate = Memory.ReadPtr(ownerItemBase + Offsets.LootItem.Template);
                            var ownerItemBsgIdPtr = Memory.ReadValue<Types.MongoID>(ownerItemTemplate + Offsets.ItemTemplate._id);
                            var ownerItemBsgId = Memory.ReadUnityString(ownerItemBsgIdPtr.StringID);
                            _ = _loot.TryAdd(p.ItemBase, new StaticLootContainer(ownerItemBsgId, interactiveClass)
                            {
                                Position = pos
                            });
                        }
                    }
                    catch
                    {
                    }
                }
                else if (isLooseLoot)
                {
                    var item = Memory.ReadPtr(interactiveClass +
                                              Offsets.InteractiveLootItem.Item); //EFT.InventoryLogic.Item
                    var itemTemplate = Memory.ReadPtr(item + Offsets.LootItem.Template); //EFT.InventoryLogic.ItemTemplate
                    var isQuestItem = Memory.ReadValue<bool>(itemTemplate + Offsets.ItemTemplate.QuestItem);

                    //If NOT a quest item. Quest items are like the quest related things you need to find like the pocket watch or Jaeger's Letter etc. We want to ignore these quest items.
                    var BSGIdPtr = Memory.ReadValue<Types.MongoID>(itemTemplate + Offsets.ItemTemplate._id);
                    var id = Memory.ReadUnityString(BSGIdPtr.StringID);
                    if (isQuestItem)
                    {
                        QuestItem questItem;
                        if (EftDataManager.AllItems.TryGetValue(id, out var entry))
                        {
                            questItem = new QuestItem(entry)
                            {
                                Position = pos
                            };
                        }
                        else
                        {
                            var shortNamePtr = Memory.ReadPtr(itemTemplate + Offsets.ItemTemplate.ShortName);
                            var shortName = Memory.ReadUnityString(shortNamePtr)?.Trim();
                            if (string.IsNullOrEmpty(shortName))
                                shortName = "Item";
                            questItem = new QuestItem(id, $"Q_{shortName}")
                            {
                                Position = pos
                            };
                        }
                        _ = _loot.TryAdd(p.ItemBase, questItem);
                    }
                    else // Regular Loose Loot Item
                    {
                        if (EftDataManager.AllItems.TryGetValue(id, out var entry))
                        {
                            _ = _loot.TryAdd(p.ItemBase, new LootItem(entry)
                            {
                                Position = pos
                            });
                        }
                    }
                }
            }
        }

        private readonly struct LootIndexParams
        {
            public IReadOnlyList<PlayerBase> DeadPlayers { get; init; }

            public ulong ItemBase { get; init; }
            public ulong InteractiveClass { get; init; }
            public string ObjectName { get; init; }
            public ulong TransformInternal { get; init; }
            public string ClassName { get; init; }
        }

        #endregion

    }
}