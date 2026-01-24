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

using LoneEftDmaRadar.Tarkov.Unity.Collections;
using LoneEftDmaRadar.Tarkov.Unity.Structures;
using LoneEftDmaRadar.Tarkov.World.Player.Helpers;
using LoneEftDmaRadar.Web.TarkovDev;
using System.Collections.Frozen;
using VmmSharpEx.Scatter;

namespace LoneEftDmaRadar.Tarkov.World.Player
{
    public class ObservedPlayer : AbstractPlayer
    {
        private readonly GameWorld _gameWorld;
        private readonly RaidCache _raidCache;
        /// <summary>
        /// Player's Unique Id within this Raid Instance [Human Players Only].
        /// </summary>
        public int Id { get; }
        private string _specialName; // Backing field for special roles
        /// <summary>
        /// Player's In-Game Name as displayed on the Radar.
        /// </summary>
        public override string Name
        {
            get => _specialName ?? base.Name;
            protected set => base.Name = value;
        }
        private PlayerType? _specialType; // Backing field for special roles
        /// <summary>
        /// Player's Type.
        /// </summary>
        public override PlayerType Type
        {
            get => _specialType ?? base.Type;
            protected set => base.Type = value;
        }
        /// <summary>
        /// Player's Current TarkovDevItems.
        /// </summary>
        public PlayerEquipment Equipment { get; }
        /// <summary>
        /// Address of InventoryController field.
        /// </summary>
        public ulong InventoryControllerAddr { get; }
        /// <summary>
        /// Hands Controller field address.
        /// </summary>
        public ulong HandsControllerAddr { get; }
        /// <summary>
        /// ObservedPlayerController for non-clientplayer players.
        /// </summary>
        private ulong ObservedPlayerController { get; }
        /// <summary>
        /// ObservedHealthController for non-clientplayer players.
        /// </summary>
        private ulong ObservedHealthController { get; }
        /// <summary>
        /// Player is Human-Controlled.
        /// </summary>
        public override bool IsHuman { get; }
        /// <summary>
        /// MovementContext / StateContext
        /// </summary>
        public override ulong MovementContext { get; }
        /// <summary>
        /// Corpse field address..
        /// </summary>
        public override ulong CorpseAddr { get; }
        /// <summary>
        /// Player Rotation Field Address (view angles).
        /// </summary>
        public override ulong RotationAddress { get; }
        /// <summary>
        /// Player's Current Health Status
        /// </summary>
        public Enums.ETagStatus HealthStatus { get; private set; } = Enums.ETagStatus.Healthy;

