using System.Collections.Generic;
using FirstLight.Game.Infos;
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
			var gameConfig = configs.GetConfig<QuantumGameConfig>();
			var baseStatsConfig =
				configs.GetConfig<QuantumBaseEquipmentStatsConfig>((int) equipment.GameId);
			var statsConfig = configs.GetConfig<QuantumEquipmentStatsConfig>(equipment.GetStatsKey());
			var statsMaterialConfig = configs.GetConfig<QuantumEquipmentMaterialStatsConfig>(equipment.GetMaterialStatsKey());
			if (equipment.GameId.IsInGroup(GameIdGroup.Weapon))
			{
				var weaponConfig = configs.GetConfig<QuantumWeaponConfig>((int) equipment.GameId);

				stats.Add(EquipmentStatType.SpecialId0, (float) weaponConfig.Specials[0]);
				stats.Add(EquipmentStatType.SpecialId1, (float) weaponConfig.Specials[1]);
				stats.Add(EquipmentStatType.MaxCapacity, weaponConfig.MaxAmmo.Get(GameMode.BattleRoyale));
				stats.Add(EquipmentStatType.TargetRange, weaponConfig.AttackRange.AsFloat);
				stats.Add(EquipmentStatType.AttackCooldown, weaponConfig.AttackCooldown.AsFloat);
			}

			stats.Add(EquipmentStatType.Hp,
			          QuantumStatCalculator
				          .CalculateStat(gameConfig, baseStatsConfig, statsConfig, statsMaterialConfig, equipment, StatType.Health).AsFloat);
			stats.Add(EquipmentStatType.Speed,
			          QuantumStatCalculator
				          .CalculateStat(gameConfig, baseStatsConfig, statsConfig, statsMaterialConfig, equipment, StatType.Speed)
				          .AsFloat);
			stats.Add(EquipmentStatType.Armor,
			          QuantumStatCalculator
				          .CalculateStat(gameConfig, baseStatsConfig, statsConfig, statsMaterialConfig, equipment, StatType.Armour)
				          .AsFloat);
			stats.Add(EquipmentStatType.Damage,
			          QuantumStatCalculator
				          .CalculateStat(gameConfig, baseStatsConfig, statsConfig, statsMaterialConfig, equipment, StatType.Power)
				          .AsFloat);
			return stats;
		}
	}
}