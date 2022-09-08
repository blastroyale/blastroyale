using System.Collections.Generic;
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
				//TODO: replace the attackRange from weapon config here
				stats.Add(EquipmentStatType.TargetRange, weaponConfig.AttackRange.AsFloat +
					 QuantumStatCalculator.CalculateWeaponStat(weaponConfig, statConfigs[(int)StatType.AttackRange],
																	baseStatsConfig, statsConfig, statsMaterialConfig, equipment).AsFloat);

				stats.Add(EquipmentStatType.MaxCapacity, weaponConfig.MaxAmmo.GetDefault());
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
				          QuantumStatCalculator.CalculateStat(configs.GetConfig<QuantumStatConfig>((int) StatType.Health),
				                                              baseStatsConfig, statsConfig, statsMaterialConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.Speed,
				          QuantumStatCalculator.CalculateStat(configs.GetConfig<QuantumStatConfig>((int) StatType.Speed), 
				                                              baseStatsConfig, statsConfig, statsMaterialConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.Armor,
				          QuantumStatCalculator.CalculateStat(configs.GetConfig<QuantumStatConfig>((int) StatType.Armour), 
				                                              baseStatsConfig, statsConfig, statsMaterialConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.Power,
				          QuantumStatCalculator.CalculateStat(configs.GetConfig<QuantumStatConfig>((int) StatType.Power), 
				                                              baseStatsConfig, statsConfig, statsMaterialConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.TargetRange,
						  QuantumStatCalculator.CalculateStat(configs.GetConfig<QuantumStatConfig>((int) StatType.AttackRange),
															  baseStatsConfig, statsConfig, statsMaterialConfig, equipment).AsFloat);
				stats.Add(EquipmentStatType.PickupSpeed,
						  QuantumStatCalculator.CalculateStat(configs.GetConfig<QuantumStatConfig>((int) StatType.PickupSpeed),
															  baseStatsConfig, statsConfig, statsMaterialConfig, equipment).AsFloat);
			}

			return stats;
		}
	}
}