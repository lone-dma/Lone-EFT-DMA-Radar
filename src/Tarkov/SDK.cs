namespace SDK
{
    public readonly partial struct Offsets
    {
        public readonly partial struct GameWorld
        {
            public const uint Location = 0xB8; // String
        }

        public readonly partial struct ClientLocalGameWorld
        {
            public const uint BtrController = 0x50; // -.\uF07E
            public const uint LootList = 0x178; // System.Collections.Generic.List<\uE311>
            public const uint RegisteredPlayers = 0x190; // System.Collections.Generic.List<IPlayer>
            public const uint MainPlayer = 0x1E0; // EFT.Player
            public const uint SynchronizableObjectLogicProcessor = 0x218; // -.\uEBD9
            public const uint Grenades = 0x258; // -.\uE3D7<Int32, Throwable>
        }

        public readonly partial struct SynchronizableObject
        {
            public const uint Type = 0x68; // System.Int32
        }

        public readonly partial struct SynchronizableObjectLogicProcessor
        {
            public const uint SynchronizableObjects = 0x18; // System.Collections.Generic.List<SynchronizableObject>
        }

        public readonly partial struct TripwireSynchronizableObject
        {
            public const uint _tripwireState = 0xE4; // System.Int32
            public const uint ToPosition = 0x16C; // UnityEngine.Vector3
        }

        public readonly partial struct BtrController
        {
            public const uint BtrView = 0x50; // EFT.Vehicle.BTRView
        }

        public readonly partial struct BTRView
        {
            public const uint turret = 0x60; // EFT.Vehicle.BTRTurretView
            public const uint _targetPosition = 0xAC; // UnityEngine.Vector3
        }

        public readonly partial struct BTRTurretView
        {
            public const uint AttachedBot = 0x60; // System.ValueTuple<ObservedPlayerView, Boolean>
        }

        public readonly partial struct Grenade
        {
            public const uint IsDestroyed = 0x4D; // Boolean
        }

        public readonly partial struct Player
        {
            public const uint MovementContext = 0x60; // EFT.MovementContext
            public const uint _playerBody = 0x190; // EFT.PlayerBody
            public const uint Corpse = 0x640; // EFT.Interactive.Corpse
            public const uint Location = 0x860; // String
            public const uint Profile = 0x8D8; // EFT.Profile
        }

        public readonly partial struct ObservedPlayerView
        {
            public const uint GroupID = 0x78; // String
            public const uint AccountId = 0xB0; // String
            public const uint PlayerBody = 0xC8; // EFT.PlayerBody
            public const uint ObservedPlayerController = 0x20; // -.\uED46
            public const uint Voice = 0xC8; // String
            public const uint Side = 0x8C; // System.Int32
            public const uint IsAI = 0x98; // Boolean
        }

        public readonly partial struct ObservedPlayerController
        {
            public const uint Player = 0x18; // EFT.NextObservedPlayer.ObservedPlayerView
            public const uint MovementController = 0xD8; // -.\uED4F
            public const uint HealthController = 0xE8; // -.\uE446
        }

        public readonly partial struct ObservedMovementController
        {
            public const uint ObservedPlayerStateContext = 0x98;
        }

        public readonly partial struct ObservedPlayerStateContext
        {
            public const uint Rotation = 0x20; // UnityEngine.Vector2
        }

        public readonly partial struct ObservedHealthController
        {
            public const uint Player = 0x18; // EFT.NextObservedPlayer.ObservedPlayerView
            public const uint PlayerCorpse = 0x20; // EFT.Interactive.ObservedCorpse
            public const uint HealthStatus = 0x10; // System.Int32
        }

        public readonly partial struct Profile
        {
            public const uint Id = 0x10; // String
            public const uint AccountId = 0x18; // String
            public const uint Info = 0x48; // -.\uE9AD
        }

        public readonly partial struct PlayerInfo
        {
            public const uint GroupId = 0x50; // String
            public const uint Side = 0x48; // [HUMAN] Int32
            public const uint RegistrationDate = 0x4C; // Int32
        }

        public readonly partial struct MovementContext
        {
            public const uint Player = 0x48; // EFT.Player
            public const uint _rotation = 0xC4; // UnityEngine.Vector2
        }

        public readonly partial struct InteractiveLootItem
        {
            public const uint Item = 0xF0; // EFT.InventoryLogic.Item
        }

        public readonly partial struct DizSkinningSkeleton
        {
            public const uint _values = 0x30; // System.Collections.Generic.List<Transform>
        }

        public readonly partial struct LootableContainer
        {
            public const uint ItemOwner = 0x168; // -.\uEFB4
        }

        public readonly partial struct LootableContainerItemOwner
        {
            public const uint RootItem = 0xD0; // EFT.InventoryLogic.Item
        }

        public readonly partial struct LootItem
        {
            public const uint Template = 0x60; // EFT.InventoryLogic.ItemTemplate
        }

        public readonly partial struct ItemTemplate
        {
            public const uint ShortName = 0x18; // String
            public const uint _id = 0xE0; // EFT.MongoID
            public const uint QuestItem = 0x34; // Boolean
        }

        public readonly partial struct PlayerBody
        {
            public const uint SkeletonRootJoint = 0x30; // Diz.Skinning.Skeleton
        }
    }

    public readonly partial struct Enums
    {
        public enum EPlayerSide
        {
            Usec = 1,
            Bear = 2,
            Savage = 4,
        }

        [Flags]
        public enum ETagStatus
        {
            Unaware = 1,
            Aware = 2,
            Combat = 4,
            Solo = 8,
            Coop = 16,
            Bear = 32,
            Usec = 64,
            Scav = 128,
            TargetSolo = 256,
            TargetMultiple = 512,
            Healthy = 1024,
            Injured = 2048,
            BadlyInjured = 4096,
            Dying = 8192,
            Birdeye = 16384,
            Knight = 32768,
            BigPipe = 65536,
        }

        [Flags]
        public enum EMemberCategory
        {
            Default = 0,
            Developer = 1,
            UniqueId = 2,
            Trader = 4,
            Group = 8,
            System = 16,
            ChatModerator = 32,
            ChatModeratorWithPermanentBan = 64,
            UnitTest = 128,
            Sherpa = 256,
            Emissary = 512,
            Unheard = 1024,
        }

        public enum WildSpawnType
        {
            marksman = 0,
            assault = 1,
            bossTest = 2,
            bossBully = 3,
            followerTest = 4,
            followerBully = 5,
            bossKilla = 6,
            bossKojaniy = 7,
            followerKojaniy = 8,
            pmcBot = 9,
            cursedAssault = 10,
            bossGluhar = 11,
            followerGluharAssault = 12,
            followerGluharSecurity = 13,
            followerGluharScout = 14,
            followerGluharSnipe = 15,
            followerSanitar = 16,
            bossSanitar = 17,
            test = 18,
            assaultGroup = 19,
            sectantWarrior = 20,
            sectantPriest = 21,
            bossTagilla = 22,
            followerTagilla = 23,
            exUsec = 24,
            gifter = 25,
            bossKnight = 26,
            followerBigPipe = 27,
            followerBirdEye = 28,
            bossZryachiy = 29,
            followerZryachiy = 30,
            bossBoar = 32,
            followerBoar = 33,
            arenaFighter = 34,
            arenaFighterEvent = 35,
            bossBoarSniper = 36,
            crazyAssaultEvent = 37,
            peacefullZryachiyEvent = 38,
            sectactPriestEvent = 39,
            ravangeZryachiyEvent = 40,
            followerBoarClose1 = 41,
            followerBoarClose2 = 42,
            bossKolontay = 43,
            followerKolontayAssault = 44,
            followerKolontaySecurity = 45,
            shooterBTR = 46,
            bossPartisan = 47,
            spiritWinter = 48,
            spiritSpring = 49,
            peacemaker = 50,
            pmcBEAR = 51,
            pmcUSEC = 52,
            skier = 53,
            sectantPredvestnik = 57,
            sectantPrizrak = 58,
            sectantOni = 59,
            infectedAssault = 60,
            infectedPmc = 61,
            infectedCivil = 62,
            infectedLaborant = 63,
            infectedTagilla = 64,
            bossTagillaAgro = 65,
            bossKillaAgro = 66,
            tagillaHelperAgro = 67,
        }

        public enum EExfiltrationStatus
        {
            NotPresent = 1,
            UncompleteRequirements = 2,
            Countdown = 3,
            RegularMode = 4,
            Pending = 5,
            AwaitsManualActivation = 6,
            Hidden = 7,
        }

        public enum SynchronizableObjectType
        {
            AirDrop = 0,
            AirPlane = 1,
            Tripwire = 2,
        }

        public enum ETripwireState
        {
            None = 0,
            Wait = 1,
            Active = 2,
            Exploding = 3,
            Exploded = 4,
            Inert = 5,
        }

        public enum EQuestStatus
        {
            Locked = 0,
            AvailableForStart = 1,
            Started = 2,
            AvailableForFinish = 3,
            Success = 4,
            Fail = 5,
            FailRestartable = 6,
            MarkedAsFailed = 7,
            Expired = 8,
            AvailableAfter = 9,
        }
    }
}
