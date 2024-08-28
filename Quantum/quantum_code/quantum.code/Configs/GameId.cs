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
						GameIdGroup.Currency,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.NOOB, new List<GameIdGroup>
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
						GameIdGroup.Map,
						GameIdGroup.Deprecated
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
						GameIdGroup.Simple,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.SoldierHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.RiotHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.WarriorHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.RoadHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Simple,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.FootballHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.BaseballHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.HockeyHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Deprecated
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
						GameIdGroup.Equipment,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.ApoSMG, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment,
						GameIdGroup.Simple,
						GameIdGroup.Deprecated
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
						GameIdGroup.Equipment
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
						GameIdGroup.Equipment
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
						GameIdGroup.Equipment
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
						GameIdGroup.Deprecated
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
						GameIdGroup.Deprecated
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
						GameIdGroup.Simple,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.RiotAmulet, new List<GameIdGroup>
					{
						GameIdGroup.Amulet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.SoldierAmulet, new List<GameIdGroup>
					{
						GameIdGroup.Amulet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.WarriorAmulet, new List<GameIdGroup>
					{
						GameIdGroup.Amulet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.TikTokAmulet, new List<GameIdGroup>
					{
						GameIdGroup.Amulet,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Simple,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.MouseArmor, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Simple,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.RiotArmor, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.SoldierArmor, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.WarriorArmor, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.RoadSignArmour, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Simple,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.BaseballArmor, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.FootballArmor, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.MouseShield, new List<GameIdGroup>
					{
						GameIdGroup.Shield,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Simple,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.SoldierShield, new List<GameIdGroup>
					{
						GameIdGroup.Shield,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.WarriorShield, new List<GameIdGroup>
					{
						GameIdGroup.Shield,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.RiotShield, new List<GameIdGroup>
					{
						GameIdGroup.Shield,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.RoadShield, new List<GameIdGroup>
					{
						GameIdGroup.Shield,
						GameIdGroup.Equipment,
						GameIdGroup.Gear,
						GameIdGroup.Simple,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.Rage, new List<GameIdGroup>
					{
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
					GameId.ChestVitality, new List<GameIdGroup>
					{
						GameIdGroup.Chest,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.ChestAmmo, new List<GameIdGroup>
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
						GameIdGroup.Special,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.SpecialShieldSelf, new List<GameIdGroup>
					{
						GameIdGroup.Special,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.SpecialSkyLaserBeam, new List<GameIdGroup>
					{
						GameIdGroup.Special,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.SpecialShieldedCharge, new List<GameIdGroup>
					{
						GameIdGroup.Special,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.SpecialAimingGrenade, new List<GameIdGroup>
					{
						GameIdGroup.Special,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.SpecialDefaultDash, new List<GameIdGroup>
					{
						GameIdGroup.Special,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.SpecialRadar, new List<GameIdGroup>
					{
						GameIdGroup.Special,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.SpecialLandmine, new List<GameIdGroup>
					{
						GameIdGroup.Special,
						GameIdGroup.Collectable
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
						GameIdGroup.Core,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.CoreUncommon, new List<GameIdGroup>
					{
						GameIdGroup.Core,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.CoreRare, new List<GameIdGroup>
					{
						GameIdGroup.Core,
						GameIdGroup.IAP,
						GameIdGroup.Chest,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.CoreEpic, new List<GameIdGroup>
					{
						GameIdGroup.Core,
						GameIdGroup.IAP,
						GameIdGroup.Chest,
						GameIdGroup.Deprecated
					}.AsReadOnly()
				},
				{
					GameId.CoreLegendary, new List<GameIdGroup>
					{
						GameIdGroup.Core,
						GameIdGroup.IAP,
						GameIdGroup.Chest,
						GameIdGroup.Deprecated
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
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinTieGuy, new List<GameIdGroup>
					{
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinFitnessChick, new List<GameIdGroup>
					{
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
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinMontyVonCue, new List<GameIdGroup>
					{
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinBoudicca, new List<GameIdGroup>
					{
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
					GameId.PlayerSkinDragon, new List<GameIdGroup>
					{
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinSnowboarder, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.BotItem,
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
					GameId.PlayerSkinViking, new List<GameIdGroup>
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
					GameId.PlayerSkinEGirl, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinPoliceFemale, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinNinja, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinBrandFemale, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinBrandMale, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinGearedApe, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinPlagueDoctor, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinBurger, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinFootballGuy, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinLincoln, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinLion, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinSatoshi, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinSheriff, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinSoldier, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinSwimmer, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinThief, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinVR, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinWitch, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinHazmat, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinAura, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinMidas, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinNFL, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinPilot, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.PlayerSkinSkeleton, new List<GameIdGroup>
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
						GameIdGroup.Collection,
						GameIdGroup.BotItem
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
						GameIdGroup.Collection,
						GameIdGroup.BotItem
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
						GameIdGroup.Collection,
						GameIdGroup.BotItem
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
						GameIdGroup.Collection,
						GameIdGroup.BotItem
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
					GameId.MeleeSkinBaton, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinKatana, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinKeyboard, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinMagicalShillelagh, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinCrowbar, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinAxe, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinKnife, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinOar, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinSpatula, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinTrophy, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinWalkingStick, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinBroccoli, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinBone, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinBroom, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinLightsaber, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinScythe, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinSickle, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinPickaxe, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinLollipop, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.MeleeSkinDoctorStaff, new List<GameIdGroup>
					{
						GameIdGroup.MeleeSkin,
						GameIdGroup.Collection
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
					GameId.AvatarBrandfemale, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarBrandmale, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarEgirl, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarGearedape, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarLeprechaun, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarNinja, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarNinjaalternative, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarPolicefemale, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarPlaguedoctormystery, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarBurger, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarFootballguy, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarLion, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarLion2, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarSatoshi, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarSheriff, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarSoldier, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarSwimmer, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarThief, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarVr, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarBall, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarLincoln, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarMidasfull, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarHazmatfull, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarSkeletonfull, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarWitchhat, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarPumpkin, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarAuraarmed, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarNflarmed, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarPilotarmed, new List<GameIdGroup>
					{
						GameIdGroup.ProfilePicture,
						GameIdGroup.Collection
					}.AsReadOnly()
				},
				{
					GameId.AvatarWitch, new List<GameIdGroup>
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
						GameId.NOOB,
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
						GameId.Fragments,
						GameId.FloodCity,
						GameId.MausHelmet,
						GameId.SoldierHelmet,
						GameId.RiotHelmet,
						GameId.WarriorHelmet,
						GameId.RoadHelmet,
						GameId.FootballHelmet,
						GameId.BaseballHelmet,
						GameId.HockeyHelmet,
						GameId.ApoCrossbow,
						GameId.ApoShotgun,
						GameId.ApoSMG,
						GameId.ApoRifle,
						GameId.ApoSniper,
						GameId.ApoRPG,
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
						GameId.RoadShield,
						GameId.CoreCommon,
						GameId.CoreUncommon,
						GameId.CoreRare,
						GameId.CoreEpic,
						GameId.CoreLegendary,
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
						GameId.PlayerSkinXmasSuperstar,
						GameId.PlayerSkinCupid,
						GameId.PlayerSkinPanda,
						GameId.PlayerSkinSnowboarder,
						GameId.PlayerSkinDunePaul,
						GameId.PlayerSkinViking,
						GameId.PlayerSkinLeprechaun,
						GameId.PlayerSkinEGirl,
						GameId.PlayerSkinPoliceFemale,
						GameId.PlayerSkinNinja,
						GameId.PlayerSkinBrandFemale,
						GameId.PlayerSkinBrandMale,
						GameId.PlayerSkinGearedApe,
						GameId.PlayerSkinPlagueDoctor,
						GameId.PlayerSkinBurger,
						GameId.PlayerSkinFootballGuy,
						GameId.PlayerSkinLincoln,
						GameId.PlayerSkinLion,
						GameId.PlayerSkinSatoshi,
						GameId.PlayerSkinSheriff,
						GameId.PlayerSkinSoldier,
						GameId.PlayerSkinSwimmer,
						GameId.PlayerSkinThief,
						GameId.PlayerSkinVR,
						GameId.PlayerSkinWitch,
						GameId.PlayerSkinHazmat,
						GameId.PlayerSkinAura,
						GameId.PlayerSkinMidas,
						GameId.PlayerSkinNFL,
						GameId.PlayerSkinPilot,
						GameId.PlayerSkinSkeleton,
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
						GameId.MeleeSkinBaton,
						GameId.MeleeSkinKatana,
						GameId.MeleeSkinKeyboard,
						GameId.MeleeSkinMagicalShillelagh,
						GameId.MeleeSkinCrowbar,
						GameId.MeleeSkinAxe,
						GameId.MeleeSkinKnife,
						GameId.MeleeSkinOar,
						GameId.MeleeSkinSpatula,
						GameId.MeleeSkinTrophy,
						GameId.MeleeSkinWalkingStick,
						GameId.MeleeSkinBroccoli,
						GameId.MeleeSkinBone,
						GameId.MeleeSkinBroom,
						GameId.MeleeSkinLightsaber,
						GameId.MeleeSkinScythe,
						GameId.MeleeSkinSickle,
						GameId.MeleeSkinPickaxe,
						GameId.MeleeSkinLollipop,
						GameId.MeleeSkinDoctorStaff,
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
						GameId.AvatarUnicornssticker,
						GameId.AvatarBrandfemale,
						GameId.AvatarBrandmale,
						GameId.AvatarEgirl,
						GameId.AvatarGearedape,
						GameId.AvatarLeprechaun,
						GameId.AvatarNinja,
						GameId.AvatarNinjaalternative,
						GameId.AvatarPolicefemale,
						GameId.AvatarPlaguedoctormystery,
						GameId.AvatarBurger,
						GameId.AvatarFootballguy,
						GameId.AvatarLion,
						GameId.AvatarLion2,
						GameId.AvatarSatoshi,
						GameId.AvatarSheriff,
						GameId.AvatarSoldier,
						GameId.AvatarSwimmer,
						GameId.AvatarThief,
						GameId.AvatarVr,
						GameId.AvatarBall,
						GameId.AvatarLincoln,
						GameId.AvatarMidasfull,
						GameId.AvatarHazmatfull,
						GameId.AvatarSkeletonfull,
						GameId.AvatarWitchhat,
						GameId.AvatarPumpkin,
						GameId.AvatarAuraarmed,
						GameId.AvatarNflarmed,
						GameId.AvatarPilotarmed,
						GameId.AvatarWitch
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
						GameId.FemaleAssassin,
						GameId.PlayerSkinBrandFemale,
						GameId.PlayerSkinBrandMale,
						GameId.PlayerSkinEGirl,
						GameId.PlayerSkinSoldier,
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
						GameId.MeleeSkinElectricSolo,
						GameId.MeleeSkinMicDrop,
						GameId.MeleeSkinOutOfThePark,
						GameId.MeleeSkinPowerPan,
						GameId.MeleeSkinThunderAxe,
						GameId.MeleeSkinToyMelee,
						GameId.MeleeSkinTvTakedown,
						GameId.MeleeSkinWheelOfPain,
						GameId.MeleeSkinYouGotMail,
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
						GameId.Health,
						GameId.AmmoSmall,
						GameId.ShieldSmall
					}.AsReadOnly()
				},
				{
					GameIdGroup.Collectable, new List<GameId>
					{
						GameId.Health,
						GameId.AmmoSmall,
						GameId.ShieldSmall,
						GameId.ChestConsumable,
						GameId.ChestEquipment,
						GameId.ChestEquipmentTutorial,
						GameId.ChestWeapon,
						GameId.ChestLegendary,
						GameId.ChestVitality,
						GameId.ChestAmmo,
						GameId.SpecialAimingStunGrenade,
						GameId.SpecialShieldSelf,
						GameId.SpecialSkyLaserBeam,
						GameId.SpecialShieldedCharge,
						GameId.SpecialAimingGrenade,
						GameId.SpecialDefaultDash,
						GameId.SpecialRadar,
						GameId.SpecialLandmine
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
						GameId.ChestVitality,
						GameId.ChestAmmo,
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
						GameId.MeleeSkinXmas2023,
						GameId.MeleeSkinBaton,
						GameId.MeleeSkinKatana,
						GameId.MeleeSkinKeyboard,
						GameId.MeleeSkinMagicalShillelagh,
						GameId.MeleeSkinCrowbar,
						GameId.MeleeSkinAxe,
						GameId.MeleeSkinKnife,
						GameId.MeleeSkinOar,
						GameId.MeleeSkinSpatula,
						GameId.MeleeSkinTrophy,
						GameId.MeleeSkinWalkingStick,
						GameId.MeleeSkinBroccoli,
						GameId.MeleeSkinBone,
						GameId.MeleeSkinBroom,
						GameId.MeleeSkinLightsaber,
						GameId.MeleeSkinScythe,
						GameId.MeleeSkinSickle,
						GameId.MeleeSkinPickaxe,
						GameId.MeleeSkinLollipop,
						GameId.MeleeSkinDoctorStaff
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
						GameId.PlayerSkinXmasSuperstar,
						GameId.PlayerSkinCupid,
						GameId.PlayerSkinPanda,
						GameId.PlayerSkinSnowboarder,
						GameId.PlayerSkinDunePaul,
						GameId.PlayerSkinViking,
						GameId.PlayerSkinLeprechaun,
						GameId.PlayerSkinEGirl,
						GameId.PlayerSkinPoliceFemale,
						GameId.PlayerSkinNinja,
						GameId.PlayerSkinBrandFemale,
						GameId.PlayerSkinBrandMale,
						GameId.PlayerSkinGearedApe,
						GameId.PlayerSkinPlagueDoctor,
						GameId.PlayerSkinBurger,
						GameId.PlayerSkinFootballGuy,
						GameId.PlayerSkinLincoln,
						GameId.PlayerSkinLion,
						GameId.PlayerSkinSatoshi,
						GameId.PlayerSkinSheriff,
						GameId.PlayerSkinSoldier,
						GameId.PlayerSkinSwimmer,
						GameId.PlayerSkinThief,
						GameId.PlayerSkinVR,
						GameId.PlayerSkinWitch,
						GameId.PlayerSkinHazmat,
						GameId.PlayerSkinAura,
						GameId.PlayerSkinMidas,
						GameId.PlayerSkinNFL,
						GameId.PlayerSkinPilot,
						GameId.PlayerSkinSkeleton
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
						GameId.AvatarUnicornssticker,
						GameId.AvatarBrandfemale,
						GameId.AvatarBrandmale,
						GameId.AvatarEgirl,
						GameId.AvatarGearedape,
						GameId.AvatarLeprechaun,
						GameId.AvatarNinja,
						GameId.AvatarNinjaalternative,
						GameId.AvatarPolicefemale,
						GameId.AvatarPlaguedoctormystery,
						GameId.AvatarBurger,
						GameId.AvatarFootballguy,
						GameId.AvatarLion,
						GameId.AvatarLion2,
						GameId.AvatarSatoshi,
						GameId.AvatarSheriff,
						GameId.AvatarSoldier,
						GameId.AvatarSwimmer,
						GameId.AvatarThief,
						GameId.AvatarVr,
						GameId.AvatarBall,
						GameId.AvatarLincoln,
						GameId.AvatarMidasfull,
						GameId.AvatarHazmatfull,
						GameId.AvatarSkeletonfull,
						GameId.AvatarWitchhat,
						GameId.AvatarPumpkin,
						GameId.AvatarAuraarmed,
						GameId.AvatarNflarmed,
						GameId.AvatarPilotarmed,
						GameId.AvatarWitch
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
