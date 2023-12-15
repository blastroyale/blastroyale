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
		/// TODO: Gabriel delete when we update the backend
		/// </summary>
		public static bool IsBroken(this Equipment equipment)
		{
			return equipment.GetCurrentDurability(DateTime.UtcNow.Ticks) == 0;
		}


		/// <summary>
		/// TODO: Gabriel delete when we update the backend
		/// </summary>
		public static uint GetCurrentDurability(this Equipment equipment, long timestamp)
		{
			return equipment.GetCurrentDurability(false, default, timestamp);
		}


		/// <summary>
		/// Shared encapsulated code to detect if a given given <paramref name="equipment"/> is broken.
		/// This code is to be used in Hub & Game Servers to validate given equipments are valid.
		/// </summary>
		public static bool IsBroken(this Equipment equipment, bool isNft, QuantumGameConfig config)
		{
			return equipment.GetCurrentDurability(isNft, config, DateTime.UtcNow.Ticks) == 0;
		}

		/// <summary>
		/// Shared encapsulated code to request the current's <paramref name="equipment"/> durability on the
		/// given <paramref name="timestamp"/>.
		/// This code is to be used in Hub & Game Servers to validate given equipments are valid.
		/// </summary>
		public static uint GetCurrentDurability(this Equipment equipment, bool isNft, QuantumGameConfig config, long timestamp)
		{
			var rustTime = new TimeSpan(timestamp - equipment.LastRepairTimestamp);
			//var dropDays = isNft ? config.NftDurabilityDropDays : config.NonNftDurabilityDropDays;
			// TODO: Gabriel delete when we update the backend
			var dropDays = FP._7;
			
			// We don't let days drop below 0, this way we can set LastRepairTimestamp in the future if needed
			var daysOfRustingPassed = Math.Max(0d, rustTime.TotalDays);
			
			var durabilityDropped = (uint) Math.Floor(daysOfRustingPassed / dropDays.AsDouble);
			
			if ((isNft && !FeatureFlags.ITEM_DURABILITY_NFTS) || (!isNft && !FeatureFlags.ITEM_DURABILITY_NON_NFTS))
			{
				durabilityDropped = 0;
			}

			return equipment.MaxDurability - Math.Min(durabilityDropped, equipment.MaxDurability);
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
				                                                      ref baseStatsConfig, ref statsConfig, equipment).AsFloat;

				stats.Add(EquipmentStatType.Hp,
						  QuantumStatCalculator.CalculateWeaponStat(ref weaponConfig, statConfigs[(int) StatType.Health],
																	ref baseStatsConfig, ref statsConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.Speed,
						  QuantumStatCalculator.CalculateWeaponStat(ref weaponConfig, statConfigs[(int) StatType.Speed],
																	ref baseStatsConfig, ref statsConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.Armor,
						  QuantumStatCalculator.CalculateWeaponStat(ref weaponConfig, statConfigs[(int) StatType.Armour],
							  ref baseStatsConfig, ref statsConfig, equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.Power,
				          QuantumStatCalculator.CalculateWeaponStat(ref weaponConfig, statConfigs[(int) StatType.Power],
				                                                    ref baseStatsConfig, ref statsConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.TargetRange,
				          (QuantumStatCalculator.CalculateWeaponStat(ref weaponConfig, statConfigs[(int) StatType.AttackRange],
				                                                     ref baseStatsConfig, ref statsConfig, equipment)
				           + weaponConfig.AttackRange).AsFloat);
				stats.Add(EquipmentStatType.PickupSpeed,
						  QuantumStatCalculator.CalculateWeaponStat(ref weaponConfig, statConfigs[(int) StatType.PickupSpeed],
							  ref baseStatsConfig, ref statsConfig, equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.AmmoCapacityBonus,
						  QuantumStatCalculator.CalculateWeaponStat(ref weaponConfig, statConfigs[(int) StatType.AmmoCapacity],
							  ref baseStatsConfig, ref statsConfig, equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.ShieldCapacity,
						  QuantumStatCalculator.CalculateWeaponStat(ref weaponConfig, statConfigs[(int) StatType.Shield],
							  ref baseStatsConfig, ref statsConfig, equipment).AsFloat);

				stats.Add(EquipmentStatType.MaxCapacity, weaponConfig.MaxAmmo);
				stats.Add(EquipmentStatType.AttackCooldown, weaponConfig.AttackCooldown.AsFloat);
				stats.Add(EquipmentStatType.MinAttackAngle, weaponConfig.MinAttackAngle);
				stats.Add(EquipmentStatType.SplashDamageRadius, weaponConfig.SplashRadius.AsFloat);
				stats.Add(EquipmentStatType.PowerToDamageRatio, weaponConfig.PowerToDamageRatio.AsFloat);
				stats.Add(EquipmentStatType.NumberOfShots, weaponConfig.NumberOfShots);
				stats.Add(EquipmentStatType.ReloadTime, weaponConfig.ReloadTime.AsFloat);
				stats.Add(EquipmentStatType.MagazineSize, Math.Max(0, weaponConfig.MagazineSize));
				
				stats.Add(EquipmentStatType.Damage, weaponConfig.PowerToDamageRatio.AsFloat * power);
			}
			else
			{
				stats.Add(EquipmentStatType.Hp,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.Health], ref baseStatsConfig,
																  ref statsConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.Speed,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.Speed], ref baseStatsConfig,
																  ref statsConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.Armor,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.Armour], ref baseStatsConfig,
																  ref statsConfig, equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.Power,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.Power], ref baseStatsConfig,
																  ref statsConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.TargetRange,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.AttackRange], ref baseStatsConfig,
																  ref statsConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.PickupSpeed,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.PickupSpeed], ref baseStatsConfig,
																  ref statsConfig, equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.AmmoCapacityBonus,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.AmmoCapacity], ref baseStatsConfig,
																  ref statsConfig, equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.ShieldCapacity,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.Shield], ref baseStatsConfig,
																  ref statsConfig, equipment).AsFloat);
			}

			return stats;
		}

		/// <summary>
		/// Calculate equipment stats given a configuration.
		/// And use the format logic to be more user friendly to be displayed
		/// This is used across other backend services (game-logic service, blockchain service)
		/// </summary>
		public static Dictionary<EquipmentStatType, string> GetStatsFormatted(this Equipment equipment, IConfigsProvider configs)
		{
			var formattedStats = new Dictionary<EquipmentStatType, string>();
			var stats = GetStats(equipment, configs);

			foreach (EquipmentStatType type in Enum.GetValues(typeof(EquipmentStatType)))
			{
				if (stats.ContainsKey(type) && (type.IsSpecial() || CanShowStat(type, stats[type])))
				{
					formattedStats.Add(type, stats[type].ToString(GetValueFormat(type)));
				}
			}

			return formattedStats;
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