using System.Collections.Frozen;
using Collections.Pooled;
using EftDmaRadarLite.Tarkov.Data;
using EftDmaRadarLite.Unity;
using EftDmaRadarLite.Unity.Collections;

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

        private readonly Stopwatch _rateLimit = new();
        private readonly ulong _profile;

        public QuestManager(ulong profile)
        {
            _profile = profile;
            Refresh();
        }

        private readonly ConcurrentDictionary<string, QuestEntry> _currentQuests = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Currently logged quests.
        /// </summary>
        public IReadOnlyDictionary<string, QuestEntry> CurrentQuests => _currentQuests;
        /// <summary>
        /// Contains a List of BSG ID's that we need to pickup.
        /// </summary>
        public IReadOnlySet<string> ItemConditions { get; private set; } = new HashSet<string>();

        /// <summary>
        /// Contains a List of locations that we need to visit.
        /// </summary>
        public IReadOnlyList<QuestLocation> LocationConditions { get; private set; } = new List<QuestLocation>();

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

        public void Refresh()
        {
            if (_rateLimit.IsRunning && _rateLimit.Elapsed.TotalSeconds < 2d)
                return;
            using var currentQuests = new PooledSet<string>(StringComparer.OrdinalIgnoreCase);
            var masterItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var masterLocations = new List<QuestLocation>();
            var questsData = Memory.ReadPtr(_profile + Offsets.Profile.QuestsData);
            using var questsDataList = new UnityList<ulong>(questsData, true);
            foreach (var qDataEntry in questsDataList) // GCLass1BBF
            {
                try
                {
                    var qStatus = Memory.ReadValue<int>(qDataEntry + Offsets.QuestData.Status);
                    if (qStatus != 2) // 2 == Started
                        continue;
                    var completedPtr = Memory.ReadPtr(qDataEntry + Offsets.QuestData.CompletedConditions);
                    using var completedHS = new UnityHashSet<Types.MongoID>(completedPtr, true);
                    using var completedConditions = new PooledSet<string>();
                    foreach (var c in completedHS)
                    {
                        var completedCond = Memory.ReadUnityString(c.Value.StringID);
                        completedConditions.Add(completedCond);
                    }

                    var qIDPtr = Memory.ReadPtr(qDataEntry + Offsets.QuestData.Id);
                    var qID = Memory.ReadUnityString(qIDPtr);
                    currentQuests.Add(qID);
                    if (App.Config.QuestHelper.BlacklistedQuests.Contains(qID))
                        continue;
                    var qTemplate = Memory.ReadPtr(qDataEntry + Offsets.QuestData.Template); // GClass1BF4
                    var qConditions =
                        Memory.ReadPtr(qTemplate + Offsets.QuestTemplate.Conditions); // EFT.Quests.QuestConditionsList
                    using var qCondDict = new UnityDictionary<int, ulong>(qConditions, true);
                    foreach (var qDicCondEntry in qCondDict)
                    {
                        var condListPtr = Memory.ReadPtr(qDicCondEntry.Value + Offsets.QuestConditionsContainer.ConditionsList);
                        using var condList = new UnityList<ulong>(condListPtr, true);
                        foreach (var condition in condList)
                            GetQuestConditions(qID, condition, completedConditions, masterItems, masterLocations);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[QuestManager] ERROR parsing Quest at 0x{qDataEntry.ToString("X")}: {ex}");
                }
            }
            // Update Current Quests for Data Binding
            foreach (var currentQuest in currentQuests)
            {
                var entry = new QuestEntry(currentQuest);
                _currentQuests.TryAdd(currentQuest, entry);
            }
            foreach (var currentQuest in _currentQuests)
            {
                if (!currentQuests.Contains(currentQuest.Key, StringComparer.OrdinalIgnoreCase))
                {
                    _currentQuests.TryRemove(currentQuest.Key, out _);
                }
            }
            ItemConditions = masterItems;
            LocationConditions = masterLocations;
            _rateLimit.Restart();
        }

        private static void GetQuestConditions(string questID, ulong condition, ISet<string> completedConditions,
            HashSet<string> items, List<QuestLocation> locations)
        {
            try
            {
                var condIDPtr = Memory.ReadValue<Types.MongoID>(condition + Offsets.QuestCondition.id);
                var condID = Memory.ReadUnityString(condIDPtr.StringID);
                if (completedConditions.Contains(condID))
                    return;
                var condName = ObjectClass.ReadName(condition);
                if (condName == "ConditionFindItem" || condName == "ConditionHandoverItem")
                {
                    var targetArray =
                        Memory.ReadPtr(condition + Offsets.QuestConditionFindItem.target); // this is a typical unity array[] at 0x48
                    using var targets = new UnityArray<ulong>(targetArray, true);
                    foreach (var targetPtr in targets)
                    {
                        var target = Memory.ReadUnityString(targetPtr);
                        items.Add(target);
                    }
                }
                else if (condName == "ConditionPlaceBeacon" || condName == "ConditionLeaveItemAtLocation")
                {
                    var zoneIDPtr = Memory.ReadPtr(condition + Offsets.QuestConditionPlaceBeacon.zoneId);
                    var target = Memory.ReadUnityString(zoneIDPtr); // Zone ID
                    if (_mapToId.TryGetValue(MapID, out var id) &&
                        _questZones.TryGetValue(id, out var zones) &&
                        zones.TryGetValue(target, out var loc))
                    {
                        locations.Add(new QuestLocation(questID, target, loc));
                    }
                }
                else if (condName == "ConditionVisitPlace")
                {
                    var targetPtr = Memory.ReadPtr(condition + Offsets.QuestConditionVisitPlace.target);
                    var target = Memory.ReadUnityString(targetPtr);
                    if (_mapToId.TryGetValue(MapID, out var id) &&
                        _questZones.TryGetValue(id, out var zones) &&
                        zones.TryGetValue(target, out var loc))
                    {
                        locations.Add(new QuestLocation(questID, target, loc));
                    }
                }
                else if (condName == "ConditionCounterCreator") // Check for children
                {
                    var conditionsPtr = Memory.ReadPtr(condition + Offsets.QuestConditionCounterCreator.Conditions);
                    var conditionsListPtr = Memory.ReadPtr(conditionsPtr + Offsets.QuestConditionsContainer.ConditionsList);
                    using var counterList = new UnityList<ulong>(conditionsListPtr, true);
                    foreach (var childCond in counterList)
                        GetQuestConditions(questID, childCond, completedConditions, items, locations);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[QuestManager] ERROR parsing Condition(s): {ex}");
            }
        }
    }

   
}