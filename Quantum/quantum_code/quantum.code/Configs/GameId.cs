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
						GameIdGroup.GameDesign
					}.AsReadOnly()
				},
				{
					GameId.SC, new List<GameIdGroup>
					{
						GameIdGroup.Currency
					}.AsReadOnly()
				},
				{
					GameId.HC, new List<GameIdGroup>
					{
						GameIdGroup.Currency
					}.AsReadOnly()
				},
				{
					GameId.XP, new List<GameIdGroup>
					{
						GameIdGroup.PlayerValue
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
					GameId.Diagonalshot, new List<GameIdGroup>
					{
						GameIdGroup.PowerUp
					}.AsReadOnly()
				},
				{
					GameId.Multishot, new List<GameIdGroup>
					{
						GameIdGroup.PowerUp
					}.AsReadOnly()
				},
				{
					GameId.Frontshot, new List<GameIdGroup>
					{
						GameIdGroup.PowerUp
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
					GameId.MausHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.GoldenBoots, new List<GameIdGroup>
					{
						GameIdGroup.Boots,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.SoldierBoots, new List<GameIdGroup>
					{
						GameIdGroup.Boots,
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
					GameId.TikTokAmulet, new List<GameIdGroup>
					{
						GameIdGroup.Amulet,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.RoadSignArmour, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
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
					GameId.M60, new List<GameIdGroup>
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
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.WarriorArmor, new List<GameIdGroup>
					{
						GameIdGroup.Armor,
						GameIdGroup.Equipment
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
					GameId.SoldierHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.RiotHelmet, new List<GameIdGroup>
					{
						GameIdGroup.Helmet,
						GameIdGroup.Equipment
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
					GameId.MouseShield, new List<GameIdGroup>
					{
						GameIdGroup.Shield,
						GameIdGroup.Equipment
					}.AsReadOnly()
				},
				{
					GameId.SoldierShield, new List<GameIdGroup>
					{
						GameIdGroup.Shield,
						GameIdGroup.Equipment
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
					GameId.Male01Avatar, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin
					}.AsReadOnly()
				},
				{
					GameId.Male02Avatar, new List<GameIdGroup>
					{
						GameIdGroup.PlayerSkin
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
						GameIdGroup.PlayerSkin
					}.AsReadOnly()
				},
				{
					GameId.CommonBox, new List<GameIdGroup>
					{
						GameIdGroup.LootBox,
						GameIdGroup.TimeBox
					}.AsReadOnly()
				},
				{
					GameId.UncommonBox, new List<GameIdGroup>
					{
						GameIdGroup.LootBox,
						GameIdGroup.TimeBox
					}.AsReadOnly()
				},
				{
					GameId.RareBox, new List<GameIdGroup>
					{
						GameIdGroup.LootBox,
						GameIdGroup.TimeBox
					}.AsReadOnly()
				},
				{
					GameId.EpicBox, new List<GameIdGroup>
					{
						GameIdGroup.LootBox,
						GameIdGroup.TimeBox
					}.AsReadOnly()
				},
				{
					GameId.LegendaryBox, new List<GameIdGroup>
					{
						GameIdGroup.LootBox,
						GameIdGroup.TimeBox
					}.AsReadOnly()
				},
				{
					GameId.CommonCore, new List<GameIdGroup>
					{
						GameIdGroup.LootBox,
						GameIdGroup.CoreBox,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.UncommonCore, new List<GameIdGroup>
					{
						GameIdGroup.LootBox,
						GameIdGroup.CoreBox,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.RareCore, new List<GameIdGroup>
					{
						GameIdGroup.LootBox,
						GameIdGroup.CoreBox,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.EpicCore, new List<GameIdGroup>
					{
						GameIdGroup.LootBox,
						GameIdGroup.CoreBox,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.LegendaryCore, new List<GameIdGroup>
					{
						GameIdGroup.LootBox,
						GameIdGroup.CoreBox,
						GameIdGroup.Collectable
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
					GameId.InterimArmourSmall, new List<GameIdGroup>
					{
						GameIdGroup.Consumable,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.InterimArmourLarge, new List<GameIdGroup>
					{
						GameIdGroup.Consumable,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.CommonStash, new List<GameIdGroup>
					{
						GameIdGroup.Consumable,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.RareStash, new List<GameIdGroup>
					{
						GameIdGroup.Consumable,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.LegendaryStash, new List<GameIdGroup>
					{
						GameIdGroup.Consumable,
						GameIdGroup.Collectable
					}.AsReadOnly()
				},
				{
					GameId.Airstrike, new List<GameIdGroup>
					{
						GameIdGroup.Projectile
					}.AsReadOnly()
				},
				{
					GameId.PointProjectile, new List<GameIdGroup>
					{
						GameIdGroup.Projectile
					}.AsReadOnly()
				},
				{
					GameId.BulletSniper, new List<GameIdGroup>
					{
						GameIdGroup.Projectile
					}.AsReadOnly()
				},
				{
					GameId.BulletHammer, new List<GameIdGroup>
					{
						GameIdGroup.Projectile
					}.AsReadOnly()
				},
				{
					GameId.BulletLaserbolt, new List<GameIdGroup>
					{
						GameIdGroup.Projectile
					}.AsReadOnly()
				},
				{
					GameId.BulletRPG, new List<GameIdGroup>
					{
						GameIdGroup.Projectile
					}.AsReadOnly()
				},
				{
					GameId.BulletSimple, new List<GameIdGroup>
					{
						GameIdGroup.Projectile
					}.AsReadOnly()
				},
				{
					GameId.BulletInvisible, new List<GameIdGroup>
					{
						GameIdGroup.Projectile
					}.AsReadOnly()
				},
				{
					GameId.BulletBFG, new List<GameIdGroup>
					{
						GameIdGroup.Projectile
					}.AsReadOnly()
				},
				{
					GameId.BulletM60, new List<GameIdGroup>
					{
						GameIdGroup.Projectile
					}.AsReadOnly()
				},
				{
					GameId.BulletShotgun, new List<GameIdGroup>
					{
						GameIdGroup.Projectile
					}.AsReadOnly()
				},
				{
					GameId.BulletLaserboltHeal, new List<GameIdGroup>
					{
						GameIdGroup.Projectile
					}.AsReadOnly()
				},
				{
					GameId.BulletGuidedMissileFat, new List<GameIdGroup>
					{
						GameIdGroup.Projectile
					}.AsReadOnly()
				},
				{
					GameId.BulletSmallAirstrike, new List<GameIdGroup>
					{
						GameIdGroup.Projectile
					}.AsReadOnly()
				},
				{
					GameId.BulletEnergyBall, new List<GameIdGroup>
					{
						GameIdGroup.Projectile
					}.AsReadOnly()
				},
				{
					GameId.BulletInvisibleSnakeBoss, new List<GameIdGroup>
					{
						GameIdGroup.Projectile
					}.AsReadOnly()
				},
				{
					GameId.BulletEnergyBallGreen, new List<GameIdGroup>
					{
						GameIdGroup.Projectile
					}.AsReadOnly()
				},
				{
					GameId.SpecialAirstrikeSimple, new List<GameIdGroup>
					{
						GameIdGroup.Special
					}.AsReadOnly()
				},
				{
					GameId.SpecialHealingField, new List<GameIdGroup>
					{
						GameIdGroup.Special
					}.AsReadOnly()
				},
				{
					GameId.SpecialStunSplash, new List<GameIdGroup>
					{
						GameIdGroup.Special
					}.AsReadOnly()
				},
				{
					GameId.SpecialAimingAirstrike, new List<GameIdGroup>
					{
						GameIdGroup.Special
					}.AsReadOnly()
				},
				{
					GameId.SpecialRageSelf, new List<GameIdGroup>
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
					GameId.SpecialInvisibilitySelf, new List<GameIdGroup>
					{
						GameIdGroup.Special
					}.AsReadOnly()
				},
				{
					GameId.SpecialAimingRageArea, new List<GameIdGroup>
					{
						GameIdGroup.Special
					}.AsReadOnly()
				},
				{
					GameId.SpecialHealAround, new List<GameIdGroup>
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
					GameId.SpecialAimingShieldArea, new List<GameIdGroup>
					{
						GameIdGroup.Special
					}.AsReadOnly()
				},
				{
					GameId.SpecialAimingHealArea, new List<GameIdGroup>
					{
						GameIdGroup.Special
					}.AsReadOnly()
				},
				{
					GameId.SpecialAimingInvisibilityArea, new List<GameIdGroup>
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
					GameId.SpecialHealingMode, new List<GameIdGroup>
					{
						GameIdGroup.Special
					}.AsReadOnly()
				},
				{
					GameId.SpecialAggroBeaconGrenade, new List<GameIdGroup>
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
					GameId.DamageHazard, new List<GameIdGroup>
					{
						GameIdGroup.Hazard
					}.AsReadOnly()
				},
				{
					GameId.HealingFieldHazard, new List<GameIdGroup>
					{
						GameIdGroup.Hazard
					}.AsReadOnly()
				},
				{
					GameId.StarStatusHazard, new List<GameIdGroup>
					{
						GameIdGroup.Hazard
					}.AsReadOnly()
				},
				{
					GameId.SkyLaserBeamHazard, new List<GameIdGroup>
					{
						GameIdGroup.Hazard
					}.AsReadOnly()
				},
				{
					GameId.CollisionDamageHazard, new List<GameIdGroup>
					{
						GameIdGroup.Hazard
					}.AsReadOnly()
				},
				{
					GameId.AggroBeaconHazard, new List<GameIdGroup>
					{
						GameIdGroup.Hazard
					}.AsReadOnly()
				},
				{
					GameId.BeamAttackHazard, new List<GameIdGroup>
					{
						GameIdGroup.Hazard
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
						GameId.Random,
						GameId.RealMoney
					}.AsReadOnly()
				},
				{
					GameIdGroup.Currency, new List<GameId>
					{
						GameId.SC,
						GameId.HC
					}.AsReadOnly()
				},
				{
					GameIdGroup.PlayerValue, new List<GameId>
					{
						GameId.XP
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
						GameId.BRGenesis
					}.AsReadOnly()
				},
				{
					GameIdGroup.PowerUp, new List<GameId>
					{
						GameId.Diagonalshot,
						GameId.Multishot,
						GameId.Frontshot
					}.AsReadOnly()
				},
				{
					GameIdGroup.Weapon, new List<GameId>
					{
						GameId.AssaultRifle,
						GameId.SniperRifle,
						GameId.Hammer,
						GameId.Laser,
						GameId.RPG,
						GameId.Shotgun,
						GameId.AK47,
						GameId.BFG,
						GameId.M60
					}.AsReadOnly()
				},
				{
					GameIdGroup.Equipment, new List<GameId>
					{
						GameId.AssaultRifle,
						GameId.MausHelmet,
						GameId.GoldenBoots,
						GameId.SoldierBoots,
						GameId.RiotShield,
						GameId.TikTokAmulet,
						GameId.RoadSignArmour,
						GameId.SniperRifle,
						GameId.Hammer,
						GameId.Laser,
						GameId.RPG,
						GameId.Shotgun,
						GameId.AK47,
						GameId.BFG,
						GameId.M60,
						GameId.MouseAmulet,
						GameId.RiotAmulet,
						GameId.SoldierAmulet,
						GameId.WarriorAmulet,
						GameId.MouseArmor,
						GameId.RiotArmor,
						GameId.SoldierArmor,
						GameId.WarriorArmor,
						GameId.MouseBoots,
						GameId.RiotBoots,
						GameId.WarriorBoots,
						GameId.SoldierHelmet,
						GameId.RiotHelmet,
						GameId.WarriorHelmet,
						GameId.MouseShield,
						GameId.SoldierShield,
						GameId.WarriorShield
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
					GameIdGroup.Shield, new List<GameId>
					{
						GameId.RiotShield,
						GameId.MouseShield,
						GameId.SoldierShield,
						GameId.WarriorShield
					}.AsReadOnly()
				},
				{
					GameIdGroup.Amulet, new List<GameId>
					{
						GameId.TikTokAmulet,
						GameId.MouseAmulet,
						GameId.RiotAmulet,
						GameId.SoldierAmulet,
						GameId.WarriorAmulet
					}.AsReadOnly()
				},
				{
					GameIdGroup.Armor, new List<GameId>
					{
						GameId.RoadSignArmour,
						GameId.MouseArmor,
						GameId.RiotArmor,
						GameId.SoldierArmor,
						GameId.WarriorArmor
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
					GameIdGroup.LootBox, new List<GameId>
					{
						GameId.CommonBox,
						GameId.UncommonBox,
						GameId.RareBox,
						GameId.EpicBox,
						GameId.LegendaryBox,
						GameId.CommonCore,
						GameId.UncommonCore,
						GameId.RareCore,
						GameId.EpicCore,
						GameId.LegendaryCore
					}.AsReadOnly()
				},
				{
					GameIdGroup.TimeBox, new List<GameId>
					{
						GameId.CommonBox,
						GameId.UncommonBox,
						GameId.RareBox,
						GameId.EpicBox,
						GameId.LegendaryBox
					}.AsReadOnly()
				},
				{
					GameIdGroup.CoreBox, new List<GameId>
					{
						GameId.CommonCore,
						GameId.UncommonCore,
						GameId.RareCore,
						GameId.EpicCore,
						GameId.LegendaryCore
					}.AsReadOnly()
				},
				{
					GameIdGroup.Collectable, new List<GameId>
					{
						GameId.CommonCore,
						GameId.UncommonCore,
						GameId.RareCore,
						GameId.EpicCore,
						GameId.LegendaryCore,
						GameId.Rage,
						GameId.Health,
						GameId.AmmoSmall,
						GameId.AmmoLarge,
						GameId.InterimArmourSmall,
						GameId.InterimArmourLarge,
						GameId.CommonStash,
						GameId.RareStash,
						GameId.LegendaryStash
					}.AsReadOnly()
				},
				{
					GameIdGroup.Consumable, new List<GameId>
					{
						GameId.Rage,
						GameId.Health,
						GameId.AmmoSmall,
						GameId.AmmoLarge,
						GameId.InterimArmourSmall,
						GameId.InterimArmourLarge,
						GameId.CommonStash,
						GameId.RareStash,
						GameId.LegendaryStash
					}.AsReadOnly()
				},
				{
					GameIdGroup.Projectile, new List<GameId>
					{
						GameId.Airstrike,
						GameId.PointProjectile,
						GameId.BulletSniper,
						GameId.BulletHammer,
						GameId.BulletLaserbolt,
						GameId.BulletRPG,
						GameId.BulletSimple,
						GameId.BulletInvisible,
						GameId.BulletBFG,
						GameId.BulletM60,
						GameId.BulletShotgun,
						GameId.BulletLaserboltHeal,
						GameId.BulletGuidedMissileFat,
						GameId.BulletSmallAirstrike,
						GameId.BulletEnergyBall,
						GameId.BulletInvisibleSnakeBoss,
						GameId.BulletEnergyBallGreen
					}.AsReadOnly()
				},
				{
					GameIdGroup.Special, new List<GameId>
					{
						GameId.SpecialAirstrikeSimple,
						GameId.SpecialHealingField,
						GameId.SpecialStunSplash,
						GameId.SpecialAimingAirstrike,
						GameId.SpecialRageSelf,
						GameId.SpecialAimingStunGrenade,
						GameId.SpecialInvisibilitySelf,
						GameId.SpecialAimingRageArea,
						GameId.SpecialHealAround,
						GameId.SpecialShieldSelf,
						GameId.SpecialAimingShieldArea,
						GameId.SpecialAimingHealArea,
						GameId.SpecialAimingInvisibilityArea,
						GameId.SpecialSkyLaserBeam,
						GameId.SpecialHealingMode,
						GameId.SpecialAggroBeaconGrenade,
						GameId.SpecialShieldedCharge,
						GameId.SpecialAimingGrenade,
						GameId.SpecialDefaultDash
					}.AsReadOnly()
				},
				{
					GameIdGroup.Hazard, new List<GameId>
					{
						GameId.DamageHazard,
						GameId.HealingFieldHazard,
						GameId.StarStatusHazard,
						GameId.SkyLaserBeamHazard,
						GameId.CollisionDamageHazard,
						GameId.AggroBeaconHazard,
						GameId.BeamAttackHazard
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
