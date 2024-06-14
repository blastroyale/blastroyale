using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;

namespace FirstLight.Game.Infos
{
	/// <summary>
	/// The different types of stats that a piece of equipment may have.
	/// TODO: This should be rethought.
	/// </summary>
	public enum EquipmentStatType
	{
		Power,
		PowerToDamageRatio,
		Hp,
		Speed,
		Armor,
		AttackCooldown,
		TargetRange,
		ProjectileSpeed,
		MaxCapacity,
		ReloadTime,
		MinAttackAngle,
		SplashDamageRadius,
		NumberOfShots,
		SpecialId0,
		SpecialId1,
		PickupSpeed,
		ShieldCapacity,
		MagazineSize,
		AmmoCapacityBonus,
		Damage,
	}

	public struct EquipmentInfo
	{
		public UniqueId Id;
		public Equipment Equipment;
		public EquipmentManufacturer Manufacturer;
		public Pair<GameId, uint> ScrappingValue;
		public bool IsEquipped;
		public bool IsNft;
		public int MaxLevel;

		public override string ToString()
		{
			return $"{nameof(Id)}: {Id}, {nameof(Equipment)}: {Equipment}, {nameof(Manufacturer)}: {Manufacturer}, {nameof(ScrappingValue)}: {ScrappingValue}, {nameof(IsEquipped)}: {IsEquipped}, {nameof(IsNft)}: {IsNft}, {nameof(MaxLevel)}: {MaxLevel}";
		}
	}

	public struct NftEquipmentInfo
	{
		public EquipmentInfo EquipmentInfo;
		public NftEquipmentData NftData;
		public uint NftCooldownInMinutes;

		/// <summary>
		/// Requests the end of the NFT cooldown in UTC time
		/// </summary>
		public DateTime CooldownEndUtcTime => new DateTime(NftData.InsertionTimestamp).AddMinutes(NftCooldownInMinutes);

		/// <summary>
		/// Requests the missing cooldown time for this NFT
		/// </summary>
		public TimeSpan Cooldown => CooldownEndUtcTime - DateTime.UtcNow;

		/// <summary>
		/// Requests if this equipment's NFT is on cooldown or not
		/// </summary>
		public bool IsOnCooldown => Cooldown.TotalSeconds > 0;

		/// <summary>
		/// Because old jsons didn't had SSL, making it backwards compatible
		/// we need SSL for iOS because 'random Apple rant'
		/// </summary>
		public string SafeImageUrl => NftData.ImageUrl.Replace("http:", "https:");
	}
}