using System.Collections.Generic;
using System.Collections.ObjectModel;

/* AUTO GENERATED CODE */
namespace Quantum
{

	public class GameIdComparer : IEqualityComparer<GameId>
	{
		public bool Equals(GameId x, GameId y)
		{
			return x == y;
		}

		public int GetHashCode(GameId obj)
		{
			return (int)obj;
		}
	}

	public class GameIdGroupComparer : IEqualityComparer<GameIdGroup>
	{
		public bool Equals(GameIdGroup x, GameIdGroup y)
		{
			return x == y;
		}

		public int GetHashCode(GameIdGroup obj)
		{
			return (int)obj;
		}
	}

	public static class GameIdLookup
	{
		public static bool IsInGroup(this GameId id, GameIdGroup group)
		{
			if (!_groups.TryGetValue(id, out var groups))
			{
				return false;
			}
			return groups.Contains(group);
		}

		public static IList<GameId> GetIds(this GameIdGroup group)
		{
			return _ids[group];
		}

		public static IList<GameIdGroup> GetGroups(this GameId id)
		{
			return _groups[id];
		}

		private static readonly Dictionary<GameId, ReadOnlyCollection<GameIdGroup>> _groups =
			new Dictionary<GameId, ReadOnlyCollection<GameIdGroup>> (new GameIdComparer())
			{
				{
					GameId.Random, new List<GameIdGroup>
					{
						GameIdGroup.GameDesign
					}.AsReadOnly()
				},
				{
					GameId.RealMoney, new List<GameIdGroup>
					{
						GameIdGroup.Currency
					}.AsReadOnly()
				},
				{
					GameId.COIN, new List<GameIdGroup>
					{
						GameIdGroup.Currency
					}.AsReadOnly()
				},
				{
					GameId.BLST, new List<GameIdGroup>
					{
						GameIdGroup.Currency
					}.AsReadOnly()
				},
				{
					GameId.CS, new List<GameIdGroup>
					{
						GameIdGroup.Currency,
						GameIdGroup.ResourcePool
					}.AsReadOnly()
				},
				{
					GameId.XP, new List<GameIdGroup>
					{
						GameIdGroup.Resource
					}.AsReadOnly()
				},
				{
					GameId.Trophies, new List<GameIdGroup>
					{
						GameIdGroup.Resource
					}.AsReadOnly()
				},
				{
					GameId.BPP, new List<GameIdGroup>
					{
						GameIdGroup.Resource,
						GameIdGroup.ResourcePool
					}.AsReadOnly()
				},
				{
					GameId.FloodCity, new List<GameIdGroup>
					{
						GameIdGroup.Map
					}.AsReadOnly()
				},
				{
					GameId.MainDeck, new List<GameIdGroup>
					{
						GameIdGroup.Map
					}.AsReadOnly()
				},
				{
					GameId.FtueDeck, new List<GameIdGroup>
					{
						GameIdGroup.Map
					}.AsReadOnly()
				},
				{
					GameId.SmallWilderness, new List<GameIdGroup>
					{
						GameIdGroup.Map
					}.AsReadOnly()
				},
				{
					GameId.FloodCitySimple, new List<GameIdGroup>
					{
						GameIdGroup.Map
					}.AsReadOnly()
				},
				{
					GameId.BlimpDeck, new List<GameIdGroup>
					{
						GameIdGroup.Map
					}.AsReadOnly()
				},
				{
					GameId.BRGenesis, new List<GameIdGroup>
					{
						GameIdGroup.Map
					}.AsReadOnly()
				},
				{
					GameId.MapTestScene, new List<GameIdGroup>
					{
						GameIdGroup.Map
					}.AsReadOnly()
				},
				{
					GameId.TestScene, new List<GameIdGroup>
					{
						GameIdGroup.Map
					}.AsReadOnly()
				},
				{
					GameId.NewBRMap, new List<GameIdGroup>
					{
						GameIdGroup.Map
					}.AsReadOnly()
				},
				{
					GameId.MausHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Simple
					}.AsReadOnly()
				},
				{
					GameId.SoldierHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear
					}.AsReadOnly()
				},
				{
					GameId.RiotHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear
					}.AsReadOnly()
				},
				{
					GameId.WarriorHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear
					}.AsReadOnly()
				},
				{
					GameId.RoadHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Simple
					}.AsReadOnly()
				},
				{
					GameId.FootballHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear
					}.AsReadOnly()
				},
				{
					GameId.BaseballHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear
					}.AsReadOnly()
				},
				{
					GameId.HockeyHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear
					}.AsReadOnly()
				},
				{
					GameId.Hammer, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Melee
					}.AsReadOnly()
				},
				{
					GameId.ApoCrossbow, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.ApoShotgun, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.ApoSMG, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Simple
					}.AsReadOnly()
				},
				{
					GameId.ApoRifle, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.ApoSniper, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.ApoRPG, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Simple
					}.AsReadOnly()
				},
				{
					GameId.ApoMinigun, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.ModPistol, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.ModShotgun, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Simple
					}.AsReadOnly()
				},
				{
					GameId.ModMachineGun, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.ModRifle, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.ModSniper, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Simple
					}.AsReadOnly()
				},
				{
					GameId.ModLauncher, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.ModHeavyMachineGun, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Simple
					}.AsReadOnly()
				},
				{
					GameId.SciPistol, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Simple
					}.AsReadOnly()
				},
				{
					GameId.SciBlaster, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.SciNeedleGun, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.SciRifle, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Simple
					}.AsReadOnly()
				},
				{
					GameId.SciSniper, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.SciCannon, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.SciMelter, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.MouseAmulet, new List<GameIdGroup>
					{
						GameIdGroup.Amulet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Simple
					}.AsReadOnly()
				},
				{
					GameId.RiotAmulet, new List<GameIdGroup>
					{
						GameIdGroup.Amulet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear
					}.AsReadOnly()
				},
				{
					GameId.SoldierAmulet, new List<GameIdGroup>
					{
						GameIdGroup.Amulet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear
					}.AsReadOnly()
				},
				{
					GameId.WarriorAmulet, new List<GameIdGroup>
					{
						GameIdGroup.Amulet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear
					}.AsReadOnly()
				},
				{
					GameId.TikTokAmulet, new List<GameIdGroup>
					{
						GameIdGroup.Amulet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Simple
					}.AsReadOnly()
				},
				{
					GameId.MouseArmor, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Simple
					}.AsReadOnly()
				},
				{
					GameId.RiotArmor, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment,
						GameIdGroup.Gear
					}.AsReadOnly()
				},
				{
					GameId.SoldierArmor, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment,
						GameIdGroup.Gear
					}.AsReadOnly()
				},
				{
					GameId.WarriorArmor, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment,
						GameIdGroup.Gear
					}.AsReadOnly()
				},
				{
					GameId.RoadSignArmour, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Simple
					}.AsReadOnly()
				},
				{
					GameId.BaseballArmor, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment,
						GameIdGroup.Gear
					}.AsReadOnly()
				},
				{
					GameId.FootballArmor, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment,
						GameIdGroup.Gear
					}.AsReadOnly()
				},
				{
					GameId.MouseShield, new List<GameIdGroup>
					{
						GameIdGroup.Shield,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Simple
					}.AsReadOnly()
				},
				{
					GameId.SoldierShield, new List<GameIdGroup>
					{
						GameIdGroup.Shield,
						GameIdGroup.Equipment,
						GameIdGroup.Gear
					}.AsReadOnly()
				},
				{
					GameId.WarriorShield, new List<GameIdGroup>
					{
						GameIdGroup.Shield,
						GameIdGroup.Equipment,
						GameIdGroup.Gear
					}.AsReadOnly()
				},
				{
					GameId.RiotShield, new List<GameIdGroup>
					{
						GameIdGroup.Shield,
						GameIdGroup.Equipment,
						GameIdGroup.Gear
					}.AsReadOnly()
				},
				{
					GameId.RoadShield, new List<GameIdGroup>
					{
						GameIdGroup.Shield,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Simple
					}.AsReadOnly()
				},
				{
					GameId.Male01Avatar, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin
					}.AsReadOnly()
				},
				{
					GameId.Male02Avatar, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.Female01Avatar, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin
					}.AsReadOnly()
				},
				{
					GameId.Female02Avatar, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.MaleAssassin, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin
					}.AsReadOnly()
				},
				{
					GameId.MaleCorpos, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin
					}.AsReadOnly()
				},
				{
					GameId.MalePunk, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin
					}.AsReadOnly()
				},
				{
					GameId.MaleSuperstar, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin
					}.AsReadOnly()
				},
				{
					GameId.FemaleAssassin, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin
					}.AsReadOnly()
				},
				{
					GameId.FemaleCorpos, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin
					}.AsReadOnly()
				},
				{
					GameId.FemalePunk, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin
					}.AsReadOnly()
				},
				{
					GameId.FemaleSuperstar, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin
					}.AsReadOnly()
				},
				{
					GameId.Divinci, new List<GameIdGroup>
					{
						GameIdGroup.Glider,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.Falcon, new List<GameIdGroup>
					{
						GameIdGroup.Glider
					}.AsReadOnly()
				},
				{
					GameId.Rocket, new List<GameIdGroup>
					{
						GameIdGroup.Glider
					}.AsReadOnly()
				},
				{
					GameId.Turbine, new List<GameIdGroup>
					{
						GameIdGroup.Glider
					}.AsReadOnly()
				},
				{
					GameId.Rage, new List<GameIdGroup>
					{
						GameIdGroup.Consumable,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.Health, new List<GameIdGroup>
					{
						GameIdGroup.Consumable,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.AmmoSmall, new List<GameIdGroup>
					{
						GameIdGroup.Consumable,
						GameIdGroup.Collectable,
						GameIdGroup.Ammo
					}.AsReadOnly()
				},
				{
					GameId.AmmoLarge, new List<GameIdGroup>
					{
						GameIdGroup.Consumable,
						GameIdGroup.Collectable,
						GameIdGroup.Ammo
					}.AsReadOnly()
				},
				{
					GameId.ShieldSmall, new List<GameIdGroup>
					{
						GameIdGroup.Consumable,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.ShieldLarge, new List<GameIdGroup>
					{
						GameIdGroup.Consumable,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.ShieldCapacitySmall, new List<GameIdGroup>
					{
						GameIdGroup.Consumable,
						GameIdGroup.Collectable,
						GameIdGroup.ShieldCapacity
					}.AsReadOnly()
				},
				{
					GameId.ShieldCapacityLarge, new List<GameIdGroup>
					{
						GameIdGroup.Consumable,
						GameIdGroup.Collectable,
						GameIdGroup.ShieldCapacity
					}.AsReadOnly()
				},
				{
					GameId.ChestCommon, new List<GameIdGroup>
					{
						GameIdGroup.Chest,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.ChestUncommon, new List<GameIdGroup>
					{
						GameIdGroup.Chest,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.ChestRare, new List<GameIdGroup>
					{
						GameIdGroup.Chest,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.ChestEpic, new List<GameIdGroup>
					{
						GameIdGroup.Chest,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.ChestLegendary, new List<GameIdGroup>
					{
						GameIdGroup.Chest,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.SpecialAimingAirstrike, new List<GameIdGroup>
					{
						GameIdGroup.Special
					}.AsReadOnly()
				},
				{
					GameId.SpecialAimingStunGrenade, new List<GameIdGroup>
					{
						GameIdGroup.Special
					}.AsReadOnly()
				},
				{
					GameId.SpecialShieldSelf, new List<GameIdGroup>
					{
						GameIdGroup.Special
					}.AsReadOnly()
				},
				{
					GameId.SpecialSkyLaserBeam, new List<GameIdGroup>
					{
						GameIdGroup.Special
					}.AsReadOnly()
				},
				{
					GameId.SpecialShieldedCharge, new List<GameIdGroup>
					{
						GameIdGroup.Special
					}.AsReadOnly()
				},
				{
					GameId.SpecialAimingGrenade, new List<GameIdGroup>
					{
						GameIdGroup.Special
					}.AsReadOnly()
				},
				{
					GameId.SpecialDefaultDash, new List<GameIdGroup>
					{
						GameIdGroup.Special
					}.AsReadOnly()
				},
				{
					GameId.SpecialRadar, new List<GameIdGroup>
					{
						GameIdGroup.Special
					}.AsReadOnly()
				},
				{
					GameId.TutorialGrenade, new List<GameIdGroup>
					{
						GameIdGroup.Special
					}.AsReadOnly()
				},
				{
					GameId.Barrel, new List<GameIdGroup>
					{
						GameIdGroup.Destructible
					}.AsReadOnly()
				},
				{
					GameId.Barrier, new List<GameIdGroup>
					{
						GameIdGroup.Destructible
					}.AsReadOnly()
				},
				{
					GameId.DummyCharacter, new List<GameIdGroup>
					{
						GameIdGroup.DummyCharacter
					}.AsReadOnly()
				},
				{
					GameId.WeaponPlatformSpawner, new List<GameIdGroup>
					{
						GameIdGroup.Platform
					}.AsReadOnly()
				},
				{
					GameId.ConsumablePlatformSpawner, new List<GameIdGroup>
					{
						GameIdGroup.Platform
					}.AsReadOnly()
				},
				{
					GameId.Tombstone, new List<GameIdGroup>
					{
						GameIdGroup.DeathMarker,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.Demon, new List<GameIdGroup>
					{
						GameIdGroup.DeathMarker
					}.AsReadOnly()
				},
				{
					GameId.SuperStar, new List<GameIdGroup>
					{
						GameIdGroup.DeathMarker
					}.AsReadOnly()
				},
				{
					GameId.Unicorn, new List<GameIdGroup>
					{
						GameIdGroup.DeathMarker
					}.AsReadOnly()
				},
				{
					GameId.CoreCommon, new List<GameIdGroup>
					{
						GameIdGroup.Core
					}.AsReadOnly()
				},
				{
					GameId.CoreUncommon, new List<GameIdGroup>
					{
						GameIdGroup.Core
					}.AsReadOnly()
				},
				{
					GameId.CoreRare, new List<GameIdGroup>
					{
						GameIdGroup.Core,
						GameIdGroup.IAP
					}.AsReadOnly()
				},
				{
					GameId.CoreEpic, new List<GameIdGroup>
					{
						GameIdGroup.Core,
						GameIdGroup.IAP
					}.AsReadOnly()
				},
				{
					GameId.CoreLegendary, new List<GameIdGroup>
					{
						GameIdGroup.Core,
						GameIdGroup.IAP
					}.AsReadOnly()
				},
			};

		private static readonly Dictionary<GameIdGroup, ReadOnlyCollection<GameId>> _ids =
			new Dictionary<GameIdGroup, ReadOnlyCollection<GameId>> (new GameIdGroupComparer())
			{
				{
					GameIdGroup.GameDesign, new List<GameId>
					{
						GameId.Random
					}.AsReadOnly()
				},
				{
					GameIdGroup.Currency, new List<GameId>
					{
						GameId.RealMoney,
						GameId.COIN,
						GameId.BLST,
						GameId.CS
					}.AsReadOnly()
				},
				{
					GameIdGroup.ResourcePool, new List<GameId>
					{
						GameId.CS,
						GameId.BPP
					}.AsReadOnly()
				},
				{
					GameIdGroup.Resource, new List<GameId>
					{
						GameId.XP,
						GameId.Trophies,
						GameId.BPP
					}.AsReadOnly()
				},
				{
					GameIdGroup.Map, new List<GameId>
					{
						GameId.FloodCity,
						GameId.MainDeck,
						GameId.FtueDeck,
						GameId.SmallWilderness,
						GameId.FloodCitySimple,
						GameId.BlimpDeck,
						GameId.BRGenesis,
						GameId.MapTestScene,
						GameId.TestScene,
						GameId.NewBRMap
					}.AsReadOnly()
				},
				{
					GameIdGroup.Helmet, new List<GameId>
					{
						GameId.MausHelmet,
						GameId.SoldierHelmet,
						GameId.RiotHelmet,
						GameId.WarriorHelmet,
						GameId.RoadHelmet,
						GameId.FootballHelmet,
						GameId.BaseballHelmet,
						GameId.HockeyHelmet
					}.AsReadOnly()
				},
				{
					GameIdGroup.Equipment, new List<GameId>
					{
						GameId.MausHelmet,
						GameId.SoldierHelmet,
						GameId.RiotHelmet,
						GameId.WarriorHelmet,
						GameId.RoadHelmet,
						GameId.FootballHelmet,
						GameId.BaseballHelmet,
						GameId.HockeyHelmet,
						GameId.Hammer,
						GameId.ApoCrossbow,
						GameId.ApoShotgun,
						GameId.ApoSMG,
						GameId.ApoRifle,
						GameId.ApoSniper,
						GameId.ApoRPG,
						GameId.ApoMinigun,
						GameId.ModPistol,
						GameId.ModShotgun,
						GameId.ModMachineGun,
						GameId.ModRifle,
						GameId.ModSniper,
						GameId.ModLauncher,
						GameId.ModHeavyMachineGun,
						GameId.SciPistol,
						GameId.SciBlaster,
						GameId.SciNeedleGun,
						GameId.SciRifle,
						GameId.SciSniper,
						GameId.SciCannon,
						GameId.SciMelter,
						GameId.MouseAmulet,
						GameId.RiotAmulet,
						GameId.SoldierAmulet,
						GameId.WarriorAmulet,
						GameId.TikTokAmulet,
						GameId.MouseArmor,
						GameId.RiotArmor,
						GameId.SoldierArmor,
						GameId.WarriorArmor,
						GameId.RoadSignArmour,
						GameId.BaseballArmor,
						GameId.FootballArmor,
						GameId.MouseShield,
						GameId.SoldierShield,
						GameId.WarriorShield,
						GameId.RiotShield,
						GameId.RoadShield
					}.AsReadOnly()
				},
				{
					GameIdGroup.Gear, new List<GameId>
					{
						GameId.MausHelmet,
						GameId.SoldierHelmet,
						GameId.RiotHelmet,
						GameId.WarriorHelmet,
						GameId.RoadHelmet,
						GameId.FootballHelmet,
						GameId.BaseballHelmet,
						GameId.HockeyHelmet,
						GameId.MouseAmulet,
						GameId.RiotAmulet,
						GameId.SoldierAmulet,
						GameId.WarriorAmulet,
						GameId.TikTokAmulet,
						GameId.MouseArmor,
						GameId.RiotArmor,
						GameId.SoldierArmor,
						GameId.WarriorArmor,
						GameId.RoadSignArmour,
						GameId.BaseballArmor,
						GameId.FootballArmor,
						GameId.MouseShield,
						GameId.SoldierShield,
						GameId.WarriorShield,
						GameId.RiotShield,
						GameId.RoadShield
					}.AsReadOnly()
				},
				{
					GameIdGroup.Simple, new List<GameId>
					{
						GameId.MausHelmet,
						GameId.RoadHelmet,
						GameId.ApoSMG,
						GameId.ApoRPG,
						GameId.ModShotgun,
						GameId.ModSniper,
						GameId.ModHeavyMachineGun,
						GameId.SciPistol,
						GameId.SciRifle,
						GameId.MouseAmulet,
						GameId.TikTokAmulet,
						GameId.MouseArmor,
						GameId.RoadSignArmour,
						GameId.MouseShield,
						GameId.RoadShield
					}.AsReadOnly()
				},
				{
					GameIdGroup.Weapon, new List<GameId>
					{
						GameId.Hammer,
						GameId.ApoCrossbow,
						GameId.ApoShotgun,
						GameId.ApoSMG,
						GameId.ApoRifle,
						GameId.ApoSniper,
						GameId.ApoRPG,
						GameId.ApoMinigun,
						GameId.ModPistol,
						GameId.ModShotgun,
						GameId.ModMachineGun,
						GameId.ModRifle,
						GameId.ModSniper,
						GameId.ModLauncher,
						GameId.ModHeavyMachineGun,
						GameId.SciPistol,
						GameId.SciBlaster,
						GameId.SciNeedleGun,
						GameId.SciRifle,
						GameId.SciSniper,
						GameId.SciCannon,
						GameId.SciMelter
					}.AsReadOnly()
				},
				{
					GameIdGroup.Melee, new List<GameId>
					{
						GameId.Hammer
					}.AsReadOnly()
				},
				{
					GameIdGroup.Amulet, new List<GameId>
					{
						GameId.MouseAmulet,
						GameId.RiotAmulet,
						GameId.SoldierAmulet,
						GameId.WarriorAmulet,
						GameId.TikTokAmulet
					}.AsReadOnly()
				},
				{
					GameIdGroup.Armor, new List<GameId>
					{
						GameId.MouseArmor,
						GameId.RiotArmor,
						GameId.SoldierArmor,
						GameId.WarriorArmor,
						GameId.RoadSignArmour,
						GameId.BaseballArmor,
						GameId.FootballArmor
					}.AsReadOnly()
				},
				{
					GameIdGroup.Shield, new List<GameId>
					{
						GameId.MouseShield,
						GameId.SoldierShield,
						GameId.WarriorShield,
						GameId.RiotShield,
						GameId.RoadShield
					}.AsReadOnly()
				},
				{
					GameIdGroup.PlayerSkin, new List<GameId>
					{
						GameId.Male01Avatar,
						GameId.Male02Avatar,
						GameId.Female01Avatar,
						GameId.Female02Avatar,
						GameId.MaleAssassin,
						GameId.MaleCorpos,
						GameId.MalePunk,
						GameId.MaleSuperstar,
						GameId.FemaleAssassin,
						GameId.FemaleCorpos,
						GameId.FemalePunk,
						GameId.FemaleSuperstar
					}.AsReadOnly()
				},
				{
					GameIdGroup.BotItem, new List<GameId>
					{
						GameId.Male02Avatar,
						GameId.Female02Avatar,
						GameId.Divinci,
						GameId.Tombstone
					}.AsReadOnly()
				},
				{
					GameIdGroup.Glider, new List<GameId>
					{
						GameId.Divinci,
						GameId.Falcon,
						GameId.Rocket,
						GameId.Turbine
					}.AsReadOnly()
				},
				{
					GameIdGroup.Consumable, new List<GameId>
					{
						GameId.Rage,
						GameId.Health,
						GameId.AmmoSmall,
						GameId.AmmoLarge,
						GameId.ShieldSmall,
						GameId.ShieldLarge,
						GameId.ShieldCapacitySmall,
						GameId.ShieldCapacityLarge
					}.AsReadOnly()
				},
				{
					GameIdGroup.Collectable, new List<GameId>
					{
						GameId.Rage,
						GameId.Health,
						GameId.AmmoSmall,
						GameId.AmmoLarge,
						GameId.ShieldSmall,
						GameId.ShieldLarge,
						GameId.ShieldCapacitySmall,
						GameId.ShieldCapacityLarge,
						GameId.ChestCommon,
						GameId.ChestUncommon,
						GameId.ChestRare,
						GameId.ChestEpic,
						GameId.ChestLegendary
					}.AsReadOnly()
				},
				{
					GameIdGroup.Ammo, new List<GameId>
					{
						GameId.AmmoSmall,
						GameId.AmmoLarge
					}.AsReadOnly()
				},
				{
					GameIdGroup.ShieldCapacity, new List<GameId>
					{
						GameId.ShieldCapacitySmall,
						GameId.ShieldCapacityLarge
					}.AsReadOnly()
				},
				{
					GameIdGroup.Chest, new List<GameId>
					{
						GameId.ChestCommon,
						GameId.ChestUncommon,
						GameId.ChestRare,
						GameId.ChestEpic,
						GameId.ChestLegendary
					}.AsReadOnly()
				},
				{
					GameIdGroup.Special, new List<GameId>
					{
						GameId.SpecialAimingAirstrike,
						GameId.SpecialAimingStunGrenade,
						GameId.SpecialShieldSelf,
						GameId.SpecialSkyLaserBeam,
						GameId.SpecialShieldedCharge,
						GameId.SpecialAimingGrenade,
						GameId.SpecialDefaultDash,
						GameId.SpecialRadar,
						GameId.TutorialGrenade
					}.AsReadOnly()
				},
				{
					GameIdGroup.Destructible, new List<GameId>
					{
						GameId.Barrel,
						GameId.Barrier
					}.AsReadOnly()
				},
				{
					GameIdGroup.DummyCharacter, new List<GameId>
					{
						GameId.DummyCharacter
					}.AsReadOnly()
				},
				{
					GameIdGroup.Platform, new List<GameId>
					{
						GameId.WeaponPlatformSpawner,
						GameId.ConsumablePlatformSpawner
					}.AsReadOnly()
				},
				{
					GameIdGroup.DeathMarker, new List<GameId>
					{
						GameId.Tombstone,
						GameId.Demon,
						GameId.SuperStar,
						GameId.Unicorn
					}.AsReadOnly()
				},
				{
					GameIdGroup.Core, new List<GameId>
					{
						GameId.CoreCommon,
						GameId.CoreUncommon,
						GameId.CoreRare,
						GameId.CoreEpic,
						GameId.CoreLegendary
					}.AsReadOnly()
				},
				{
					GameIdGroup.IAP, new List<GameId>
					{
						GameId.CoreRare,
						GameId.CoreEpic,
						GameId.CoreLegendary
					}.AsReadOnly()
				},
			};
	}
}
