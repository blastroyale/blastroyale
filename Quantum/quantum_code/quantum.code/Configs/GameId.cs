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
					GameId.CS, new List<GameIdGroup>
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
					GameId.XP, new List<GameIdGroup>
					{
						GameIdGroup.Resource
					}.AsReadOnly()
				},
				{
					GameId.EquipmentXP, new List<GameIdGroup>
					{
						GameIdGroup.Resource
					}.AsReadOnly()
				},
				{
					GameId.HcBundle1, new List<GameIdGroup>
					{
						GameIdGroup.IAP
					}.AsReadOnly()
				},
				{
					GameId.HcBundle2, new List<GameIdGroup>
					{
						GameIdGroup.IAP
					}.AsReadOnly()
				},
				{
					GameId.HcBundle3, new List<GameIdGroup>
					{
						GameIdGroup.IAP
					}.AsReadOnly()
				},
				{
					GameId.ScBundle1, new List<GameIdGroup>
					{
						GameIdGroup.IAP
					}.AsReadOnly()
				},
				{
					GameId.ScBundle2, new List<GameIdGroup>
					{
						GameIdGroup.IAP
					}.AsReadOnly()
				},
				{
					GameId.ScBundle3, new List<GameIdGroup>
					{
						GameIdGroup.IAP
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
					GameId.TestScene, new List<GameIdGroup>
					{
						GameIdGroup.Map
					}.AsReadOnly()
				},
				{
					GameId.MausHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.SoldierHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.RiotHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.WarriorHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.SniperRifle, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.Hammer, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.Laser, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.RPG, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.Shotgun, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.AK47, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.BFG, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.AssaultRifle, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.M60, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
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
						GameIdGroup.Equipment
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
						GameIdGroup.Equipment
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
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.SciPistol, new List<GameIdGroup>
					{
						GameIdGroup.Weapon,
						GameIdGroup.Equipment
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
						GameIdGroup.Equipment
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
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.RiotAmulet, new List<GameIdGroup>
					{
						GameIdGroup.Amulet,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.SoldierAmulet, new List<GameIdGroup>
					{
						GameIdGroup.Amulet,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.WarriorAmulet, new List<GameIdGroup>
					{
						GameIdGroup.Amulet,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.TikTokAmulet, new List<GameIdGroup>
					{
						GameIdGroup.Amulet,
						GameIdGroup.Equipment,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.MouseArmor, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.RiotArmor, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.SoldierArmor, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.WarriorArmor, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.RoadSignArmour, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.MouseShield, new List<GameIdGroup>
					{
						GameIdGroup.Shield,
						GameIdGroup.Equipment,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.SoldierShield, new List<GameIdGroup>
					{
						GameIdGroup.Shield,
						GameIdGroup.Equipment,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.WarriorShield, new List<GameIdGroup>
					{
						GameIdGroup.Shield,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.RiotShield, new List<GameIdGroup>
					{
						GameIdGroup.Shield,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.GoldenBoots, new List<GameIdGroup>
					{
						GameIdGroup.Boots,
						GameIdGroup.Equipment,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.SoldierBoots, new List<GameIdGroup>
					{
						GameIdGroup.Boots,
						GameIdGroup.Equipment,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.MouseBoots, new List<GameIdGroup>
					{
						GameIdGroup.Boots,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.RiotBoots, new List<GameIdGroup>
					{
						GameIdGroup.Boots,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.WarriorBoots, new List<GameIdGroup>
					{
						GameIdGroup.Boots,
						GameIdGroup.Equipment
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
						GameIdGroup.PlayerSkin,
						GameIdGroup.BotItem
					}.AsReadOnly()
				},
				{
					GameId.Female02Avatar, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin
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
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.AmmoLarge, new List<GameIdGroup>
					{
						GameIdGroup.Consumable,
						GameIdGroup.Collectable
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
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.ShieldCapacityLarge, new List<GameIdGroup>
					{
						GameIdGroup.Consumable,
						GameIdGroup.Collectable
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
					GameId.EmojiAngry, new List<GameIdGroup>
					{
						GameIdGroup.Emoji
					}.AsReadOnly()
				},
				{
					GameId.EmojiLove, new List<GameIdGroup>
					{
						GameIdGroup.Emoji
					}.AsReadOnly()
				},
				{
					GameId.EmojiAngel, new List<GameIdGroup>
					{
						GameIdGroup.Emoji
					}.AsReadOnly()
				},
				{
					GameId.EmojiCool, new List<GameIdGroup>
					{
						GameIdGroup.Emoji
					}.AsReadOnly()
				},
				{
					GameId.EmojiSick, new List<GameIdGroup>
					{
						GameIdGroup.Emoji
					}.AsReadOnly()
				},
				{
					GameId.Barrel, new List<GameIdGroup>
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
						GameId.CS,
						GameId.BLST
					}.AsReadOnly()
				},
				{
					GameIdGroup.Resource, new List<GameId>
					{
						GameId.XP,
						GameId.EquipmentXP
					}.AsReadOnly()
				},
				{
					GameIdGroup.IAP, new List<GameId>
					{
						GameId.HcBundle1,
						GameId.HcBundle2,
						GameId.HcBundle3,
						GameId.ScBundle1,
						GameId.ScBundle2,
						GameId.ScBundle3
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
						GameId.TestScene
					}.AsReadOnly()
				},
				{
					GameIdGroup.Helmet, new List<GameId>
					{
						GameId.MausHelmet,
						GameId.SoldierHelmet,
						GameId.RiotHelmet,
						GameId.WarriorHelmet
					}.AsReadOnly()
				},
				{
					GameIdGroup.Equipment, new List<GameId>
					{
						GameId.MausHelmet,
						GameId.SoldierHelmet,
						GameId.RiotHelmet,
						GameId.WarriorHelmet,
						GameId.SniperRifle,
						GameId.Hammer,
						GameId.Laser,
						GameId.RPG,
						GameId.Shotgun,
						GameId.AK47,
						GameId.BFG,
						GameId.AssaultRifle,
						GameId.M60,
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
						GameId.MouseShield,
						GameId.SoldierShield,
						GameId.WarriorShield,
						GameId.RiotShield,
						GameId.GoldenBoots,
						GameId.SoldierBoots,
						GameId.MouseBoots,
						GameId.RiotBoots,
						GameId.WarriorBoots
					}.AsReadOnly()
				},
				{
					GameIdGroup.BotItem, new List<GameId>
					{
						GameId.SoldierHelmet,
						GameId.RiotHelmet,
						GameId.TikTokAmulet,
						GameId.SoldierArmor,
						GameId.WarriorArmor,
						GameId.RoadSignArmour,
						GameId.MouseShield,
						GameId.SoldierShield,
						GameId.GoldenBoots,
						GameId.SoldierBoots,
						GameId.Male02Avatar,
						GameId.Female01Avatar
					}.AsReadOnly()
				},
				{
					GameIdGroup.Weapon, new List<GameId>
					{
						GameId.SniperRifle,
						GameId.Hammer,
						GameId.Laser,
						GameId.RPG,
						GameId.Shotgun,
						GameId.AK47,
						GameId.BFG,
						GameId.AssaultRifle,
						GameId.M60,
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
						GameId.RoadSignArmour
					}.AsReadOnly()
				},
				{
					GameIdGroup.Shield, new List<GameId>
					{
						GameId.MouseShield,
						GameId.SoldierShield,
						GameId.WarriorShield,
						GameId.RiotShield
					}.AsReadOnly()
				},
				{
					GameIdGroup.Boots, new List<GameId>
					{
						GameId.GoldenBoots,
						GameId.SoldierBoots,
						GameId.MouseBoots,
						GameId.RiotBoots,
						GameId.WarriorBoots
					}.AsReadOnly()
				},
				{
					GameIdGroup.PlayerSkin, new List<GameId>
					{
						GameId.Male01Avatar,
						GameId.Male02Avatar,
						GameId.Female01Avatar,
						GameId.Female02Avatar
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
						GameId.SpecialDefaultDash
					}.AsReadOnly()
				},
				{
					GameIdGroup.Emoji, new List<GameId>
					{
						GameId.EmojiAngry,
						GameId.EmojiLove,
						GameId.EmojiAngel,
						GameId.EmojiCool,
						GameId.EmojiSick
					}.AsReadOnly()
				},
				{
					GameIdGroup.Destructible, new List<GameId>
					{
						GameId.Barrel
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
			};
	}
}
