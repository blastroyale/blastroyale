using System;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using Quantum;

namespace FirstLight.Game.Infos
{
	/// <summary>
	/// The different types of stats that a piece of equipment may have.
	/// TODO: This should be rethought.
	/// </summary>
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
	
	public struct EquipmentInfo
	{
		public UniqueId Id;
		public Equipment Equipment;
		public TimeSpan NftCooldown;
		public string CardUrl;
		public Dictionary<EquipmentStatType, float> Stats;

		/// <summary>
		/// Requests the info if this equipment is of NFT type
		/// </summary>
		public bool IsNft => true;
	}

	/// <summary>
	/// Extension implementations for the <see cref="EquipmentInfo"/>
	/// </summary>
	public static class EquipmentInfoExtensions
	{
		public static uint GetAvgDurabilty(this List<EquipmentInfo> items, out uint maxDurability)
		{
			var total = 0u;
			
			maxDurability = 0u;
			
			foreach (var nft in items)
			{
				total += nft.Equipment.Durability;
				maxDurability += nft.Equipment.MaxDurability;
			}

			return total;
		}
	}
}