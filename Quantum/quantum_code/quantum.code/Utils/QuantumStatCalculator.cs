using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This class contains various helper functions to help calculate the stat value to use when Quantum's simulation is running
	/// </summary>
	public static class QuantumStatCalculator
	{
		/// <summary>
		/// Requests the character's stats based on currently equipped gear / weapon (<see cref="gear"/>) from
		/// it's base stats and NFT configs.
		/// </summary>
		public static void CalculateStats(Frame f, IEnumerable<Equipment> equipment, out int armour, out int health,
		                                  out FP speed, out FP power)
		{
			var gameConfig = f.GameConfig;

			health = 0;
			speed = FP._0;
			armour = 0;
			power = FP._0;

			foreach (var item in equipment)
			{
				if (!item.IsValid())
				{
					continue;
				}

				var baseStatConfig = f.BaseEquipmentStatsConfigs.GetConfig(item.GameId);
				var statConfig = f.EquipmentStatsConfigs.GetConfig(item);
				var statMaterialConfig = f.EquipmentMaterialStatsConfigs.GetConfig(item);

				health += CalculateStat(gameConfig, baseStatConfig, statConfig, statMaterialConfig, item, StatType.Health).AsInt;
				speed += CalculateStat(gameConfig, baseStatConfig, statConfig, statMaterialConfig, item, StatType.Speed);
				armour += CalculateStat(gameConfig, baseStatConfig, statConfig, statMaterialConfig, item, StatType.Armour).AsInt;
				power += CalculateStat(gameConfig, baseStatConfig, statConfig, statMaterialConfig, item, StatType.Power);
			}
		}

		/// <summary>
		/// Requests the character's stats based on currently equipped <paramref name="weapon"/> and
		/// <paramref name="gear"/> from it's base stats and NFT configs.
		/// </summary>
		public static void CalculateStats(Frame f, Equipment weapon, FixedArray<Equipment> gear, out int armour,
		                                  out int health,
		                                  out FP speed, out FP power)
		{
			var gameConfig = f.GameConfig;

			health = 0;
			speed = FP._0;
			armour = 0;
			power = FP._0;

			if (weapon.IsValid())
			{
				var baseStatConfig = f.BaseEquipmentStatsConfigs.GetConfig(weapon.GameId);
				var statConfig = f.EquipmentStatsConfigs.GetConfig(weapon);
				var statMaterialConfig = f.EquipmentMaterialStatsConfigs.GetConfig(weapon);

				health += CalculateStat(gameConfig, baseStatConfig, statConfig, statMaterialConfig, weapon, StatType.Health).AsInt;
				speed += CalculateStat(gameConfig, baseStatConfig, statConfig, statMaterialConfig, weapon, StatType.Speed);
				armour += CalculateStat(gameConfig, baseStatConfig, statConfig, statMaterialConfig, weapon, StatType.Armour).AsInt;
				power += CalculateStat(gameConfig, baseStatConfig, statConfig, statMaterialConfig, weapon, StatType.Power);
			}

			for (int i = 0; i < gear.Length; i++)
			{
				var item = gear[i];
				if (!item.IsValid())
				{
					continue;
				}

				var baseStatConfig = f.BaseEquipmentStatsConfigs.GetConfig(item.GameId);
				var statConfig = f.EquipmentStatsConfigs.GetConfig(item);
				var statMaterialConfig = f.EquipmentMaterialStatsConfigs.GetConfig(item);

				health += CalculateStat(gameConfig, baseStatConfig, statConfig, statMaterialConfig, item, StatType.Health).AsInt;
				speed += CalculateStat(gameConfig, baseStatConfig, statConfig, statMaterialConfig, item, StatType.Speed);
				armour += CalculateStat(gameConfig, baseStatConfig, statConfig, statMaterialConfig, item, StatType.Armour).AsInt;
				power += CalculateStat(gameConfig, baseStatConfig, statConfig, statMaterialConfig, item, StatType.Power);
			}
		}

		/// <summary>
		/// Calculates the weapon power based on weapon stats and NFT config.
		/// </summary>
		public static FP CalculateStat(QuantumGameConfig gameConfig, QuantumBaseEquipmentStatsConfig baseStatsConfig,
		                               QuantumEquipmentStatsConfig statsConfig, QuantumEquipmentMaterialStatsConfig materialStatsConfig,
		                               Equipment equipment, StatType stat)
		{
			GetBaseValues(baseStatsConfig, gameConfig, stat, out var baseValue, out var baseRatio);
			var statRatio = GetStatRatioK(statsConfig, stat);
			statRatio += GetMaterialStatRatioK(materialStatsConfig, stat);

			return ApplyModifiers(baseValue, baseRatio, statRatio, equipment, gameConfig, stat);
		}

		/// <summary>
		/// Applies NFT attributes to a base stat value (i.e. calculates the stat value based on Rarity, Grade etc...).
		/// </summary>
		private static FP ApplyModifiers(FP baseValue, FP baseRatio, FP statRatio, Equipment equipment,
		                                 QuantumGameConfig gameConfig, StatType stat)
		{
			StatCalculationData calculationData;
			var ceil = false;

			switch (stat)
			{
				case StatType.Health:
					ceil = true;
					calculationData = new StatCalculationData(baseValue, baseRatio, statRatio,
					                                          gameConfig.StatsHpRarityMultiplier,
					                                          gameConfig.StatsHpLevelStepMultiplier,
					                                          gameConfig.StatsHpGradeStepMultiplier);
					break;
				case StatType.Speed:
					calculationData = new StatCalculationData(baseValue, baseRatio, statRatio,
					                                          gameConfig.StatsSpeedRarityMultiplier,
					                                          gameConfig.StatsSpeedLevelStepMultiplier,
					                                          gameConfig.StatsSpeedGradeStepMultiplier);
					break;
				case StatType.Armour:
					ceil = true;
					calculationData = new StatCalculationData(baseValue, baseRatio, statRatio,
					                                          gameConfig.StatsArmorRarityMultiplier,
					                                          gameConfig.StatsArmorLevelStepMultiplier,
					                                          gameConfig.StatsArmorGradeStepMultiplier);
					break;
				case StatType.Power:
					ceil = true;
					calculationData = new StatCalculationData(baseValue, baseRatio, statRatio,
					                                          gameConfig.StatsPowerRarityMultiplier,
					                                          gameConfig.StatsPowerLevelStepMultiplier,
					                                          gameConfig.StatsPowerGradeStepMultiplier);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(stat), stat, "The stat type is not defined");
			}


			var modifiedValue = CalculateModifiedStatValue(equipment, calculationData);
			if (ceil)
			{
				modifiedValue = FPMath.CeilToInt(modifiedValue);
			}

			return modifiedValue;
		}

		private static FP GetStatRatioK(QuantumEquipmentStatsConfig statsConfig, StatType stat)
		{
			switch (stat)
			{
				case StatType.Health:
					return statsConfig.HpRatioToBaseK;
				case StatType.Power:
					return statsConfig.PowerRatioToBaseK;
				case StatType.Speed:
					return statsConfig.SpeedRatioToBaseK;
				case StatType.Armour:
					return statsConfig.ArmorRatioToBaseK;
				default:
					throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
			}
		}

		private static FP GetMaterialStatRatioK(QuantumEquipmentMaterialStatsConfig statsConfig, StatType stat)
		{
			switch (stat)
			{
				case StatType.Health:
					return statsConfig.HpRatioToBaseK;
				case StatType.Power:
					return statsConfig.PowerRatioToBaseK;
				case StatType.Speed:
					return statsConfig.SpeedRatioToBaseK;
				case StatType.Armour:
					return statsConfig.ArmorRatioToBaseK;
				default:
					throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
			}
		}
		
		/// <summary>
		/// Fetches the base stat value and ratio. 
		/// </summary>
		private static void GetBaseValues(QuantumBaseEquipmentStatsConfig baseStatsConfig, QuantumGameConfig gameConfig,
		                                  StatType type, out FP baseValue, out FP baseRatio)
		{
			switch (type)
			{
				case StatType.Health:
					baseValue = gameConfig.StatsHpBaseValue;
					baseRatio = baseStatsConfig.HpRatioToBase;
					break;
				case StatType.Speed:
					baseValue = gameConfig.StatsSpeedBaseValue;
					baseRatio = baseStatsConfig.SpeedRatioToBase;
					break;
				case StatType.Armour:
					baseValue = gameConfig.StatsArmorBaseValue;
					baseRatio = baseStatsConfig.ArmorRatioToBase;
					break;
				case StatType.Power:
					baseValue = gameConfig.StatsPowerBaseValue;
					baseRatio = baseStatsConfig.PowerRatioToBase;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
		}

		private static FP CalculateModifiedStatValue(Equipment equipment, StatCalculationData data)
		{
			FP modifiedValue = 0;

			var baseValue = data.Value * (data.RatioToBase + data.RatioToBaseK);
			var baseValueForRarity = baseValue * QuantumHelpers.PowFp(data.RarityMultiplier, (uint) equipment.Rarity);

			// Apply rarity
			modifiedValue += baseValueForRarity;

			// Apply grade (keep in mind that the first in order, GradeI, is the most powerful one, so it's reversed to levels and rarities)
			modifiedValue += baseValueForRarity * data.GradeStepMultiplier *
			                 ((uint) EquipmentGrade.TOTAL - (uint) equipment.Grade - 1);

			// Apply level step (equipment.level starts at 0 so we don't need to do -1 like we do in design data)
			modifiedValue += baseValueForRarity * data.LevelStepMultiplier * equipment.Level;

			return modifiedValue;
		}

		private struct StatCalculationData
		{
			public readonly FP Value;
			public readonly FP RatioToBase;
			public readonly FP RatioToBaseK;
			public readonly FP RarityMultiplier;
			public readonly FP LevelStepMultiplier;
			public readonly FP GradeStepMultiplier;

			public StatCalculationData(FP value, FP ratioToBase, FP ratioToBaseK, FP rarityMultiplier,
			                           FP levelStepMultiplier, FP gradeStepMultiplier)
			{
				Value = value;
				RatioToBase = ratioToBase;
				RatioToBaseK = ratioToBaseK;
				RarityMultiplier = rarityMultiplier;
				LevelStepMultiplier = levelStepMultiplier;
				GradeStepMultiplier = gradeStepMultiplier;
			}
		}
	}
}