using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Extensions responsible for equipment specific functions that requires game configuration to be evaluated.
	/// </summary>
	public static class EquipmentExtensions
	{
		/// <summary>
		/// TODO: Gabriel delete when we update the backend
		/// </summary>
		public static bool IsBroken(this Equipment equipment, bool isNft)
		{
			return equipment.IsBroken(isNft, new QuantumGameConfig { NonNftDurabilityDropDays = 7, NftDurabilityDropDays = 7 });
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
			var dropDays = isNft ? config.NftDurabilityDropDays : config.NonNftDurabilityDropDays;
			var durabilityDropped = (uint) Math.Floor(rustTime.TotalDays / dropDays.AsDouble);

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
			var statsMaterialConfig = configs.GetConfig<QuantumEquipmentMaterialStatConfig>(equipment.GetMaterialStatsKey());
			
			if (equipment.GameId.IsInGroup(GameIdGroup.Weapon))
			{
				var weaponConfig = configs.GetConfig<QuantumWeaponConfig>((int) equipment.GameId);
				
				stats.Add(EquipmentStatType.Hp, 
				          QuantumStatCalculator.CalculateWeaponStat(weaponConfig, statConfigs[(int) StatType.Health],
				                                                    baseStatsConfig, statsConfig, statsMaterialConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.Speed,
				          QuantumStatCalculator.CalculateWeaponStat(weaponConfig, statConfigs[(int) StatType.Speed],
				                                                    baseStatsConfig, statsConfig, statsMaterialConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.Armor,
				          QuantumStatCalculator.CalculateWeaponStat(weaponConfig, statConfigs[(int) StatType.Armour],
				                                                    baseStatsConfig, statsConfig, statsMaterialConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.Power,
				          QuantumStatCalculator.CalculateWeaponStat(weaponConfig, statConfigs[(int) StatType.Power],
				                                                    baseStatsConfig, statsConfig, statsMaterialConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.TargetRange, 
				          (QuantumStatCalculator.CalculateWeaponStat(weaponConfig, statConfigs[(int)StatType.AttackRange],
				                                                    baseStatsConfig, statsConfig, statsMaterialConfig, equipment)
				           + weaponConfig.AttackRange).AsFloat);
				stats.Add(EquipmentStatType.PickupSpeed, 
				          QuantumStatCalculator.CalculateWeaponStat(weaponConfig, statConfigs[(int)StatType.PickupSpeed],
				                                                    baseStatsConfig, statsConfig, statsMaterialConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.MaxCapacity, 
				          QuantumStatCalculator.CalculateWeaponStat(weaponConfig, statConfigs[(int)StatType.AmmoCapacity],
				                                                    baseStatsConfig, statsConfig, statsMaterialConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.ShieldCapacity,
						  QuantumStatCalculator.CalculateWeaponStat(weaponConfig, statConfigs[(int)StatType.Shield],
						                                            baseStatsConfig, statsConfig, statsMaterialConfig, equipment).AsFloat);

				stats.Add(EquipmentStatType.AttackCooldown, weaponConfig.AttackCooldown.AsFloat);
				stats.Add(EquipmentStatType.MinAttackAngle, weaponConfig.MinAttackAngle);
				stats.Add(EquipmentStatType.MaxAttackAngle, weaponConfig.MaxAttackAngle);
				stats.Add(EquipmentStatType.SplashDamageRadius, weaponConfig.SplashRadius.AsFloat);
				stats.Add(EquipmentStatType.PowerToDamageRatio, weaponConfig.PowerToDamageRatio.AsFloat);
				stats.Add(EquipmentStatType.NumberOfShots, weaponConfig.NumberOfShots);
				stats.Add(EquipmentStatType.SpecialId0, (float) weaponConfig.Specials[0]);
				stats.Add(EquipmentStatType.SpecialId1, (float) weaponConfig.Specials[1]);
			}
			else
			{
				stats.Add(EquipmentStatType.Hp, 
				          QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.Health], baseStatsConfig, 
				                                                  statsConfig, statsMaterialConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.Speed,
				          QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.Speed], baseStatsConfig,
				                                                  statsConfig, statsMaterialConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.Armor,
				          QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.Armour], baseStatsConfig, 
				                                                  statsConfig, statsMaterialConfig, equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.Power,
				          QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.Power], baseStatsConfig,
				                                                  statsConfig, statsMaterialConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.TargetRange,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int)  StatType.AttackRange],baseStatsConfig, 
						                                          statsConfig, statsMaterialConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.PickupSpeed,
				          QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.PickupSpeed], baseStatsConfig, 
				                                                  statsConfig, statsMaterialConfig, equipment).AsFloat / 100f);
				stats.Add(EquipmentStatType.MaxCapacity,
				          QuantumStatCalculator.CalculateGearStat(statConfigs[(int) StatType.AmmoCapacity], baseStatsConfig, 
				                                                  statsConfig, statsMaterialConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.ShieldCapacity,
						  QuantumStatCalculator.CalculateGearStat(statConfigs[(int)StatType.Shield], baseStatsConfig,
																  statsConfig, statsMaterialConfig, equipment).AsFloat);
			}

			return stats;
		}
	}
}