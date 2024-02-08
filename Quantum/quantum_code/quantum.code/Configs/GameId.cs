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
					GameId.Any, new List<GameIdGroup>
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
					GameId.BlastBuck, new List<GameIdGroup>
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
					GameId.Fragments, new List<GameIdGroup>
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
					GameId.FtueMiniMap, new List<GameIdGroup>
					{
						GameIdGroup.Map
					}.AsReadOnly()
				},
				{
					GameId.District, new List<GameIdGroup>
					{
						GameIdGroup.Map
					}.AsReadOnly()
				},
				{
					GameId.TestAssetsMap, new List<GameIdGroup>
					{
						GameIdGroup.Map
					}.AsReadOnly()
				},
				{
					GameId.BattlelandsMap, new List<GameIdGroup>
					{
						GameIdGroup.Map
					}.AsReadOnly()
				},
				{
					GameId.IslandsMap, new List<GameIdGroup>
					{
						GameIdGroup.Map
					}.AsReadOnly()
				},
				{
					GameId.MazeMayhem, new List<GameIdGroup>
					{
						GameIdGroup.Map
					}.AsReadOnly()
				},
				{
					GameId.Cemetery, new List<GameIdGroup>
					{
						GameIdGroup.Map
					}.AsReadOnly()
				},
				{
					GameId.Fortress, new List<GameIdGroup>
					{
						GameIdGroup.Map
					}.AsReadOnly()
				},
				{
					GameId.IslandOne, new List<GameIdGroup>
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
						GameIdGroup.Equipment,
						GameIdGroup.Deprecated
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
						GameIdGroup.Equipment,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.ApoSniper, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.ApoRPG, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.ApoMinigun, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.ModPistol, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Simple
					}.AsReadOnly()
				},
				{
					GameId.ModShotgun, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.ModMachineGun, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.ModRifle, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.ModSniper, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.ModLauncher, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.ModHeavyMachineGun, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.SciPistol, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.SciBlaster, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.SciNeedleGun, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.SciRifle, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.SciSniper, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.SciCannon, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Simple
					}.AsReadOnly()
				},
				{
					GameId.SciMelter, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Deprecated
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
					}.AsReadOnly()
				},
				{
					GameId.ShieldCapacitySmall, new List<GameIdGroup>
					{
					}.AsReadOnly()
				},
				{
					GameId.ShieldCapacityLarge, new List<GameIdGroup>
					{
					}.AsReadOnly()
				},
				{
					GameId.EnergyCubeSmall, new List<GameIdGroup>
					{
					}.AsReadOnly()
				},
				{
					GameId.EnergyCubeLarge, new List<GameIdGroup>
					{
						GameIdGroup.Consumable,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.ChestCommon, new List<GameIdGroup>
					{
					}.AsReadOnly()
				},
				{
					GameId.ChestUncommon, new List<GameIdGroup>
					{
					}.AsReadOnly()
				},
				{
					GameId.ChestRare, new List<GameIdGroup>
					{
					}.AsReadOnly()
				},
				{
					GameId.ChestEpic, new List<GameIdGroup>
					{
					}.AsReadOnly()
				},
				{
					GameId.ChestConsumable, new List<GameIdGroup>
					{
						GameIdGroup.Chest,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.ChestEquipment, new List<GameIdGroup>
					{
						GameIdGroup.Chest,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.ChestEquipmentTutorial, new List<GameIdGroup>
					{
						GameIdGroup.Chest,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.ChestWeapon, new List<GameIdGroup>
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
					GameId.SpecialLandmine, new List<GameIdGroup>
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
					GameId.SkipTutorial, new List<GameIdGroup>
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
						GameIdGroup.IAP,
						GameIdGroup.Chest
					}.AsReadOnly()
				},
				{
					GameId.CoreEpic, new List<GameIdGroup>
					{
						GameIdGroup.Core,
						GameIdGroup.IAP,
						GameIdGroup.Chest
					}.AsReadOnly()
				},
				{
					GameId.CoreLegendary, new List<GameIdGroup>
					{
						GameIdGroup.Core,
						GameIdGroup.IAP,
						GameIdGroup.Chest
					}.AsReadOnly()
				},
				{
					GameId.Male01Avatar, new List<GameIdGroup>
					{
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.Male02Avatar, new List<GameIdGroup>
					{
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.Female01Avatar, new List<GameIdGroup>
					{
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.Female02Avatar, new List<GameIdGroup>
					{
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.MaleAssassin, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.BotItem,
						GameIdGroup.Assassin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MaleCorpos, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Corpo,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MalePunk, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.BotItem,
						GameIdGroup.Punk,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MaleSuperstar, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.BotItem,
						GameIdGroup.Superstar,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.FemaleAssassin, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.BotItem,
						GameIdGroup.Assassin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.FemaleCorpos, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Corpo,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.FemalePunk, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.BotItem,
						GameIdGroup.Punk,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.FemaleSuperstar, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.BotItem,
						GameIdGroup.Superstar,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.TestSkin, new List<GameIdGroup>
					{
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinDragonBoxer, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinTieGuy, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinFitnessChick, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinSkellyQueen, new List<GameIdGroup>
					{
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinXmasSuperstar, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.BotItem,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinJodie, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.BotItem,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinMontyVonCue, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.BotItem,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinBoudicca, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.BotItem,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinCupid, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.BotItem,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinPanda, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinLeprechaun, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinDragon, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinSnowboarder, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinDunePaul, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinHarald, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.Divinci, new List<GameIdGroup>
					{
						GameIdGroup.Glider,
						GameIdGroup.BotItem,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.Falcon, new List<GameIdGroup>
					{
						GameIdGroup.Glider,
						GameIdGroup.BotItem,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.Rocket, new List<GameIdGroup>
					{
						GameIdGroup.Glider,
						GameIdGroup.BotItem,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.Turbine, new List<GameIdGroup>
					{
						GameIdGroup.Glider,
						GameIdGroup.BotItem,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.Tombstone, new List<GameIdGroup>
					{
						GameIdGroup.DeathMarker,
						GameIdGroup.BotItem,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.Demon, new List<GameIdGroup>
					{
						GameIdGroup.DeathMarker,
						GameIdGroup.BotItem,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.Superstar, new List<GameIdGroup>
					{
						GameIdGroup.DeathMarker,
						GameIdGroup.BotItem,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.Unicorn, new List<GameIdGroup>
					{
						GameIdGroup.DeathMarker,
						GameIdGroup.BotItem,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinDefault, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinSausage, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinCactus, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinAtomSlicer, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinDaggerOfDestiny, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinElectricSolo, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinGigaMelee, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinHatchet, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinMicDrop, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinMightySledge, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinOutOfThePark, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinPowerPan, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinPutter, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinSirQuacks, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinThunderAxe, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinToyMelee, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinTvTakedown, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinWheelOfPain, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinWrench, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinYouGotMail, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinXmas2023, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.Avatar1, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.Avatar2, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.Avatar3, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.Avatar4, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.Avatar5, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarRemote, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection,
						GameIdGroup.GenericCollectionItem
					}.AsReadOnly()
				},
				{
					GameId.AvatarNFTCollection, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection,
						GameIdGroup.GenericCollectionItem
					}.AsReadOnly()
				},
				{
					GameId.AvatarAssasinmask, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarBlastcatads, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarBurgerads, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarCatcupads, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarCorpoads, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarCorpocrossads, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarCorpomask, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarEyesads, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarFemaleassasinwantedads, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarFemaleassassinconcept, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarFemaleassassinwhatsticker, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarFemalecorpo, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarFemalecorpoconcept, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarFemalecorpophonesticker, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarFemalecorposticker, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarFemalehost, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarFemalepunk, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarFemalepunkconcept, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarFemalepunkfunsticker, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarFemalepunkgraffiti, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarFemalesuperstarads, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarFemalesuperstarconcept, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarFemalesuperstardisguststicker, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarFemalesupperstar, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarMaleassasin, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarMaleassasinconcept, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarMaleassasinexcitedsticker, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarMaleassasinwantedads, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarMalecorpoangryads, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarMalecorpoconcept, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarMalecorposcaredsticker, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarMalehost, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarMalepunk, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarMalepunkads, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarMalepunkconcept, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarMalepunkgraffiti, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarMalepunkhahasticker, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarMalesuperstarads, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarMalesuperstarconcept, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarMalesuperstarstopsticker, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarMusic, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarPunklogoads, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarRocketads, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarSuperstarloveads, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarUnicornssticker, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.FootprintDot, new List<GameIdGroup>
					{
						GameIdGroup.Footprint,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
			};

		private static readonly Dictionary<GameIdGroup, ReadOnlyCollection<GameId>> _ids =
			new Dictionary<GameIdGroup, ReadOnlyCollection<GameId>> (new GameIdGroupComparer())
			{
				{
					GameIdGroup.GameDesign, new List<GameId>
					{
						GameId.Random,
						GameId.Any
					}.AsReadOnly()
				},
				{
					GameIdGroup.Currency, new List<GameId>
					{
						GameId.RealMoney,
						GameId.COIN,
						GameId.BlastBuck,
						GameId.BLST,
						GameId.Fragments,
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
						GameId.NewBRMap,
						GameId.FtueMiniMap,
						GameId.District,
						GameId.TestAssetsMap,
						GameId.BattlelandsMap,
						GameId.IslandsMap,
						GameId.MazeMayhem,
						GameId.Cemetery,
						GameId.Fortress,
						GameId.IslandOne
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
						GameId.ModPistol,
						GameId.SciCannon,
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
					GameIdGroup.Deprecated, new List<GameId>
					{
						GameId.ApoCrossbow,
						GameId.ApoRifle,
						GameId.ApoSniper,
						GameId.ApoRPG,
						GameId.ApoMinigun,
						GameId.ModShotgun,
						GameId.ModMachineGun,
						GameId.ModRifle,
						GameId.ModLauncher,
						GameId.SciPistol,
						GameId.SciBlaster,
						GameId.SciNeedleGun,
						GameId.SciRifle,
						GameId.SciSniper,
						GameId.SciMelter,
						GameId.Male01Avatar,
						GameId.Male02Avatar,
						GameId.Female01Avatar,
						GameId.Female02Avatar
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
					GameIdGroup.Collection, new List<GameId>
					{
						GameId.MaleAssassin,
						GameId.MaleCorpos,
						GameId.MalePunk,
						GameId.MaleSuperstar,
						GameId.FemaleAssassin,
						GameId.FemaleCorpos,
						GameId.FemalePunk,
						GameId.FemaleSuperstar,
						GameId.PlayerSkinDragonBoxer,
						GameId.PlayerSkinTieGuy,
						GameId.PlayerSkinFitnessChick,
						GameId.PlayerSkinXmasSuperstar,
						GameId.PlayerSkinJodie,
						GameId.PlayerSkinMontyVonCue,
						GameId.PlayerSkinBoudicca,
						GameId.PlayerSkinCupid,
						GameId.PlayerSkinPanda,
						GameId.PlayerSkinLeprechaun,
						GameId.PlayerSkinDragon,
						GameId.PlayerSkinSnowboarder,
						GameId.PlayerSkinDunePaul,
						GameId.PlayerSkinHarald,
						GameId.Divinci,
						GameId.Falcon,
						GameId.Rocket,
						GameId.Turbine,
						GameId.Tombstone,
						GameId.Demon,
						GameId.Superstar,
						GameId.Unicorn,
						GameId.MeleeSkinDefault,
						GameId.MeleeSkinSausage,
						GameId.MeleeSkinCactus,
						GameId.MeleeSkinAtomSlicer,
						GameId.MeleeSkinDaggerOfDestiny,
						GameId.MeleeSkinElectricSolo,
						GameId.MeleeSkinGigaMelee,
						GameId.MeleeSkinHatchet,
						GameId.MeleeSkinMicDrop,
						GameId.MeleeSkinMightySledge,
						GameId.MeleeSkinOutOfThePark,
						GameId.MeleeSkinPowerPan,
						GameId.MeleeSkinPutter,
						GameId.MeleeSkinSirQuacks,
						GameId.MeleeSkinThunderAxe,
						GameId.MeleeSkinToyMelee,
						GameId.MeleeSkinTvTakedown,
						GameId.MeleeSkinWheelOfPain,
						GameId.MeleeSkinWrench,
						GameId.MeleeSkinYouGotMail,
						GameId.MeleeSkinXmas2023,
						GameId.Avatar1,
						GameId.Avatar2,
						GameId.Avatar3,
						GameId.Avatar4,
						GameId.Avatar5,
						GameId.AvatarRemote,
						GameId.AvatarNFTCollection,
						GameId.AvatarAssasinmask,
						GameId.AvatarBlastcatads,
						GameId.AvatarBurgerads,
						GameId.AvatarCatcupads,
						GameId.AvatarCorpoads,
						GameId.AvatarCorpocrossads,
						GameId.AvatarCorpomask,
						GameId.AvatarEyesads,
						GameId.AvatarFemaleassasinwantedads,
						GameId.AvatarFemaleassassinconcept,
						GameId.AvatarFemaleassassinwhatsticker,
						GameId.AvatarFemalecorpo,
						GameId.AvatarFemalecorpoconcept,
						GameId.AvatarFemalecorpophonesticker,
						GameId.AvatarFemalecorposticker,
						GameId.AvatarFemalehost,
						GameId.AvatarFemalepunk,
						GameId.AvatarFemalepunkconcept,
						GameId.AvatarFemalepunkfunsticker,
						GameId.AvatarFemalepunkgraffiti,
						GameId.AvatarFemalesuperstarads,
						GameId.AvatarFemalesuperstarconcept,
						GameId.AvatarFemalesuperstardisguststicker,
						GameId.AvatarFemalesupperstar,
						GameId.AvatarMaleassasin,
						GameId.AvatarMaleassasinconcept,
						GameId.AvatarMaleassasinexcitedsticker,
						GameId.AvatarMaleassasinwantedads,
						GameId.AvatarMalecorpoangryads,
						GameId.AvatarMalecorpoconcept,
						GameId.AvatarMalecorposcaredsticker,
						GameId.AvatarMalehost,
						GameId.AvatarMalepunk,
						GameId.AvatarMalepunkads,
						GameId.AvatarMalepunkconcept,
						GameId.AvatarMalepunkgraffiti,
						GameId.AvatarMalepunkhahasticker,
						GameId.AvatarMalesuperstarads,
						GameId.AvatarMalesuperstarconcept,
						GameId.AvatarMalesuperstarstopsticker,
						GameId.AvatarMusic,
						GameId.AvatarPunklogoads,
						GameId.AvatarRocketads,
						GameId.AvatarSuperstarloveads,
						GameId.AvatarUnicornssticker
					}.AsReadOnly()
				},
				{
					GameIdGroup.Assassin, new List<GameId>
					{
						GameId.MaleAssassin,
						GameId.FemaleAssassin
					}.AsReadOnly()
				},
				{
					GameIdGroup.Corpo, new List<GameId>
					{
						GameId.MaleCorpos,
						GameId.FemaleCorpos
					}.AsReadOnly()
				},
				{
					GameIdGroup.BotItem, new List<GameId>
					{
						GameId.MaleAssassin,
						GameId.MalePunk,
						GameId.MaleSuperstar,
						GameId.FemaleAssassin,
						GameId.FemalePunk,
						GameId.FemaleSuperstar,
						GameId.PlayerSkinXmasSuperstar,
						GameId.PlayerSkinJodie,
						GameId.PlayerSkinMontyVonCue,
						GameId.PlayerSkinBoudicca,
						GameId.PlayerSkinCupid,
						GameId.Divinci,
						GameId.Falcon,
						GameId.Rocket,
						GameId.Turbine,
						GameId.Tombstone,
						GameId.Demon,
						GameId.Superstar,
						GameId.Unicorn,
						GameId.MeleeSkinDefault,
						GameId.MeleeSkinDaggerOfDestiny,
						GameId.MeleeSkinMicDrop,
						GameId.MeleeSkinPowerPan,
						GameId.MeleeSkinThunderAxe,
						GameId.MeleeSkinToyMelee,
						GameId.MeleeSkinWheelOfPain,
						GameId.MeleeSkinXmas2023,
						GameId.FootprintDot
					}.AsReadOnly()
				},
				{
					GameIdGroup.Punk, new List<GameId>
					{
						GameId.MalePunk,
						GameId.FemalePunk
					}.AsReadOnly()
				},
				{
					GameIdGroup.Superstar, new List<GameId>
					{
						GameId.MaleSuperstar,
						GameId.FemaleSuperstar
					}.AsReadOnly()
				},
				{
					GameIdGroup.Consumable, new List<GameId>
					{
						GameId.Rage,
						GameId.Health,
						GameId.AmmoSmall,
						GameId.ShieldSmall,
						GameId.EnergyCubeLarge
					}.AsReadOnly()
				},
				{
					GameIdGroup.Collectable, new List<GameId>
					{
						GameId.Rage,
						GameId.Health,
						GameId.AmmoSmall,
						GameId.ShieldSmall,
						GameId.EnergyCubeLarge,
						GameId.ChestConsumable,
						GameId.ChestEquipment,
						GameId.ChestEquipmentTutorial,
						GameId.ChestWeapon,
						GameId.ChestLegendary
					}.AsReadOnly()
				},
				{
					GameIdGroup.Ammo, new List<GameId>
					{
						GameId.AmmoSmall
					}.AsReadOnly()
				},
				{
					GameIdGroup.Chest, new List<GameId>
					{
						GameId.ChestConsumable,
						GameId.ChestEquipment,
						GameId.ChestEquipmentTutorial,
						GameId.ChestWeapon,
						GameId.ChestLegendary,
						GameId.CoreRare,
						GameId.CoreEpic,
						GameId.CoreLegendary
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
						GameId.SpecialLandmine,
						GameId.TutorialGrenade
					}.AsReadOnly()
				},
				{
					GameIdGroup.Destructible, new List<GameId>
					{
						GameId.Barrel,
						GameId.Barrier,
						GameId.SkipTutorial
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
				{
					GameIdGroup.GenericCollectionItem, new List<GameId>
					{
						GameId.AvatarRemote,
						GameId.AvatarNFTCollection
					}.AsReadOnly()
				},
				{
					GameIdGroup.MeleeSkin, new List<GameId>
					{
						GameId.MeleeSkinDefault,
						GameId.MeleeSkinSausage,
						GameId.MeleeSkinCactus,
						GameId.MeleeSkinAtomSlicer,
						GameId.MeleeSkinDaggerOfDestiny,
						GameId.MeleeSkinElectricSolo,
						GameId.MeleeSkinGigaMelee,
						GameId.MeleeSkinHatchet,
						GameId.MeleeSkinMicDrop,
						GameId.MeleeSkinMightySledge,
						GameId.MeleeSkinOutOfThePark,
						GameId.MeleeSkinPowerPan,
						GameId.MeleeSkinPutter,
						GameId.MeleeSkinSirQuacks,
						GameId.MeleeSkinThunderAxe,
						GameId.MeleeSkinToyMelee,
						GameId.MeleeSkinTvTakedown,
						GameId.MeleeSkinWheelOfPain,
						GameId.MeleeSkinWrench,
						GameId.MeleeSkinYouGotMail,
						GameId.MeleeSkinXmas2023
					}.AsReadOnly()
				},
				{
					GameIdGroup.PlayerSkin, new List<GameId>
					{
						GameId.MaleAssassin,
						GameId.MaleCorpos,
						GameId.MalePunk,
						GameId.MaleSuperstar,
						GameId.FemaleAssassin,
						GameId.FemaleCorpos,
						GameId.FemalePunk,
						GameId.FemaleSuperstar,
						GameId.PlayerSkinDragonBoxer,
						GameId.PlayerSkinTieGuy,
						GameId.PlayerSkinFitnessChick,
						GameId.PlayerSkinXmasSuperstar,
						GameId.PlayerSkinJodie,
						GameId.PlayerSkinMontyVonCue,
						GameId.PlayerSkinBoudicca,
						GameId.PlayerSkinCupid,
						GameId.PlayerSkinPanda,
						GameId.PlayerSkinLeprechaun,
						GameId.PlayerSkinDragon,
						GameId.PlayerSkinSnowboarder,
						GameId.PlayerSkinDunePaul,
						GameId.PlayerSkinHarald
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
					GameIdGroup.DeathMarker, new List<GameId>
					{
						GameId.Tombstone,
						GameId.Demon,
						GameId.Superstar,
						GameId.Unicorn
					}.AsReadOnly()
				},
				{
					GameIdGroup.ProfilePicture, new List<GameId>
					{
						GameId.Avatar1,
						GameId.Avatar2,
						GameId.Avatar3,
						GameId.Avatar4,
						GameId.Avatar5,
						GameId.AvatarRemote,
						GameId.AvatarNFTCollection,
						GameId.AvatarAssasinmask,
						GameId.AvatarBlastcatads,
						GameId.AvatarBurgerads,
						GameId.AvatarCatcupads,
						GameId.AvatarCorpoads,
						GameId.AvatarCorpocrossads,
						GameId.AvatarCorpomask,
						GameId.AvatarEyesads,
						GameId.AvatarFemaleassasinwantedads,
						GameId.AvatarFemaleassassinconcept,
						GameId.AvatarFemaleassassinwhatsticker,
						GameId.AvatarFemalecorpo,
						GameId.AvatarFemalecorpoconcept,
						GameId.AvatarFemalecorpophonesticker,
						GameId.AvatarFemalecorposticker,
						GameId.AvatarFemalehost,
						GameId.AvatarFemalepunk,
						GameId.AvatarFemalepunkconcept,
						GameId.AvatarFemalepunkfunsticker,
						GameId.AvatarFemalepunkgraffiti,
						GameId.AvatarFemalesuperstarads,
						GameId.AvatarFemalesuperstarconcept,
						GameId.AvatarFemalesuperstardisguststicker,
						GameId.AvatarFemalesupperstar,
						GameId.AvatarMaleassasin,
						GameId.AvatarMaleassasinconcept,
						GameId.AvatarMaleassasinexcitedsticker,
						GameId.AvatarMaleassasinwantedads,
						GameId.AvatarMalecorpoangryads,
						GameId.AvatarMalecorpoconcept,
						GameId.AvatarMalecorposcaredsticker,
						GameId.AvatarMalehost,
						GameId.AvatarMalepunk,
						GameId.AvatarMalepunkads,
						GameId.AvatarMalepunkconcept,
						GameId.AvatarMalepunkgraffiti,
						GameId.AvatarMalepunkhahasticker,
						GameId.AvatarMalesuperstarads,
						GameId.AvatarMalesuperstarconcept,
						GameId.AvatarMalesuperstarstopsticker,
						GameId.AvatarMusic,
						GameId.AvatarPunklogoads,
						GameId.AvatarRocketads,
						GameId.AvatarSuperstarloveads,
						GameId.AvatarUnicornssticker
					}.AsReadOnly()
				},
				{
					GameIdGroup.Footprint, new List<GameId>
					{
						GameId.FootprintDot
					}.AsReadOnly()
				},
			};
	}
}
