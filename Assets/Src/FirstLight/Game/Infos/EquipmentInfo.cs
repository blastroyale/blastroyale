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
	}

	public struct EquipmentInfo
	{
		public UniqueId Id;
		public Equipment Equipment;
		public EquipmentManufacturer Manufacturer;
		public Pair<GameId, uint> ScrappingValue;
		public Pair<GameId, uint> UpgradeCost;
		public Pair<GameId, uint>[] FuseCost;
		public Pair<GameId, uint> RepairCost;
		public uint CurrentDurability;
		public bool IsEquipped;
		public bool IsNft;
		public int MaxLevel;
		public Dictionary<EquipmentStatType, float> Stats;
		public Dictionary<EquipmentStatType, float> NextLevelStats;

		/// <summary>
		/// Check if the item is broken or not
		/// </summary>
		public bool IsBroken => CurrentDurability == 0;

		public override string ToString()
		{
			return $"{nameof(Id)}: {Id}, {nameof(Equipment)}: {Equipment}, {nameof(Manufacturer)}: {Manufacturer}, {nameof(ScrappingValue)}: {ScrappingValue}, {nameof(UpgradeCost)}: {UpgradeCost}, {nameof(FuseCost)}: {FuseCost}, {nameof(RepairCost)}: {RepairCost}, {nameof(CurrentDurability)}: {CurrentDurability}, {nameof(IsEquipped)}: {IsEquipped}, {nameof(IsNft)}: {IsNft}, {nameof(MaxLevel)}: {MaxLevel}, {nameof(Stats)}: {Stats}, {nameof(NextLevelStats)}: {NextLevelStats}, {nameof(IsBroken)}: {IsBroken}";
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

	/// <summary>
	/// Extension implementations for the <see cref="EquipmentInfo"/>
	/// </summary>
	public static class EquipmentInfoExtensions
	{
		/// <summary>
		/// Requests the Augmented Sum value for the <see cref="EquipmentInfo"/> list based on the given
		/// <paramref name="modSumFunc"/> modifier
		/// </summary>
		public static double GetAugmentedModSum(this List<EquipmentInfo> items, QuantumGameConfig gameConfig,
												Func<EquipmentInfo, double> modSumFunc)
		{
			var modEquipmentList = new List<Tuple<double, Equipment>>();
			var nftAssumed = gameConfig.NftAssumedOwned;
			var earningsAugDropMod = (double) gameConfig.EarningsAugmentationStrengthDropMod;
			var earningsAugSteepnessMod = (double) gameConfig.EarningsAugmentationStrengthSteepnessMod;
			var augmentedModSum = 0d;

			foreach (var nft in items)
			{
				modEquipmentList.Add(new Tuple<double, Equipment>(modSumFunc(nft), nft.Equipment));
			}

			modEquipmentList = modEquipmentList.OrderByDescending(x => x.Item1).ToList();

			for (var i = 0; i < modEquipmentList.Count; i++)
			{
				var strength = Math.Pow(Math.Max(0, 1 - Math.Pow(i, earningsAugDropMod) / nftAssumed),
					earningsAugSteepnessMod);

				augmentedModSum += modEquipmentList[i].Item1 * strength;
			}

			return augmentedModSum;
		}

		/// <summary>
		/// Requests the durability states for all the equipments in the given <paramref name="items"/>
		/// </summary>
		public static uint GetAvgDurability(this List<EquipmentInfo> items, out uint maxDurability)
		{
			var total = 0u;

			maxDurability = 0u;

			foreach (var nft in items)
			{
				total += nft.CurrentDurability;
				maxDurability += nft.Equipment.MaxDurability;
			}

			return total;
		}

		/// <summary>
		/// Requests a specified <paramref name="stat"/> for all the equipments in the given <paramref name="items"/>
		/// </summary>
		public static float GetTotalStat(this List<EquipmentInfo> items, EquipmentStatType stat)
		{
			var total = 0f;

			foreach (var nft in items)
			{
				total += nft.Stats[stat];
			}

			return total;
		}

		/// <summary>
		/// Requests "Might" for all the equipments in the given <paramref name="items"/>
		/// </summary>
		public static float GetTotalMight(this List<EquipmentInfo> items, IConfigsProvider configsProvider)
		{
			var total = 0f;
			var gameConfig = configsProvider.GetConfig<QuantumGameConfig>();

			foreach (var nft in items)
			{
				total += QuantumStatCalculator.GetMightOfItem(gameConfig, nft.Equipment);
			}

			return total;
		}

		/// <summary>
		/// Requests "Might" for all the equipments in the given <paramref name="items"/>
		/// </summary>
		public static float GetTotalMight(this IEnumerable<Equipment> items, IConfigsProvider configsProvider)
		{
			var total = 0f;
			var gameConfig = configsProvider.GetConfig<QuantumGameConfig>();

			foreach (var item in items)
			{
				var stats = item.GetStats(configsProvider);

				total += QuantumStatCalculator.GetMightOfItem(gameConfig, item);
			}

			return total;
		}
	}
}