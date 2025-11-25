namespace SDK
{
    public readonly partial struct Offsets
    {
        public readonly partial struct GameWorld
        {
            public const uint BtrController = 0x20; // EFT.Vehicle.BtrController
            public const uint LocationId = 0xB8; // string
            public const uint LootList = 0x178; // System.Collections.Generic.List<IKillable>
            public const uint RegisteredPlayers = 0x190; // System.Collections.Generic.List<IPlayer>
            public const uint MainPlayer = 0x1E0; // EFT.Player
            public const uint SynchronizableObjectLogicProcessor = 0x218; // EFT.SynchronizableObjects.SynchronizableObjectLogicProcessor
            public const uint Grenades = 0x258; // DictionaryListHydra<int, Throwable>
        }

        public readonly partial struct SynchronizableObject
        {
            public const uint Type = 0x68; // EFT.SynchronizableObjects.SynchronizableObjectType
        }

        public readonly partial struct SynchronizableObjectLogicProcessor
        {
            public const uint _activeSynchronizableObjects = 0x10; // System.Collections.Generic.List<SynchronizableObject>
        }

        public readonly partial struct TripwireSynchronizableObject
        {
            public const uint _tripwireState = 0xE4; // EFT.SynchronizableObjects.ETripwireState
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
            public const uint _bot = 0x60; // System.ValueTuple<ObservedPlayerView, bool>
        }

        public readonly partial struct Throwable
        {
            public const uint _isDestroyed = 0x4D; // bool
        }

        public readonly partial struct Player
        {
            public const uint MovementContext = 0x60; // EFT.MovementContext
            public const uint _playerBody = 0x190; // EFT.PlayerBody
            public const uint Corpse = 0x670; // EFT.Interactive.Corpse
            public const uint Location = 0x860; // string
            public const uint Profile = 0x8D8; // EFT.Profile
        }

        public readonly partial struct ObservedPlayerView
        {
            public const uint ObservedPlayerController = 0x20; // EFT.NextObservedPlayer.ObservedPlayerController
            public const uint Voice = 0x38; // string
            public const uint GroupID = 0x78; // string
            public const uint Side = 0x8C; // EFT.EPlayerSide
            public const uint IsAI = 0x98; // bool
            public const uint AccountId = 0xB0; // string
            public const uint PlayerBody = 0xC8; // EFT.PlayerBody
        }

        public readonly partial struct ObservedPlayerController
        {
            public const uint InventoryController = 0x10; // EFT.NextObservedPlayer.ObservedPlayerInventoryController
            public const uint PlayerView = 0x18; // EFT.NextObservedPlayer.ObservedPlayerView
            public const uint MovementController = 0xD8; // EFT.NextObservedPlayer.ObservedPlayerMovementController
            public const uint HealthController = 0xE8; // ObservedPlayerHealthController
        }

        public readonly partial struct InventoryController
        {
            public const uint Inventory = 0x100; // EFT.InventoryLogic.Inventory
        }

        public readonly partial struct Inventory
        {
            public const uint Equipment = 0x18; // EFT.InventoryLogic.InventoryEquipment
        }

        public readonly partial struct InventoryEquipment
        {
            public const uint _cachedSlots = 0x90; // EFT.InventoryLogic.Slot[]
        }

        public readonly partial struct Slot
        {
            public const uint ContainedItem = 0x48; // EFT.InventoryLogic.Item
            public const uint ID = 0x58; // string
        }

        public readonly partial struct ObservedPlayerMovementController
        {
            public const uint ObservedPlayerStateContext = 0x98; // EFT.NextObservedPlayer.ObservedPlayerStateContext
        }

        public readonly partial struct ObservedPlayerStateContext
        {
            public const uint Rotation = 0x20; // UnityEngine.Vector2
        }

        public readonly partial struct ObservedHealthController
        {
            public const uint HealthStatus = 0x10; // ETagStatus
            public const uint _player = 0x18; // EFT.NextObservedPlayer.ObservedPlayerView
            public const uint _playerCorpse = 0x20; // EFT.Interactive.ObservedCorpse
        }

        public readonly partial struct Profile
        {
            public const uint Id = 0x10; // string
            public const uint AccountId = 0x18; // string
            public const uint Info = 0x48; // EFT.ProfileInfo
        }

        public readonly partial struct PlayerInfo
        {
            public const uint Side = 0x48; // [HUMAN] Int32
            public const uint RegistrationDate = 0x4C; // int
            public const uint GroupId = 0x50; // string
        }

        public readonly partial struct MovementContext
        {
            public const uint Player = 0x48; // EFT.Player
            public const uint _rotation = 0xC4; // UnityEngine.Vector2
        }

        public readonly partial struct InteractiveLootItem
        {
            public const uint _item = 0xF0; // EFT.InventoryLogic.Item
        }

        public readonly partial struct DizSkinningSkeleton
        {
            public const uint _values = 0x30; // System.Collections.Generic.List<Transform>
        }

        public readonly partial struct LootableContainer
        {
            public const uint ItemOwner = 0x168; // EFT.InventoryLogic.ItemController
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
            public const uint ShortName = 0x18; // string
            public const uint QuestItem = 0x34; // bool
            public const uint _id = 0xE0; // EFT.MongoID
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
            BlackDivision = 131072,
            VSRF = 262144,
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
    }
}
