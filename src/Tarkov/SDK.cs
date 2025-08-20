namespace SDK
{
	public readonly partial struct ClassNames
	{
		public readonly partial struct OpticCameraManagerContainer
		{
			public const uint ClassName_ClassToken = 0x2002F5F; // MDToken
			public const string ClassName = @"\uF124";
		}

		public readonly partial struct ScreenManager
		{
			public const uint ClassName_ClassToken = 0x200369A; // MDToken
			public const string ClassName = @"\uF1EF";
		}
	}

	public readonly partial struct Offsets
	{
		public readonly partial struct GameWorld
		{
			public const uint Location = 0x90; // String
		}

		public readonly partial struct ClientLocalGameWorld
		{
			public const uint TransitController = 0x20; // -.\uE868
			public const uint ExfilController = 0x30; // -.\uE731
			public const uint BtrController = 0x50; // -.\uF07E
			public const uint ClientShellingController = 0x80; // -.\uE742
			public const uint LocationId = 0x90; // String
			public const uint LootList = 0x118; // System.Collections.Generic.List<\uE311>
			public const uint RegisteredPlayers = 0x140; // System.Collections.Generic.List<IPlayer>
			public const uint MainPlayer = 0x1A8; // EFT.Player
			public const uint SynchronizableObjectLogicProcessor = 0x1D8; // -.\uEBD9
			public const uint Grenades = 0x200; // -.\uE3D7<Int32, Throwable>
		}

		public readonly partial struct TransitController
		{
			public const uint TransitPoints = 0x18; // System.Collections.Generic.Dictionary<Int32, TransitPoint>
		}

		public readonly partial struct ClientShellingController
		{
			public const uint ActiveClientProjectiles = 0x68; // System.Collections.Generic.Dictionary<Int32, ArtilleryProjectileClient>
		}

		public readonly partial struct ArtilleryProjectileClient
		{
			public const uint Position = 0x34; // UnityEngine.Vector3
			public const uint IsActive = 0x40; // Boolean
		}

		public readonly partial struct TransitPoint
		{
			public const uint parameters = 0x20; // -.\uE6CC.Location.TransitParameters
		}

		public readonly partial struct TransitParameters
		{
			public const uint name = 0x10; // String
			public const uint description = 0x18; // String
			public const uint location = 0x30; // String
		}

		public readonly partial struct SynchronizableObject
		{
			public const uint Type = 0x70; // System.Int32
		}

		public readonly partial struct SynchronizableObjectLogicProcessor
		{
			public const uint SynchronizableObjects = 0x18; // System.Collections.Generic.List<SynchronizableObject>
		}

		public readonly partial struct TripwireSynchronizableObject
		{
			public const uint GrenadeTemplateId = 0x110; // EFT.MongoID
			public const uint _tripwireState = 0x16C; // System.Int32
			public const uint FromPosition = 0x170; // UnityEngine.Vector3
			public const uint ToPosition = 0x17C; // UnityEngine.Vector3
		}

		public readonly partial struct BtrController
		{
			public const uint BtrView = 0x30; // EFT.Vehicle.BTRView
		}

		public readonly partial struct BTRView
		{
			public const uint turret = 0x50; // EFT.Vehicle.BTRTurretView
			public const uint _targetPosition = 0xF8; // UnityEngine.Vector3
		}

		public readonly partial struct BTRTurretView
		{
			public const uint AttachedBot = 0x50; // System.ValueTuple<ObservedPlayerView, Boolean>
		}

		public readonly partial struct ExfilController
		{
			public const uint ExfiltrationPointArray = 0x28; // EFT.Interactive.ExfiltrationPoint[]
			public const uint ScavExfiltrationPointArray = 0x30; // EFT.Interactive.ScavExfiltrationPoint[]
			public const uint SecretExfiltrationPointArray = 0x38; // EFT.Interactive.SecretExfiltrations.SecretExfiltrationPoint[]
		}

		public readonly partial struct Exfil
		{
			public const uint Settings = 0x78; // EFT.Interactive.ExitTriggerSettings
			public const uint EligibleEntryPoints = 0xA0; // System.String[]
			public const uint _status = 0xC8; // System.Byte
		}

		public readonly partial struct ScavExfil
		{
			public const uint EligibleIds = 0xE0; // System.Collections.Generic.List<String>
		}

		public readonly partial struct ExfilSettings
		{
			public const uint Name = 0x18; // String
		}

		public readonly partial struct Grenade
		{
			public const uint IsDestroyed = 0x5D; // Boolean
			public const uint WeaponSource = 0x80; // -.\uEF81
		}

		public readonly partial struct Player
		{
			public const uint _characterController = 0x40; // -.ICharacterController
			public const uint MovementContext = 0x58; // EFT.MovementContext
			public const uint _playerBody = 0xC0; // EFT.PlayerBody
			public const uint ProceduralWeaponAnimation = 0x1E0; // EFT.Animations.ProceduralWeaponAnimation
			public const uint Corpse = 0x3E0; // EFT.Interactive.Corpse
			public const uint Location = 0x5E0; // String
			public const uint InteractableObject = 0x5F0; // EFT.Interactive.InteractableObject
			public const uint Profile = 0x618; // EFT.Profile
			public const uint Physical = 0x630; // -.\uE399
			public const uint AIData = 0x640; // -.IAIData
			public const uint _healthController = 0x660; // EFT.HealthSystem.IHealthController
			public const uint _inventoryController = 0x678; // -.Player.PlayerInventoryController
			public const uint _handsController = 0x680; // -.Player.AbstractHandsController
			public const uint IsYourPlayer = 0xA36; // Boolean
		}

		public readonly partial struct ObservedPlayerView
		{
			public const uint GroupID = 0x20; // String
			public const uint NickName = 0x50; // String
			public const uint AccountId = 0x58; // String
			public const uint PlayerBody = 0x68; // EFT.PlayerBody
			public const uint ObservedPlayerController = 0x88; // -.\uED46
			public const uint Voice = 0x98; // String
			public const uint Side = 0x100; // System.Int32
			public const uint IsAI = 0x111; // Boolean
		}

		public readonly partial struct ObservedPlayerController
		{
			public const uint Player = 0x10; // EFT.NextObservedPlayer.ObservedPlayerView
			public static readonly uint[] MovementController = new uint[] { 0xC8, 0x10 }; // -.\uED66, -.\uED68
			public const uint HandsController = 0xD8; // -.\uED50
			public const uint InfoContainer = 0xE8; // -.\uED59
			public const uint HealthController = 0xF0; // -.\uE446
			public const uint InventoryController = 0x118; // -.\uED5B
		}

		public readonly partial struct ObservedMovementController
		{
			public const uint Rotation = 0x90; // UnityEngine.Vector2
			public const uint Velocity = 0x114; // UnityEngine.Vector3
		}

		public readonly partial struct ObservedHandsController
		{
			public const uint ItemInHands = 0x58; // EFT.InventoryLogic.Item
		}

		public readonly partial struct ObservedHealthController
		{
			public const uint Player = 0x10; // EFT.NextObservedPlayer.ObservedPlayerView
			public const uint PlayerCorpse = 0x18; // EFT.Interactive.ObservedCorpse
			public const uint HealthStatus = 0xD8; // System.Int32
		}

		public readonly partial struct ProceduralWeaponAnimation
		{
			public const uint _optics = 0xC8; // System.Collections.Generic.List<SightNBone>
			public const uint _isAiming = 0x1C5; // Boolean
		}

		public readonly partial struct SightNBone
		{
			public const uint Mod = 0x10; // EFT.InventoryLogic.SightComponent
		}

		public readonly partial struct Profile
		{
			public const uint Id = 0x10; // String
			public const uint AccountId = 0x18; // String
			public const uint Info = 0x40; // -.\uE9AD
			public const uint QuestsData = 0x88; // System.Collections.Generic.List<\uF286>
			public const uint WishlistManager = 0xD0; // -.\uE8D9
		}

		public readonly partial struct WishlistManager
		{
			public const uint Items = 0x28; // System.Collections.Generic.Dictionary<MongoID, Int32>
		}

		public readonly partial struct PlayerInfo
		{
			public const uint EntryPoint = 0x18; // String
			public const uint GroupId = 0x28; // String
			public const uint Side = 0x88; // [HUMAN] Int32
			public const uint RegistrationDate = 0x8C; // Int32
		}

		public readonly partial struct QuestData
		{
			public const uint Id = 0x10; // String
			public const uint CompletedConditions = 0x20; // System.Collections.Generic.HashSet<MongoID>
			public const uint Template = 0x28; // -.\uF287
			public const uint Status = 0x34; // System.Int32
		}

		public readonly partial struct QuestTemplate
		{
			public const uint Conditions = 0x40; // EFT.Quests.ConditionsDict
			public const uint Name = 0x70; // String
		}

		public readonly partial struct QuestConditionsContainer
		{
			public const uint ConditionsList = 0x50; // System.Collections.Generic.List<Var>
		}

		public readonly partial struct QuestCondition
		{
			public const uint id = 0x10; // EFT.MongoID
		}

		public readonly partial struct QuestConditionItem
		{
			public const uint value = 0x58; // Single
		}

		public readonly partial struct QuestConditionFindItem
		{
			public const uint target = 0x70; // System.String[]
		}

		public readonly partial struct QuestConditionCounterCreator
		{
			public const uint Conditions = 0x78; // -.\uF267
		}

		public readonly partial struct QuestConditionVisitPlace
		{
			public const uint target = 0x70; // String
		}

		public readonly partial struct QuestConditionPlaceBeacon
		{
			public const uint zoneId = 0x78; // String
		}

		public readonly partial struct ItemHandsController
		{
			public const uint Item = 0x68; // EFT.InventoryLogic.Item
		}

		public readonly partial struct MovementContext
		{
			public const uint Player = 0x10; // EFT.Player
			public const uint _rotation = 0x27C; // UnityEngine.Vector2
		}

		public readonly partial struct InventoryController
		{
			public const uint Inventory = 0x120; // EFT.InventoryLogic.Inventory
		}

		public readonly partial struct Inventory
		{
			public const uint Equipment = 0x10; // EFT.InventoryLogic.InventoryEquipment
		}

		public readonly partial struct Equipment
		{
			public const uint Grids = 0x78; // -.\uEE74[]
			public const uint Slots = 0x80; // EFT.InventoryLogic.Slot[]
		}

		public readonly partial struct Grids
		{
			public const uint ContainedItems = 0x30; // -.\uEE76
		}

		public readonly partial struct GridContainedItems
		{
			public const uint Items = 0x18; // System.Collections.Generic.List<Item>
		}

		public readonly partial struct Slot
		{
			public const uint ContainedItem = 0x38; // EFT.InventoryLogic.Item
			public const uint ID = 0x48; // String
			public const uint Required = 0x60; // Boolean
		}

		public readonly partial struct InteractiveLootItem
		{
			public const uint Item = 0xB8; // EFT.InventoryLogic.Item
		}

		public readonly partial struct DizSkinningSkeleton
		{
			public const uint _values = 0x30; // System.Collections.Generic.List<Transform>
		}

		public readonly partial struct LootableContainer
		{
			public const uint InteractingPlayer = 0xC0; // EFT.IPlayer
			public const uint ItemOwner = 0x148; // -.\uEFB4
		}

		public readonly partial struct LootableContainerItemOwner
		{
			public const uint RootItem = 0xB8; // EFT.InventoryLogic.Item
		}

		public readonly partial struct LootItem
		{
			public const uint Template = 0x40; // EFT.InventoryLogic.ItemTemplate
		}

		public readonly partial struct LootItemMod
		{
			public const uint Grids = 0x78; // -.\uEE74[]
			public const uint Slots = 0x80; // EFT.InventoryLogic.Slot[]
		}

		public readonly partial struct LootItemWeapon
		{
			public const uint Chambers = 0xB0; // EFT.InventoryLogic.Slot[]
		}

		public readonly partial struct ItemTemplate
		{
			public const uint ShortName = 0x18; // String
			public const uint _id = 0x50; // EFT.MongoID
			public const uint QuestItem = 0xBC; // Boolean
		}

		public readonly partial struct PlayerBody
		{
			public const uint SkeletonRootJoint = 0x30; // Diz.Skinning.Skeleton
		}

		public readonly partial struct OpticCameraManagerContainer
		{
			public const uint Instance = 0x0; // -.\uF124
			public const uint OpticCameraManager = 0x10; // -.\uF125
			public const uint FPSCamera = 0x60; // UnityEngine.Camera
		}

		public readonly partial struct OpticCameraManager
		{
			public const uint Camera = 0x68; // UnityEngine.Camera
			public const uint CurrentOpticSight = 0x70; // EFT.CameraControl.OpticSight
		}

		public readonly partial struct SightComponent
		{
			public const uint _template = 0x20; // -.\uEE6C
			public const uint ScopesSelectedModes = 0x30; // System.Int32[]
			public const uint SelectedScope = 0x38; // Int32
			public const uint ScopeZoomValue = 0x3C; // Single
		}

		public readonly partial struct SightInterface
		{
			public const uint Zooms = 0x198; // System.Single[]
		}
	}

	public readonly partial struct Enums
	{
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
