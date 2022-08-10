using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This class contains various helper functions to help calculate the stat value to use when Quantum's simulation is running
	/// </summary>
	public static class QuantumStatCalculator
	{
		/// <summary>
		/// Requests the character's stats based on currently equipped <paramref name="weapon"/> and
		/// <paramref name="gear"/> from it's base stats and NFT configs.
		/// </summary>
		public static void CalculateStats(Frame f, Equipment weapon, FixedArray<Equipment> gear, out int armour,
		                                  out int health,
		                                  out FP speed, out FP power)
		{
			health = 0;
			speed = FP._0;
			armour = 0;
			power = FP._0;

			if (weapon.IsValid())
			{
				health += CalculateStat(f, weapon, StatType.Health).AsInt;
				speed += CalculateStat(f, weapon, StatType.Speed);
				armour += CalculateStat(f, weapon, StatType.Armour).AsInt;
				power += CalculateStat(f, weapon, StatType.Power);
			}

			for (var i = 0; i < gear.Length; i++)
			{
				var item = gear[i];
				
				if (!item.IsValid())
				{
					continue;
				}
				
				health += CalculateStat(f, item, StatType.Health).AsInt;
				speed += CalculateStat(f, item, StatType.Speed);
				armour += CalculateStat(f, item, StatType.Armour).AsInt;
				power += CalculateStat(f, item, StatType.Power);
			}
		}

		/// <summary>
		/// Calculates the weapon power based on weapon stats and NFT config.
		/// </summary>
		public static FP CalculateStat(QuantumStatConfig statConfig, QuantumBaseEquipmentStatConfig baseStatConfig,
		                               QuantumEquipmentStatConfig equipmentStatConfig, 
		                               QuantumEquipmentMaterialStatConfig materialStatConfig, Equipment equipment)
		{
			var statRatio = equipmentStatConfig.GetValue(statConfig.StatType) + materialStatConfig.GetValue(statConfig.StatType);
			var attributeValue = CalculateAttributeStatValue(statConfig, baseStatConfig.GetValue(statConfig.StatType), 
			                                                 statRatio, equipment);

			return statConfig.StatType == StatType.Speed ? attributeValue : FPMath.CeilToInt(attributeValue);
		}
		
		private static FP CalculateStat(Frame f, Equipment equipment, StatType statType)
		{
			return CalculateStat(f.StatConfigs.GetConfig(statType), 
			                     f.BaseEquipmentStatConfigs.GetConfig(equipment.GameId), 
			                     f.EquipmentStatConfigs.GetConfig(equipment),
			                     f.EquipmentMaterialStatConfigs.GetConfig(equipment), equipment);
		}

		private static FP CalculateAttributeStatValue(QuantumStatConfig statConfig, FP ratioToBase, FP statRatio, Equipment equipment)
		{
			var modifiedValue = FP._0;
			var baseValue = statConfig.BaseValue * (ratioToBase + statRatio);
			var baseValueForRarity = baseValue * QuantumHelpers.PowFp(statConfig.RarityMultiplier, (uint) equipment.Rarity);

			// Apply rarity
			modifiedValue += baseValueForRarity;

			// Apply grade (keep in mind that the first in order, GradeI, is the most powerful one, so it's reversed to levels and rarities)
			modifiedValue += baseValueForRarity * statConfig.GradeStepMultiplier *
			                 ((uint) EquipmentGrade.TOTAL - (uint) equipment.Grade - 1);

			// Apply level step (equipment.level starts at 0 so we don't need to do -1 like we do in design data)
			modifiedValue += baseValueForRarity * statConfig.LevelStepMultiplier * equipment.Level;

			return modifiedValue;
		}
	}
}