using System.Collections.Frozen;
using eft_dma_radar.Misc;
using eft_dma_radar.Tarkov.Player;
using eft_dma_radar.UI.Loot;
using eft_dma_radar.Unity;
using eft_dma_radar.Unity.Collections;
using eft_dma_radar.Tarkov.Data;
using VmmSharpEx;

namespace eft_dma_radar.Tarkov.Loot
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
        /// All Static Loot Containers on the map.
        /// </summary>
        public IReadOnlyList<StaticLootContainer> StaticLootContainers { get; private set; }

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
            using var lootList = new UnityList<ulong>(
                addr: lootListAddr, 
                useCache: true);
            // Remove any loot no longer present
            var lootListHs = lootList.ToHashSet();
            foreach (var existing in _loot.Keys) 
            {
                if (!lootListHs.Contains(existing))
                {
                    _ = _loot.TryRemove(existing, out _);
                }
            }
            // Proceed to get new loot
            var containers = new List<StaticLootContainer>(64);
            var deadPlayers = Memory.Players?
                .Where(x => x.Corpse is not null)?.ToList();
            using var map = Memory.GetScatterMap();
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
                                                        ProcessLootIndex(
                                                            loot: _loot, 
                                                            containers: containers, 
                                                            deadPlayers: deadPlayers,
                                                            itemBase: lootBase,
                                                            interactiveClass: interactiveClass, 
                                                            objectName: objectName,
                                                            transformInternal: transformInternal, 
                                                            className: className);
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
            StaticLootContainers = containers;
        }

        /// <summary>
        /// Process a single loot index.
        /// </summary>
        private static void ProcessLootIndex(ConcurrentDictionary<ulong, LootItem> loot, List<StaticLootContainer> containers, IReadOnlyList<PlayerBase> deadPlayers,
            ulong itemBase, ulong interactiveClass, string objectName, ulong transformInternal, string className)
        {
            var isCorpse = className.Contains("Corpse", StringComparison.OrdinalIgnoreCase);
            var isLooseLoot = className.Equals("ObservedLootItem", StringComparison.OrdinalIgnoreCase);
            var isContainer = className.Equals("LootableContainer", StringComparison.OrdinalIgnoreCase);
            if (objectName.Contains("script", StringComparison.OrdinalIgnoreCase))
            {
                // skip these
            }
            else
            {
                // Get Item Position
                var pos = new UnityTransform(transformInternal, true).UpdatePosition();
                if (isCorpse)
                {
                    var player = deadPlayers?.FirstOrDefault(x => x.Corpse == interactiveClass);
                    var corpseLoot = new List<LootItem>();
                    bool isPMC = player?.IsPmc ?? true; // Default to true to omit things like Red Rebel Scabbard if we're not sure
                    GetCorpseLoot(interactiveClass, corpseLoot, isPMC);
                    var corpse = new LootCorpse(corpseLoot)
                    {
                        Position = pos,
                        PlayerObject = player
                    };
                    _ = loot.TryAdd(itemBase, corpse);
                    if (player is not null)
                    {
                        player.LootObject = corpse;
                    }
                }
                else if (isContainer)
                {
                    try
                    {
                        if (objectName.Equals("loot_collider", StringComparison.OrdinalIgnoreCase))
                        {
                            _ = loot.TryAdd(itemBase, new LootAirdrop
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
                            bool containerOpened = Memory.ReadValue<ulong>(interactiveClass + Offsets.LootableContainer.InteractingPlayer) != 0;
                            containers.Add(new StaticLootContainer(ownerItemBsgId, containerOpened)
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
                        _ = loot.TryAdd(itemBase, questItem);
                    }
                    else // Regular Loose Loot Item
                    {
                        if (EftDataManager.AllItems.TryGetValue(id, out var entry))
                        {
                            _ = loot.TryAdd(itemBase, new LootItem(entry)
                            {
                                Position = pos
                            });
                        }
                    }
                }
            }
        }

        private static readonly FrozenSet<string> _skipSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SecuredContainer", "Dogtag", "Compass", "Eyewear", "ArmBand"
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Recurse slots for gear.
        /// </summary>
        private static void GetItemsInSlots(ulong slotsPtr, List<LootItem> loot, bool isPMC)
        {
            var slotDict = new Dictionary<string, ulong>(StringComparer.OrdinalIgnoreCase);
            using var slots = new UnityArray<ulong>(slotsPtr, true);

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
                    var inventorytemplate = Memory.ReadPtr(containedItem + Offsets.LootItem.Template);
                    var idPtr = Memory.ReadValue<Types.MongoID>(inventorytemplate + Offsets.ItemTemplate._id);
                    var id = Memory.ReadUnityString(idPtr.StringID);
                    if (EftDataManager.AllItems.TryGetValue(id, out var entry))
                        loot.Add(new LootItem(entry));
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Gets all loot on a corpse.
        /// </summary>
        private static void GetCorpseLoot(ulong lootInteractiveClass, List<LootItem> loot, bool isPMC)
        {
            var itemBase = Memory.ReadPtr(lootInteractiveClass + Offsets.InteractiveLootItem.Item);
            var slots = Memory.ReadPtr(itemBase + Offsets.LootItemMod.Slots);
            try
            {
                GetItemsInSlots(slots, loot, isPMC);
            }
            catch
            {
            }
        }

        #endregion

    }
}