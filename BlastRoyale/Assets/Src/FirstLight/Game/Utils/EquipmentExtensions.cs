using System;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Photon.Deterministic;
using Quantum;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Extensions responsible for equipment specific functions that requires game configuration to be evaluated.
	/// </summary>
	public static class EquipmentExtensions
	{
		public static AnalyticsData GetAnalyticsData(this Equipment equipment)
		{
			return new AnalyticsData()
			{
				{ "gameid", equipment.GameId },
				{ "level", equipment.Level },
				{ "rarity", equipment.Rarity },
			};
		}

		/// <summary>
		/// Calculate equipment stats given a configuration.
		/// This is used across other backend services (game-logic service, blockchain service)
		/// </summary>
		public static Dictionary<EquipmentStatType, float> GetStats(this Equipment equipment, IConfigsProvider configs)
		{
			var stats = new Dictionary<EquipmentStatType, float>();
			var statConfigs = configs.GetConfigsDictionary<QuantumStatConfig>();
			var baseStatsConfig = configs.GetConfig<QuantumBaseEquipmentStatConfig>((int) equipment.GameId);
			var statsConfig = configs.GetConfig<QuantumEquipmentStatConfig>(equipment.GetStatsKey());

			if (equipment.GameId.IsInGroup(GameIdGroup.Weapon))
			{
				var weaponConfig = configs.GetConfig<QuantumWeaponConfig>((int) equipment.GameId);
				var power = QuantumStatCalculator.CalculateWeaponStat(ref weaponConfig, statConfigs[(int)StatType.Power],
				                                                      ref baseStatsConfig, ref statsConfig, ref equipment).AsFloat;

				stats.Add(EquipmentStatType.Hp,
						  QuantumStatCalculator.CalculateWeaponStat(ref weaponConfig, statConfigs[(int) StatType.Health],
																	ref baseStatsConfig, ref statsConfig, ref equipment).AsFloat);
				stats.Add(EquipmentStatType.Speed,
						  QuantumStatCalculator.CalculateWeaponStat(ref weaponConfig, statConfigs[(int) StatType.Speed],
																	ref baseStatsConfig, ref statsConfig, ref equipment).AsFloat);
				stats.Add(EquipmentStatType.Armor,
						  QuantumStatCalculator.CalculateWeaponStat(ref weaponConfig, statConfigs[(int) StatType.Armour],
							  ref baseStatsConfig, ref statsConfig, ref equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.Power,
				          QuantumStatCalculator.CalculateWeaponStat(ref weaponConfig, statConfigs[(int) StatType.Power],
				                                                    ref baseStatsConfig, ref statsConfig, ref equipment).AsFloat);
				stats.Add(EquipmentStatType.TargetRange,
				          (QuantumStatCalculator.CalculateWeaponStat(ref weaponConfig, statConfigs[(int) StatType.AttackRange],
				                                                     ref baseStatsConfig, ref statsConfig, ref equipment)
				           + weaponConfig.AttackRange).AsFloat);
				stats.Add(EquipmentStatType.PickupSpeed,
						  QuantumStatCalculator.CalculateWeaponStat(ref weaponConfig, statConfigs[(int) StatType.PickupSpeed],
							  ref baseStatsConfig, ref statsConfig, ref equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.AmmoCapacityBonus,
						  QuantumStatCalculator.CalculateWeaponStat(ref weaponConfig, statConfigs[(int) StatType.AmmoCapacity],
							  ref baseStatsConfig, ref statsConfig, ref equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.ShieldCapacity,
						  QuantumStatCalculator.CalculateWeaponStat(ref weaponConfig, statConfigs[(int) StatType.Shield],
							  ref baseStatsConfig, ref statsConfig, ref equipment).AsFloat);

				stats.Add(EquipmentStatType.MaxCapacity, weaponConfig.MaxAmmo);
				stats.Add(EquipmentStatType.AttackCooldown, weaponConfig.AttackCooldown.AsFloat);
				stats.Add(EquipmentStatType.MinAttackAngle, weaponConfig.MinAttackAngle);
				stats.Add(EquipmentStatType.SplashDamageRadius, weaponConfig.SplashRadius.AsFloat);
				stats.Add(EquipmentStatType.PowerToDamageRatio, weaponConfig.PowerToDamageRatio.AsFloat);
				stats.Add(EquipmentStatType.NumberOfShots, weaponConfig.NumberOfShots);
				stats.Add(EquipmentStatType.MagazineSize, Math.Max(0, weaponConfig.MagazineSize));
				
				stats.Add(EquipmentStatType.Damage, weaponConfig.PowerToDamageRatio.AsFloat * power);
			}
			else
			{
				stats.Add(EquipmentStatType.Hp,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.Health], ref baseStatsConfig,
																  ref statsConfig, ref equipment).AsFloat);
				stats.Add(EquipmentStatType.Speed,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.Speed], ref baseStatsConfig,
																  ref statsConfig, ref equipment).AsFloat);
				stats.Add(EquipmentStatType.Armor,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.Armour], ref baseStatsConfig,
																  ref statsConfig, ref equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.Power,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.Power], ref baseStatsConfig,
																  ref statsConfig, ref equipment).AsFloat);
				stats.Add(EquipmentStatType.TargetRange,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.AttackRange], ref baseStatsConfig,
																  ref statsConfig, ref equipment).AsFloat);
				stats.Add(EquipmentStatType.PickupSpeed,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.PickupSpeed], ref baseStatsConfig,
																  ref statsConfig, ref equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.AmmoCapacityBonus,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.AmmoCapacity], ref baseStatsConfig,
																  ref statsConfig, ref equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.ShieldCapacity,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.Shield], ref baseStatsConfig,
																  ref statsConfig, ref equipment).AsFloat);
			}

			return stats;
		}

		public static bool CanShowStat(EquipmentStatType type, float value)
		{
			if (!MAX_VALUES.TryGetValue(type, out var maxValue)) return false;

			return INVERT_VALUES.Contains(type) || value != 0f;
		}

		public static bool IsSpecial(this EquipmentStatType type)
		{
			return SPECIAL_TYPES.Contains(type);
		}
		
		public static string GetValueFormat(EquipmentStatType type)
		{
			return type switch
			{
				EquipmentStatType.PowerToDamageRatio => "P2",
				EquipmentStatType.Armor => "P2",
				EquipmentStatType.AttackCooldown => "N2",
				EquipmentStatType.TargetRange => "N3",
				EquipmentStatType.PickupSpeed => "P2",
				EquipmentStatType.Speed => "N3",
				EquipmentStatType.SplashDamageRadius => "N2",
				EquipmentStatType.AmmoCapacityBonus => "P2",
				_ => "N0"
			};
		}
		
		// Max values below are PER ITEM
		public static readonly Dictionary<EquipmentStatType, float> MAX_VALUES = new()
		{
			{EquipmentStatType.Hp, 120},
			{EquipmentStatType.Speed, 0.09f},
			{EquipmentStatType.Armor, 0.065f},
			{EquipmentStatType.TargetRange, 11.5f},
			{EquipmentStatType.SplashDamageRadius, 2f},
			{EquipmentStatType.PickupSpeed, 0.12f},
			{EquipmentStatType.ShieldCapacity, 120},
			{EquipmentStatType.Damage, 30},
		};

		public static readonly HashSet<EquipmentStatType> INVERT_VALUES = new()
		{
			EquipmentStatType.AttackCooldown,
			EquipmentStatType.MinAttackAngle
		};
		
		public static readonly HashSet<EquipmentStatType> SPECIAL_TYPES = new()
		{
			EquipmentStatType.SpecialId0,
			EquipmentStatType.SpecialId1,
		};
	}
}