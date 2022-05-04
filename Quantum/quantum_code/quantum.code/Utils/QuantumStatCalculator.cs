using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This class contains various helper functions to help calculate the stat value to use when Quantum's simulation is running
	/// </summary>
	public static unsafe class QuantumStatCalculator
	{
		/// <summary>
		/// Requests the character's max stats from the given <paramref name="gear"/> load out
		/// </summary>
		public static void CalculateStats(Frame f, Equipment[] gear, out int armour, out int health, out FP speed)
		{
			var gameConfig = f.GameConfig;

			health = 0;
			speed = 0;
			armour = 0;

			foreach (var item in gear)
			{
				if (!item.IsValid)
				{
					continue;
				}
				
				var config = f.GearConfigs.GetConfig(item.GameId);
					
				health += CalculateStatValue(item.Rarity, config.HpRatioToBase, item.Level, item.GradeIndex, gameConfig, StatType.Health).AsInt;
				speed += CalculateStatValue(item.Rarity, config.SpeedRatioToBase, item.Level, item.GradeIndex, gameConfig, StatType.Speed).AsInt;
				armour += CalculateStatValue(item.Rarity, config.ArmorRatioToBase, item.Level, item.GradeIndex, gameConfig, StatType.Armour).AsInt;
			}
		}

		/// <summary>
		/// Requests the math <paramref name="power"/> of the given <paramref name="baseValue"/>
		/// </summary>
		public static FP Pow(FP baseValue, uint power)
		{
			var ret = FP._1;

			for (var i = 0; i < power; i++)
			{
				ret *= baseValue;
			}

			return ret;
		}

		/// <summary>
		/// Requests the rarity value based on the given stat information on the pre defined formula
		/// </summary>
		public static FP CalculateStatValue(ItemRarity rarity, FP ratioBaseValue, uint level, uint gradeIndex, QuantumGameConfig config, StatType statType)
		{
			switch (statType)
			{
				case StatType.Health:
					return FPMath.CeilToInt(CalculateStatValue(config.StatsHpBaseValue, ratioBaseValue, rarity, 
					                                           config.StatsHpRarityMultiplier, (int) level, 
					                                           config.StatsHpLevelStepMultiplier,
					                                           (int) gradeIndex, config.StatsHpGradeStepMultiplier));
				case StatType.Power:
					return FPMath.CeilToInt(CalculateStatValue(config.StatsPowerBaseValue, ratioBaseValue, rarity, 
					                                           config.StatsPowerRarityMultiplier, (int) level, 
					                                           config.StatsPowerLevelStepMultiplier,
					                                           (int) gradeIndex, config.StatsPowerGradeStepMultiplier));
				case StatType.Speed:
					
					return CalculateStatValue(config.StatsSpeedBaseValue, ratioBaseValue, rarity, 
					                          config.StatsSpeedRarityMultiplier, (int) level, 
					                          config.StatsSpeedLevelStepMultiplier,
					                          (int) gradeIndex,config.StatsSpeedGradeStepMultiplier);
				case StatType.Armour:
					
					return FPMath.CeilToInt(CalculateStatValue(config.StatsArmorBaseValue, ratioBaseValue, rarity, 
					                                           config.StatsArmorRarityMultiplier, (int) level, 
					                                           config.StatsArmorLevelStepMultiplier,
					                                           (int) gradeIndex, config.StatsArmorGradeStepMultiplier));
				default:
					throw new ArgumentOutOfRangeException(nameof(statType), statType, "The stat type is not defined");
			}
		}

		/// <summary>
		/// Requests the rarity value based on the given stat information on the pre defined formula
		/// </summary>
		public static FP CalculateStatValue(FP baseValue, FP ratioBaseValue, ItemRarity rarity,
		                                        FP rarityMultiplier, int level, FP levelStepMultiplier,
		                                        int gradeIndex, FP gradeStepMultiplier)
		{
			var baseValueForRarity = baseValue * ratioBaseValue * Pow(rarityMultiplier, (uint) rarity);
			return baseValueForRarity + baseValueForRarity * levelStepMultiplier * (level - 1)
			                          + baseValueForRarity * gradeStepMultiplier * (gradeIndex - 1);
		}
	}
}