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
		public static AnalyticsData GetAnalyticsData(this Equipment equipment, UniqueId id)
		{
			return new AnalyticsData()
			{
				{ "gameid", equipment.GameId },
				{ "level", equipment.Level },
				{ "rarity", equipment.Rarity },
				{ "uniqueid", id.ToString() },
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
			var durabilityDropped = (uint) Math.Floor(rustTime.TotalDays / dropDays.AsDouble);

			if (!FeatureFlags.ITEM_DURABILITY)
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

				stats.Add(EquipmentStatType.Hp,
						  QuantumStatCalculator.CalculateWeaponStat(weaponConfig, statConfigs[(int) StatType.Health],
																	baseStatsConfig, statsConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.Speed,
						  QuantumStatCalculator.CalculateWeaponStat(weaponConfig, statConfigs[(int) StatType.Speed],
																	baseStatsConfig, statsConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.Armor,
						  QuantumStatCalculator.CalculateWeaponStat(weaponConfig, statConfigs[(int) StatType.Armour],
																	baseStatsConfig, statsConfig, equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.Power,
						  QuantumStatCalculator.CalculateWeaponStat(weaponConfig, statConfigs[(int) StatType.Power],
																	baseStatsConfig, statsConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.TargetRange,
						  (QuantumStatCalculator.CalculateWeaponStat(weaponConfig, statConfigs[(int) StatType.AttackRange],
																	baseStatsConfig, statsConfig, equipment)
						   + weaponConfig.AttackRange).AsFloat);
				stats.Add(EquipmentStatType.PickupSpeed,
						  QuantumStatCalculator.CalculateWeaponStat(weaponConfig, statConfigs[(int) StatType.PickupSpeed],
																	baseStatsConfig, statsConfig, equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.AmmoCapacityBonus,
						  QuantumStatCalculator.CalculateWeaponStat(weaponConfig, statConfigs[(int) StatType.AmmoCapacity],
																	baseStatsConfig, statsConfig, equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.ShieldCapacity,
						  QuantumStatCalculator.CalculateWeaponStat(weaponConfig, statConfigs[(int) StatType.Shield],
																	baseStatsConfig, statsConfig, equipment).AsFloat);

				stats.Add(EquipmentStatType.MaxCapacity, weaponConfig.MaxAmmo.GetDefault());
				stats.Add(EquipmentStatType.AttackCooldown, weaponConfig.AttackCooldown.AsFloat);
				stats.Add(EquipmentStatType.MinAttackAngle, weaponConfig.MinAttackAngle);
				stats.Add(EquipmentStatType.MaxAttackAngle, weaponConfig.MaxAttackAngle);
				stats.Add(EquipmentStatType.SplashDamageRadius, weaponConfig.SplashRadius.AsFloat);
				stats.Add(EquipmentStatType.PowerToDamageRatio, weaponConfig.PowerToDamageRatio.AsFloat);
				stats.Add(EquipmentStatType.NumberOfShots, weaponConfig.NumberOfShots);
				stats.Add(EquipmentStatType.ReloadTime, weaponConfig.ReloadTime.AsFloat);
				stats.Add(EquipmentStatType.MagazineSize, Math.Max(0, weaponConfig.MagazineSize));
				stats.Add(EquipmentStatType.SpecialId0, (float) weaponConfig.Specials[0]);
				stats.Add(EquipmentStatType.SpecialId1, (float) weaponConfig.Specials[1]);
			}
			else
			{
				stats.Add(EquipmentStatType.Hp,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.Health], baseStatsConfig,
																  statsConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.Speed,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.Speed], baseStatsConfig,
																  statsConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.Armor,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.Armour], baseStatsConfig,
																  statsConfig, equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.Power,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.Power], baseStatsConfig,
																  statsConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.TargetRange,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.AttackRange], baseStatsConfig,
																  statsConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.PickupSpeed,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.PickupSpeed], baseStatsConfig,
																  statsConfig, equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.AmmoCapacityBonus,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.AmmoCapacity], baseStatsConfig,
																  statsConfig, equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.ShieldCapacity,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.Shield], baseStatsConfig,
																  statsConfig, equipment).AsFloat);
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
				if (stats.ContainsKey(type) && CanShowStat(type, stats[type]))
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

		public static string GetValueFormat(EquipmentStatType type)
		{
			return type switch
			{
				EquipmentStatType.ReloadTime => "N2",
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

		public static readonly Dictionary<EquipmentStatType, float> MAX_VALUES = new()
		{
			{EquipmentStatType.Power, 1400},
			{EquipmentStatType.Hp, 1000},
			{EquipmentStatType.Speed, 45f},
			{EquipmentStatType.AttackCooldown, 2f},
			{EquipmentStatType.Armor, 0.10f},
			{EquipmentStatType.ProjectileSpeed, 20},
			{EquipmentStatType.TargetRange, 15f},
			{EquipmentStatType.MaxCapacity, 120},
			{EquipmentStatType.ReloadTime, 4f},
			{EquipmentStatType.MinAttackAngle, 60},
			{EquipmentStatType.MaxAttackAngle, 60},
			{EquipmentStatType.SplashDamageRadius, 4f},
			{EquipmentStatType.PowerToDamageRatio, 2f},
			{EquipmentStatType.NumberOfShots, 10},
			{EquipmentStatType.PickupSpeed, 0.25f},
			{EquipmentStatType.ShieldCapacity, 800},
			{EquipmentStatType.MagazineSize, 30},
			{EquipmentStatType.AmmoCapacityBonus, 0.25f},
		};

		public static readonly HashSet<EquipmentStatType> INVERT_VALUES = new()
		{
			EquipmentStatType.AttackCooldown,
			EquipmentStatType.MaxAttackAngle,
			EquipmentStatType.MinAttackAngle,
			EquipmentStatType.ReloadTime
		};
	}
}