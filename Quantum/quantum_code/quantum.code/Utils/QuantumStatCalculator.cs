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

				var statConfig = f.EquipmentStatsConfigs.GetConfig(item);

				if (item.IsWeapon())
				{
					var weaponConfig = f.WeaponConfigs.GetConfig(item.GameId);

					health += CalculateWeaponStat(gameConfig, weaponConfig, statConfig, item, StatType.Health).AsInt;
					speed += CalculateWeaponStat(gameConfig, weaponConfig, statConfig, item, StatType.Speed);
					armour += CalculateWeaponStat(gameConfig, weaponConfig, statConfig, item, StatType.Armour).AsInt;
					power += CalculateWeaponStat(gameConfig, weaponConfig, statConfig, item, StatType.Power);
				}
				else
				{
					var gearConfig = f.GearConfigs.GetConfig(item.GameId);

					health += CalculateGearStat(gameConfig, gearConfig, statConfig, item, StatType.Health).AsInt;
					speed += CalculateGearStat(gameConfig, gearConfig, statConfig, item, StatType.Speed);
					armour += CalculateGearStat(gameConfig, gearConfig, statConfig, item, StatType.Armour).AsInt;
					power += CalculateGearStat(gameConfig, gearConfig, statConfig, item, StatType.Power);
				}
			}
		}
		
		/// <summary>
		/// Calculates a single stat for a single gear item.
		/// </summary>
		public static FP CalculateGearStat(QuantumGameConfig gameConfig, QuantumGearConfig gearConfig,
		                                   QuantumEquipmentStatsConfig statsConfig, Equipment equipment, StatType stat)
		{
			GetBaseGearValues(gearConfig, gameConfig, stat, out var baseValue, out var baseRatio);
			var statRatio = GetStatRatioK(statsConfig, stat);

			return ApplyModifiers(baseValue, baseRatio, statRatio, equipment, gameConfig, stat);
		}

		/// <summary>
		/// Calculates the weapon power based on weapon stats and NFT config.
		/// </summary>
		public static FP CalculateWeaponStat(QuantumGameConfig gameConfig, QuantumWeaponConfig weaponConfig,
		                                     QuantumEquipmentStatsConfig statsConfig, Equipment equipment,
		                                     StatType stat)
		{
			GetBaseWeaponValues(weaponConfig, gameConfig, stat, out var baseValue, out var baseRatio);
			var statRatio = GetStatRatioK(statsConfig, stat);

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

		/// <summary>
		/// Calculates a base stat value (i.e. speed / health) from a gear item.
		/// </summary>
		private static void GetBaseGearValues(QuantumGearConfig gearConfig, QuantumGameConfig gameConfig, StatType type,
		                                      out FP baseValue, out FP baseRatio)
		{
			switch (type)
			{
				case StatType.Health:
					baseValue = gameConfig.StatsHpBaseValue;
					baseRatio = gearConfig.HpRatioToBase;
					break;
				case StatType.Speed:
					baseValue = gameConfig.StatsSpeedBaseValue;
					baseRatio = gearConfig.SpeedRatioToBase;
					break;
				case StatType.Armour:
					baseValue = gameConfig.StatsArmorBaseValue;
					baseRatio = gearConfig.ArmorRatioToBase;
					break;
				case StatType.Power:
					baseValue = gameConfig.StatsPowerBaseValue;
					baseRatio = 0;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
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

		/// <summary>
		/// Calculates a base power value (i.e. speed / health) from a weapon.
		/// </summary>
		private static void GetBaseWeaponValues(QuantumWeaponConfig weaponConfig, QuantumGameConfig gameConfig,
		                                        StatType type, out FP baseValue, out FP baseRatio)
		{
			switch (type)
			{
				case StatType.Health:
					baseValue = gameConfig.StatsHpBaseValue;
					baseRatio = 0;
					break;
				case StatType.Speed:
					baseValue = gameConfig.StatsSpeedBaseValue;
					baseRatio = 0;
					break;
				case StatType.Armour:
					baseValue = gameConfig.StatsArmorBaseValue;
					baseRatio = 0;
					break;
				case StatType.Power:
					baseValue = gameConfig.StatsPowerBaseValue;
					baseRatio = weaponConfig.PowerRatioToBase;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
		}

		private static FP CalculateModifiedStatValue(Equipment equipment, StatCalculationData data)
		{
			FP modifiedValue = 0;

			var baseValue = data.Value * (data.RatioToBase + data.RatioToBaseK);
			var baseValueForRarity = baseValue * Pow(data.RarityMultiplier, (uint) equipment.Rarity);

			// Apply rarity
			modifiedValue += baseValueForRarity;

			// Apply grade (keep in mind that the first in order, GradeI, is the most powerful one, so it's reversed to levels and rarities)
			modifiedValue += baseValueForRarity * data.GradeStepMultiplier *
			                 ((uint) EquipmentGrade.TOTAL - (uint) equipment.Grade - 1);

			// Apply level step (equipment.level starts at 0 so we don't need to do -1 like we do in design data)
			modifiedValue += baseValueForRarity * data.LevelStepMultiplier * equipment.Level;

			return modifiedValue;
		}

		/// <summary>
		/// Requests the math <paramref name="power"/> of the given <paramref name="baseValue"/>
		/// </summary>
		private static FP Pow(FP baseValue, uint power)
		{
			var ret = FP._1;

			for (var i = 0; i < power; i++)
			{
				ret *= baseValue;
			}

			return ret;
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