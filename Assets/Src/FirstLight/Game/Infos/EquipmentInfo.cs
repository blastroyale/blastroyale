using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using Quantum;

namespace FirstLight.Game.Infos
{
	public struct WeaponInfo
	{
		public EquipmentInfo EquipmentInfo;
		public QuantumWeaponConfig WeaponConfig;
	}
	
	public struct GearInfo
	{
		public EquipmentInfo EquipmentInfo;
		public QuantumGearConfig GearConfig;
	}

	public struct EquipmentDataInfo
	{
		public EquipmentData Data;
		public GameId GameId;

		public EquipmentDataInfo(UniqueId id, GameId gameId, ItemRarity rarity, ItemAdjective adjective,
		                         ItemMaterial material, ItemManufacturer manufacturer, ItemFaction faction, uint level, uint grade)
		{
			Data = new EquipmentData(id, rarity, adjective, material, manufacturer, faction, level, grade);
			GameId = gameId;
		}

		public EquipmentDataInfo(GameId gameId, ItemRarity rarity, ItemAdjective adjective,
		                         ItemMaterial material, ItemManufacturer manufacturer, ItemFaction faction, uint level, uint grade)
		{
			Data = new EquipmentData(UniqueId.Invalid, rarity, adjective, material, manufacturer, faction, level, grade);
			GameId = gameId;
		}

		public static implicit operator Equipment(EquipmentDataInfo info)
		{
			return new Equipment(info.GameId, info.Data.Rarity, info.Data.Adjective, info.Data.Material,
			                     info.Data.Manufacturer, info.Data.Faction, info.Data.Level, info.Data.Grade);
		}
	}
	
	public struct EquipmentInfo
	{
		public EquipmentDataInfo DataInfo;
		public uint UpgradeCost;
		public uint MaxLevel;
		public uint SellCost;
		public Dictionary<EquipmentStatType, float> Stats;
		public ItemRarity BaseRarity;
		public bool IsEquipped;
		public bool IsInInventory;
		public uint ItemPower;
		public bool IsWeapon;
		
		/// <summary>
		/// Requests the information if this equipment is on max level
		/// </summary>
		public bool IsMaxLevel => DataInfo.Data.Level == MaxLevel;
	}

	public struct EquipmentLoadOutInfo
	{
		public EquipmentDataInfo? Weapon;
		public List<EquipmentDataInfo> Gear;
		public uint TotalItemPower;
	}

	public enum EquipmentStatType
	{
		AttackCooldown,
		Damage,
		Hp,
		Speed,
		Armor,
		TargetRange,
		ProjectileSpeed,
		SpecialId0,
		SpecialId1,
		MaxCapacity,
		ReloadSpeed
	}

	/// <summary>
	/// Dictionary comparer for the <see cref="EquipmentStatType"/>
	/// </summary>
	public class EquipmentStatTypeComparer : IEqualityComparer<EquipmentStatType>
	{
		public bool Equals(EquipmentStatType x, EquipmentStatType y)
		{
			return x == y;
		}

		public int GetHashCode(EquipmentStatType obj)
		{
			return (int)obj;
		}
	}
}