        internal ObservedPlayer(ulong playerBase, GameWorld gameWorld) : base(playerBase)
        {
            _gameWorld = gameWorld!;
            _raidCache = Config.Cache.RaidCache!; // Populate Raid Cache Access
            ObservedPlayerController = Memory.ReadPtr(this + Offsets.ObservedPlayerView.ObservedPlayerController);
            ArgumentOutOfRangeException.ThrowIfNotEqual(this,
                Memory.ReadValue<ulong>(ObservedPlayerController + Offsets.ObservedPlayerController.PlayerView),
                nameof(ObservedPlayerController));
            InventoryControllerAddr = ObservedPlayerController + Offsets.ObservedPlayerController.InventoryController;
            HandsControllerAddr = ObservedPlayerController + Offsets.ObservedPlayerController.HandsController;
            ObservedHealthController = Memory.ReadPtr(ObservedPlayerController + Offsets.ObservedPlayerController.HealthController);
            ArgumentOutOfRangeException.ThrowIfNotEqual(this,
                Memory.ReadValue<ulong>(ObservedHealthController + Offsets.ObservedHealthController._player),
                nameof(ObservedHealthController));
            CorpseAddr = ObservedHealthController + Offsets.ObservedHealthController._playerCorpse;

            MovementContext = GetMovementContext();
            RotationAddress = ValidateRotationAddr(MovementContext + Offsets.ObservedPlayerStateContext.Rotation);
            /// Setup Transform
            var ti = Memory.ReadPtrChain(this, false, _transformInternalChain);
            SkeletonRoot = new UnityTransform(ti);
            _ = SkeletonRoot.UpdatePosition();

            bool isAI = Memory.ReadValue<bool>(this + Offsets.ObservedPlayerView.IsAI);
            IsHuman = !isAI;
            Id = GetPlayerId();
            /// Determine Player Type
            PlayerSide = (Enums.EPlayerSide)Memory.ReadValue<int>(this + Offsets.ObservedPlayerView.Side); // Usec,Bear,Scav,etc.
            if (!Enum.IsDefined(PlayerSide)) // Make sure PlayerSide is valid
                throw new ArgumentOutOfRangeException(nameof(PlayerSide));
            if (IsScav)
            {
                if (isAI)
                {
                    if (_raidCache.SpecialAi.TryGetValue(Id, out var specialRole))
                    {
                        Name = specialRole.Name;
                        Type = specialRole.Type;
                    }
                    else
                    {
                        var voicePtr = Memory.ReadPtr(this + Offsets.ObservedPlayerView.Voice);
                        string voice = Memory.ReadUnityString(voicePtr);
                        var role = GetInitialAIRole(voice);
                        Name = role.Name;
                        Type = role.Type;
                    }
                }
                else
                {
                    Name = $"PScav{Id}";
                    Type = PlayerType.PScav;
                }
            }
            else if (IsPmc)
            {
                Name = $"{PlayerSide}{Id}";
                Type = PlayerType.PMC;
            }
            else
                throw new NotImplementedException(nameof(PlayerSide));
            Equipment = new PlayerEquipment(this);
            GroupId = TryGetGroup(Id);
            if (GroupId == TeammateGroupId)
            {
                Type = PlayerType.Teammate;
            }
            IsFocused = _raidCache is RaidCache rc && rc.Focused.ContainsKey(Id);
        }

        /// <summary>
        /// If the Player is Human, Toggle Teammate Status.
        /// </summary>
        public void ToggleTeammate()
        {
            bool isTeammate = Type == PlayerType.Teammate;
            if (!IsHuman)
                return;
            AssignTeammate(!isTeammate);
        }

        /// <summary>
        /// Assign this Player to a Group.
        /// </summary>
        /// <param name="groupId"></param>
        public void AssignGroup(int groupId)
        {
            GroupId = groupId;
        }

        /// <summary>
        /// Assign this Player as a Teammate to <see cref="LocalPlayer"/>.
        /// </summary>
        /// <param name="isTeammate">True if the Player is a Teammate, otherwise false.</param>
        public void AssignTeammate(bool isTeammate)
        {
            if (isTeammate)
            {
                Type = PlayerType.Teammate;
                GroupId = TeammateGroupId;
            }
            else
            {
                Type = PlayerSide == Enums.EPlayerSide.Savage ? PlayerType.PScav : PlayerType.PMC;
                GroupId = SoloGroupId;
            }
        }

        /// <summary>
        /// Focus or Unfocus this Player.
        /// </summary>
        /// <param name="isFocused">True to focus the player, otherwise false.</param>
        public override void SetFocus(bool isFocused)
        {
            IsFocused = isFocused;
            if (isFocused)
            {
                _raidCache.Focused.TryAdd(Id, 0);
            }
            else
            {
                _ = _raidCache.Focused.TryRemove(Id, out _);
            }
        }

        /// <summary>
        /// Get the Player's ID.
        /// </summary>
        /// <returns>Player Id.</returns>
        private int GetPlayerId()
        {
            return Memory.ReadValueEnsure<int>(this + Offsets.ObservedPlayerView.Id);
        }

        /// <summary>
        /// Tries to get an existing Group Id (if possible).
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private int TryGetGroup(int id)
        {
            if (Config.Misc.AutoGroups && IsHuman && IsPmc &&
                _raidCache is RaidCache raidCache &&
                raidCache.Groups.TryGetValue(id, out var group))
            {
                return group;
            }
            return SoloGroupId;
        }

