namespace SDK
{
	public readonly partial struct ClassNames
	{
		// No data!
	}

	public readonly partial struct Offsets
	{
		public readonly partial struct GameWorld
		{
			public const uint Location = 0x90; // String
		}

		public readonly partial struct ClientLocalGameWorld
		{
			public const uint RegisteredPlayers = 0x140; // System.Collections.Generic.List<IPlayer>
			public const uint MainPlayer = 0x1B0; // EFT.Player
			public const uint Grenades = 0x210; // -.\uE400<Int32, Throwable>
			public const uint IsInRaid = 0x290; // [HUMAN] Bool
		}

		public readonly partial struct Player
		{
			public const uint _characterController = 0x50; // -.ICharacterController
			public const uint MovementContext = 0x68; // EFT.MovementContext
			public const uint _playerBody = 0xD0; // EFT.PlayerBody
			public const uint Corpse = 0x408; // EFT.Interactive.Corpse
			public const uint Profile = 0x650; // EFT.Profile
			public const uint _inventoryController = 0x6B0; // -.Player.PlayerInventoryController
		}

		public readonly partial struct ObservedPlayerView
		{
			public const uint NickName = 0x50; // String
			public const uint AccountId = 0x58; // String
			public const uint PlayerBody = 0x68; // EFT.PlayerBody
			public const uint ObservedPlayerController = 0x88; // -.\uEDD0
			public const uint Side = 0x108; // System.Int32
			public const uint IsAI = 0x119; // Boolean
			public const uint VisibleToCameraType = 0x120; // System.Int32
		}

		public readonly partial struct ObservedPlayerController
		{
			public const uint Player = 0x10; // EFT.NextObservedPlayer.ObservedPlayerView
			public static readonly uint[] MovementController = new uint[] { 0x100, 0x10 }; // -.\uEDF3, -.\uEDF5
			public const uint HandsController = 0x110; // -.\uEDDE
			public const uint HealthController = 0x128; // -.\uE473
			public const uint InventoryController = 0x150; // -.\uEDC6
		}

		public readonly partial struct ObservedMovementController
		{
			public const uint Rotation = 0x88; // UnityEngine.Vector2
		}

		public readonly partial struct ObservedHealthController
		{
			public const uint Player = 0x10; // EFT.NextObservedPlayer.ObservedPlayerView
			public const uint PlayerCorpse = 0x18; // EFT.Interactive.ObservedCorpse
			public const uint HealthStatus = 0xE0; // System.Int32
		}

		public readonly partial struct Profile
		{
			public const uint Id = 0x10; // String
			public const uint AccountId = 0x18; // String
			public const uint Info = 0x40; // -.\uE8F6
		}

		public readonly partial struct PlayerInfo
		{
			public const uint Nickname = 0x20; // String
			public const uint Settings = 0x58; // -.\uEA3C
			public const uint Side = 0xA0; // [HUMAN] Int32
			public const uint RegistrationDate = 0xA4; // Int32
			public const uint MemberCategory = 0xB0; // System.Int32
			public const uint Experience = 0xB4; // Int32
		}

		public readonly partial struct PlayerInfoSettings
		{
			public const uint Role = 0x10; // System.Int32
		}

		public readonly partial struct MovementContext
		{
			public const uint Player = 0x10; // EFT.Player
			public const uint _rotation = 0x290; // UnityEngine.Vector2
		}

		public readonly partial struct InventoryController
		{
			public const uint Inventory = 0x138; // EFT.InventoryLogic.Inventory
		}

		public readonly partial struct Inventory
		{
			public const uint Equipment = 0x10; // EFT.InventoryLogic.InventoryEquipment
		}

		public readonly partial struct Equipment
		{
			public const uint Slots = 0x98; // EFT.InventoryLogic.Slot[]
		}

		public readonly partial struct Slot
		{
			public const uint ContainedItem = 0x48; // EFT.InventoryLogic.Item
			public const uint ID = 0x58; // String
		}

		public readonly partial struct LootItem
		{
			public const uint Template = 0x58; // EFT.InventoryLogic.ItemTemplate
			public const uint Version = 0x80; // Int32
		}

		public readonly partial struct LootItemMod
		{
			public const uint Slots = 0x98; // EFT.InventoryLogic.Slot[]
		}

		public readonly partial struct ItemTemplate
		{
			public const uint ShortName = 0x18; // String
			public const uint _id = 0x68; // EFT.MongoID
		}

		public readonly partial struct Grenade
		{
			public const uint IsDestroyed = 0x5D; // Boolean
		}

		public readonly partial struct DizSkinningSkeleton
		{
			public const uint _values = 0x30; // System.Collections.Generic.List<Transform>
		}

		public readonly partial struct PlayerBody
		{
			public const uint SkeletonRootJoint = 0x30; // Diz.Skinning.Skeleton
		}

		public readonly partial struct NetworkGame
		{
			public const uint NetworkGameData = 0x70; // -.\uEA3B
		}

		public readonly partial struct NetworkGameData
		{
			public const uint raidMode = 0x4C; // System.Int32
		}
	}

	public readonly partial struct Enums
	{
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

		public enum ERaidMode
		{
			Online = 0,
			Local = 1,
			Coop = 2,
			OverRun = 3,
			TeamFight = 4,
			LastHero = 5,
			FinalRun = 6,
			OneManArmy = 7,
			Duel = 8,
			ShootOut = 9,
			ShootOutSolo = 10,
			ShootOutDuo = 11,
			ShootOutTrio = 12,
			BlastGang = 13,
			CheckPoint = 14,
		}

		public enum ArmbandColorType
		{
			red = 1,
			fuchsia = 2,
			yellow = 3,
			green = 4,
			azure = 5,
			white = 6,
			blue = 7,
			grey = 8,
		}

		public enum ECameraType
		{
			Default = 0,
			Spectator = 1,
			UIBackground = 2,
			KillCamera = 3,
		}
	}
}
