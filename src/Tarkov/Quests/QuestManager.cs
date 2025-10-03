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
using EftDmaRadarLite.Mono.Collections;
using EftDmaRadarLite.Tarkov.Data;
using EftDmaRadarLite.Unity;
using EftDmaRadarLite.Unity.Structures;
using System.Collections.Frozen;

namespace EftDmaRadarLite.Tarkov.Quests
{
    public sealed class QuestManager
    {
        private static readonly FrozenDictionary<string, string> _mapToId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "factory4_day", "55f2d3fd4bdc2d5f408b4567" },
            { "factory4_night", "59fc81d786f774390775787e" },
            { "bigmap", "56f40101d2720b2a4d8b45d6" },
            { "woods", "5704e3c2d2720bac5b8b4567" },
            { "lighthouse", "5704e4dad2720bb55b8b4567" },
            { "shoreline", "5704e554d2720bac5b8b456e" },
            { "rezervbase", "5704e5fad2720bc05b8b4567" },
            { "interchange", "5714dbc024597771384a510d" },
            { "tarkovstreets", "5714dc692459777137212e12" },
            { "laboratory", "5b0fc42d86f7744a585f9105" },
            { "Sandbox", "653e6760052c01c1c805532f" },
            { "Sandbox_high", "65b8d6f5cdde2479cb2a3125" },
            { "Labyrinth", "6733700029c367a3d40b02af" }
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        private static readonly FrozenDictionary<string, FrozenDictionary<string, Vector3>> _questZones = EftDataManager.TaskData.Values
            .Where(task => task.Objectives is not null) // Ensure the Objectives are not null
            .SelectMany(task => task.Objectives)   // Flatten the Objectives from each TaskElement
            .Where(objective => objective.Zones is not null) // Ensure the Zones are not null
            .SelectMany(objective => objective.Zones)    // Flatten the Zones from each Objective
            .Where(zone => zone.Position is not null && zone.Map?.Id is not null) // Ensure Position and Map are not null
            .GroupBy(zone => zone.Map.Id, zone => new
            {
                id = zone.Id,
                pos = new Vector3(zone.Position.X, zone.Position.Y, zone.Position.Z)
            }, StringComparer.OrdinalIgnoreCase)
            .DistinctBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key, // Map Id
                group => group
                .DistinctBy(x => x.id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    zone => zone.id,
                    zone => zone.pos,
                    StringComparer.OrdinalIgnoreCase
                ).ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase
            )
            .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        private readonly ulong _profile;
        private DateTimeOffset _last = DateTimeOffset.MinValue;

        public QuestManager(ulong profile)
        {
            _profile = profile;
        }

        private readonly ConcurrentDictionary<string, QuestEntry> _quests = new(StringComparer.OrdinalIgnoreCase); // Key = Quest ID
        /// <summary>
        /// All current quests.
        /// </summary>
        public IReadOnlyDictionary<string, QuestEntry> Quests => _quests;
        private readonly ConcurrentDictionary<string, byte> _items = new(StringComparer.OrdinalIgnoreCase); // Key = Item ID
        /// <summary>
        /// All item BSG ID's that we need to pickup.
        /// </summary>
        public IReadOnlyDictionary<string, byte> ItemConditions => _items;
        private readonly ConcurrentDictionary<string, QuestLocation> _locations = new(StringComparer.OrdinalIgnoreCase); // Key = Target ID
        /// <summary>
        /// All locations that we need to visit.
        /// </summary>
        public IReadOnlyDictionary<string, QuestLocation> LocationConditions => _locations;

        /// <summary>
        /// Map Identifier of Current Map.
        /// </summary>
        private static string MapID
        {
            get
            {
                var id = Memory.MapID;
                id ??= "MAPDEFAULT";
                return id;
            }
        }

        public void Refresh(CancellationToken ct)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                if (now - _last < TimeSpan.FromSeconds(1))
                    return;
                using var masterQuests = new PooledSet<string>(StringComparer.OrdinalIgnoreCase);
                using var masterItems = new PooledSet<string>(StringComparer.OrdinalIgnoreCase);
                using var masterLocations = new PooledSet<string>(StringComparer.OrdinalIgnoreCase);
                var questsData = Memory.ReadPtr(_profile + Offsets.Profile.QuestsData);
                using var questsDataList = MonoList<ulong>.Create(questsData, true);
                foreach (var qDataEntry in questsDataList) // GCLass1BBF
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var qStatus = Memory.ReadValue<int>(qDataEntry + Offsets.QuestData.Status);
                        if (qStatus != 2) // 2 == Started
                            continue;
                        var completedPtr = Memory.ReadPtr(qDataEntry + Offsets.QuestData.CompletedConditions);
                        using var completedHS = MonoHashSet<MongoID>.Create(completedPtr, true);
                        using var completedConditions = new PooledSet<string>();
                        foreach (var c in completedHS)
                        {
                            var completedCond = Memory.ReadUnicodeString(c.Value.StringID);
                            completedConditions.Add(completedCond);
                        }

                        var qIDPtr = Memory.ReadPtr(qDataEntry + Offsets.QuestData.Id);
                        var qID = Memory.ReadUnicodeString(qIDPtr);
                        masterQuests.Add(qID);
                        _ = _quests.GetOrAdd(
                            qID, 
                            id => new QuestEntry(id));
                        if (App.Config.QuestHelper.BlacklistedQuests.ContainsKey(qID))
                            continue;
                        var qTemplate = Memory.ReadPtr(qDataEntry + Offsets.QuestData.Template); // GClass1BF4
                        var qConditions =
                            Memory.ReadPtr(qTemplate + Offsets.QuestTemplate.Conditions); // EFT.Quests.QuestConditionsList
                        using var qCondDict = MonoDictionary<int, ulong>.Create(qConditions, true);
                        foreach (var qDicCondEntry in qCondDict)
                        {
                            var condListPtr = Memory.ReadPtr(qDicCondEntry.Value + Offsets.QuestConditionsContainer.ConditionsList);
                            using var condList = MonoList<ulong>.Create(condListPtr, true);
                            foreach (var condition in condList)
                                GetQuestConditions(qID, condition, completedConditions, masterItems, masterLocations);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[QuestManager] ERROR parsing Quest at 0x{qDataEntry.ToString("X")}: {ex}");
                    }
                }
                // Remove stale Quests/Items/Locations
                foreach (var oldQuest in _quests)
                {
                    if (!masterQuests.Contains(oldQuest.Key))
                    {
                        _quests.TryRemove(oldQuest.Key, out _);
                    }
                }
                foreach (var oldItem in _items)
                {
                    if (!masterItems.Contains(oldItem.Key))
                    {
                        _items.TryRemove(oldItem.Key, out _);
                    }
                }
                foreach (var oldLoc in _locations.Keys)
                {
                    if (!masterLocations.Contains(oldLoc))
                    {
                        _locations.TryRemove(oldLoc, out _);
                    }
                }
                _last = now;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Debug.WriteLine($"[QuestManager] CRITICAL ERROR: {ex}");
            }
        }

        private void GetQuestConditions(string questID, ulong condition, ISet<string> completedConditions,
            ISet<string> masterItems, ISet<string> masterLocations)
        {
            try
            {
                var condIDPtr = Memory.ReadValue<MongoID>(condition + Offsets.QuestCondition.id);
                var condID = Memory.ReadUnicodeString(condIDPtr.StringID);
                if (completedConditions.Contains(condID))
                    return;
                var condName = ObjectClass.ReadName(condition);
                if (condName == "ConditionFindItem" || condName == "ConditionHandoverItem")
                {
                    var targetArray =
                        Memory.ReadPtr(condition + Offsets.QuestConditionFindItem.target); // this is a typical unity array[] at 0x48
                    using var targets = MonoArray<ulong>.Create(targetArray, true);
                    foreach (var targetPtr in targets)
                    {
                        var target = Memory.ReadUnicodeString(targetPtr);
                        masterItems.Add(target);
                        _items.TryAdd(target, 0);
                    }
                }
                else if (condName == "ConditionPlaceBeacon" || condName == "ConditionLeaveItemAtLocation")
                {
                    var zoneIDPtr = Memory.ReadPtr(condition + Offsets.QuestConditionPlaceBeacon.zoneId);
                    var target = Memory.ReadUnicodeString(zoneIDPtr); // Zone ID
                    if (_mapToId.TryGetValue(MapID, out var id) &&
                        _questZones.TryGetValue(id, out var zones) &&
                        zones.TryGetValue(target, out var loc))
                    {
                        masterLocations.Add(target);
                        _ = _locations.GetOrAdd(
                            target,
                            t => new QuestLocation(questID, t, loc));
                    }
                }
                else if (condName == "ConditionVisitPlace")
                {
                    var targetPtr = Memory.ReadPtr(condition + Offsets.QuestConditionVisitPlace.target);
                    var target = Memory.ReadUnicodeString(targetPtr);
                    if (_mapToId.TryGetValue(MapID, out var id) &&
                        _questZones.TryGetValue(id, out var zones) &&
                        zones.TryGetValue(target, out var loc))
                    {
                        masterLocations.Add(target);
                        _ = _locations.GetOrAdd(
                            target,
                            t => new QuestLocation(questID, t, loc));
                    }
                }
                else if (condName == "ConditionCounterCreator") // Check for children
                {
                    var conditionsPtr = Memory.ReadPtr(condition + Offsets.QuestConditionCounterCreator.Conditions);
                    var conditionsListPtr = Memory.ReadPtr(conditionsPtr + Offsets.QuestConditionsContainer.ConditionsList);
                    using var counterList = MonoList<ulong>.Create(conditionsListPtr, true);
                    foreach (var childCond in counterList)
                        GetQuestConditions(questID, childCond, completedConditions, masterItems, masterLocations);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[QuestManager] ERROR parsing Condition(s): {ex}");
            }
        }
    }

   
}