        /// <summary>
        /// Get Movement Context Instance.
        /// </summary>
        private ulong GetMovementContext()
        {
            var movementController = Memory.ReadPtrChain(ObservedPlayerController, true, Offsets.ObservedPlayerController.MovementController, Offsets.ObservedPlayerMovementController.ObservedPlayerStateContext);
            return movementController;
        }

        /// <summary>
        /// Sync Player Information.
        /// </summary>
        public override void OnRegRefresh(VmmScatter scatter, ISet<ulong> registered, bool? isActiveParam = null)
        {
            if (isActiveParam is not bool isActive)
                isActive = registered.Contains(this);
            if (isActive)
            {
                UpdateHealthStatus();
            }
            base.OnRegRefresh(scatter, registered, isActive);
        }

        /// <summary>
        /// Get Player's Updated Health Condition
        /// Only works in Online Mode.
        /// </summary>
        private void UpdateHealthStatus()
        {
            try
            {
                var tag = (Enums.ETagStatus)Memory.ReadValue<int>(ObservedHealthController + Offsets.ObservedHealthController.HealthStatus);
                if ((tag & Enums.ETagStatus.Dying) == Enums.ETagStatus.Dying)
                    HealthStatus = Enums.ETagStatus.Dying;
                else if ((tag & Enums.ETagStatus.BadlyInjured) == Enums.ETagStatus.BadlyInjured)
                    HealthStatus = Enums.ETagStatus.BadlyInjured;
                else if ((tag & Enums.ETagStatus.Injured) == Enums.ETagStatus.Injured)
                    HealthStatus = Enums.ETagStatus.Injured;
                else
                    HealthStatus = Enums.ETagStatus.Healthy;
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"ERROR updating Health Status for '{Name}': {ex}");
            }
        }

        private static readonly uint[] _transformInternalChain =
        [
            Offsets.ObservedPlayerView.PlayerBody,
            Offsets.PlayerBody.SkeletonRootJoint,
            Offsets.DizSkinningSkeleton._values,
            UnityList<byte>.ArrOffset,
            UnityList<byte>.ArrStartOffset + (uint)Bones.HumanBase * 0x8,
            0x10
        ];

        #region AI Player Roles

        private static readonly FrozenDictionary<string, AIRole> _aiRolesByVoice = new Dictionary<string, AIRole>(StringComparer.OrdinalIgnoreCase)
        {
            ["BossSanitar"] = new() { Name = "Sanitar", Type = PlayerType.AIBoss },
            ["BossBully"] = new() { Name = "Reshala", Type = PlayerType.AIBoss },
            ["BossGluhar"] = new() { Name = "Gluhar", Type = PlayerType.AIBoss },
            ["SectantPriest"] = new() { Name = "Priest", Type = PlayerType.AIBoss },
            ["SectantWarrior"] = new() { Name = "Cultist", Type = PlayerType.AIRaider },
            ["BossKilla"] = new() { Name = "Killa", Type = PlayerType.AIBoss },
            ["BossTagilla"] = new() { Name = "Tagilla", Type = PlayerType.AIBoss },
            ["Boss_Partizan"] = new() { Name = "Partisan", Type = PlayerType.AIBoss },
            ["BossBigPipe"] = new() { Name = "Big Pipe", Type = PlayerType.AIBoss },
            ["BossBirdEye"] = new() { Name = "Birdeye", Type = PlayerType.AIBoss },
            ["BossKnight"] = new() { Name = "Knight", Type = PlayerType.AIBoss },
            ["Arena_Guard_1"] = new() { Name = "Arena Guard", Type = PlayerType.AIScav },
            ["Arena_Guard_2"] = new() { Name = "Arena Guard", Type = PlayerType.AIScav },
            ["Boss_Kaban"] = new() { Name = "Kaban", Type = PlayerType.AIBoss },
            ["Boss_Kollontay"] = new() { Name = "Kollontay", Type = PlayerType.AIBoss },
            ["Boss_Sturman"] = new() { Name = "Shturman", Type = PlayerType.AIBoss },
            ["Zombie_Generic"] = new() { Name = "Zombie", Type = PlayerType.AIScav },
            ["BossZombieTagilla"] = new() { Name = "Zombie Tagilla", Type = PlayerType.AIBoss },
            ["Zombie_Fast"] = new() { Name = "Zombie", Type = PlayerType.AIScav },
            ["Zombie_Medium"] = new() { Name = "Zombie", Type = PlayerType.AIScav },
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Get Initial AI Role for this player.
        /// Checks voice line against known roles, otherwise falls back to pattern matching, and other misc. checks.
        /// </summary>
        /// <returns><see cref="AIRole"/> for this player.</returns>
        private AIRole GetInitialAIRole(string voiceLine)
        {
            if (!_aiRolesByVoice.TryGetValue(voiceLine, out AIRole role))
            {
                // Fallback pattern matching
                role = voiceLine switch
                {
                    _ when voiceLine.Contains("scav", StringComparison.OrdinalIgnoreCase) => new() { Name = "Scav", Type = PlayerType.AIScav },
                    _ when voiceLine.Contains("boss", StringComparison.OrdinalIgnoreCase) => new() { Name = "Boss", Type = PlayerType.AIBoss },
                    _ when voiceLine.Contains("usec", StringComparison.OrdinalIgnoreCase) => new() { Name = "Usec", Type = PlayerType.AIRaider },
                    _ when voiceLine.Contains("bear", StringComparison.OrdinalIgnoreCase) => new() { Name = "Bear", Type = PlayerType.AIRaider },
                    _ when voiceLine.Contains("black_division", StringComparison.OrdinalIgnoreCase) => new() { Name = "BD", Type = PlayerType.AIRaider },
                    _ when voiceLine.Contains("vsrf", StringComparison.OrdinalIgnoreCase) => new() { Name = "Vsrf", Type = PlayerType.AIRaider },
                    _ when voiceLine.Contains("civilian", StringComparison.OrdinalIgnoreCase) => new() { Name = "Civ", Type = PlayerType.AIScav },
                    _ => new() { Name = "AI", Type = PlayerType.AIScav }
                };
            }

            // Labs Raider Check
            if (_gameWorld.MapID == "laboratory" && role.Type != PlayerType.AIBoss)
            {
                role = new("Raider", PlayerType.AIRaider);
            }
            return role;
        }

        /// <summary>
        /// Assign a Special AI Role for this player.
        /// Usually done during pre-raid checks for Santa/Guards/etc.
        /// </summary>
        /// <param name="name">New special role to be set. Set to <see langword="null"/> to revert to original role.</param>
        public void AssignSpecialAiRole(AIRole? role)
        {
            if (role is AIRole value)
            {
                _specialName = value.Name;
                _specialType = value.Type;
                _raidCache.SpecialAi.TryAdd(Id, value);
            }
            else
            {
                _specialName = null;
                _specialType = null;
                _ = _raidCache.SpecialAi.TryRemove(Id, out _);
            }
        }

        /// <summary>
        /// Check's if this AI Unit is a "special" role based on special checks.
        /// </summary>
        /// <returns><see cref="AIRole"/> value if a special unit, otherwise <see langword="null"/>.</returns>
        public AIRole? GetSpecialAiRole()
        {
            if (!IsAI || Equipment?.Items?.Values is not IEnumerable<TarkovMarketItem> items)
                return null;
            if (items.Any(i => i.BsgId == "61b9e1aaef9a1b5d6a79899a")) // Santa's Bag
            {
                return new("Santa", PlayerType.AIBoss);
            }
            else if (items.Any(i => i.BsgId == "63626d904aa74b8fe30ab426")) // Zryachiy's balaclava
            {
                return new("Zryachiy", PlayerType.AIBoss);
            }
            return null;
        }

        #endregion
    }

    public readonly record struct AIRole(string Name, PlayerType Type);

}
