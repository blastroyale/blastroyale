using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This class contains various helper functions to help calculate the stat value to use when Quantum's simulation is running
	/// </summary>
	public static class QuantumStatCalculator
	{
		/// <summary>
		/// Requests the character's stats based on currently equipped gear (<see cref="gear"/>) from it's base stats
		/// and NFT configs.
		/// </summary>
		public static void CalculateGearStats(Frame f, Equipment[] gear, out int armour, out int health, out FP speed)
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

				var gearConfig = f.GearConfigs.GetConfig(item.GameId);

				var baseHealth = CalculateBaseGearStat(gearConfig, gameConfig, StatType.Health);
				health += ApplyModifiers(baseHealth, item, gameConfig, StatType.Health).AsInt;

				var baseSpeed = CalculateBaseGearStat(gearConfig, gameConfig, StatType.Speed);
				speed += ApplyModifiers(baseSpeed, item, gameConfig, StatType.Speed).AsInt;

				var baseArmour = CalculateBaseGearStat(gearConfig, gameConfig, StatType.Armour);
				armour += ApplyModifiers(baseArmour, item, gameConfig, StatType.Armour).AsInt;
			}
		}

		/// <summary>
		/// Calculates the weapon power based on weapon stats and NFT config.
		/// </summary>
		public static FP CalculateWeaponPower(Frame f, Equipment weapon)
		{
			var weaponConfig = f.WeaponConfigs.GetConfig(weapon.GameId);

			var basePower = CalculateBaseWeaponPower(weaponConfig, f.GameConfig);
			return ApplyModifiers(basePower, weapon, f.GameConfig, StatType.Power);
		}

		/// <summary>
		/// Applies NFT attributes to a base stat value (i.e. calculates the stat value based on Rarity, Grade etc...).
		/// </summary>
		private static FP ApplyModifiers(FP statValue, Equipment equipment, QuantumGameConfig gameConfig, StatType stat)
		{
			StatCalculationData calculationData;
			var ceil = false;

			switch (stat)
			{
				case StatType.Health:
					ceil = true;
					calculationData = new StatCalculationData(statValue, gameConfig.StatsHpRarityMultiplier,
					                                          gameConfig.StatsHpLevelStepMultiplier,
					                                          gameConfig.StatsHpGradeStepMultiplier);
					break;
				case StatType.Speed:
					calculationData = new StatCalculationData(statValue, gameConfig.StatsSpeedRarityMultiplier,
					                                          gameConfig.StatsSpeedLevelStepMultiplier,
					                                          gameConfig.StatsSpeedGradeStepMultiplier);
					break;
				case StatType.Armour:
					ceil = true;
					calculationData = new StatCalculationData(statValue, gameConfig.StatsArmorRarityMultiplier,
					                                          gameConfig.StatsArmorLevelStepMultiplier,
					                                          gameConfig.StatsArmorGradeStepMultiplier);
					break;
				case StatType.Power:
					ceil = true;
					calculationData = new StatCalculationData(statValue, gameConfig.StatsPowerRarityMultiplier,
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
		/// Calculates a base stat value (i.e. speed / health) from a gear item. This is without any NFT attributes.
		/// </summary>
		private static FP CalculateBaseGearStat(QuantumGearConfig gearConfig, QuantumGameConfig gameConfig,
		                                        StatType type)
		{
			switch (type)
			{
				case StatType.Health:
					return gameConfig.StatsHpBaseValue * gearConfig.HpRatioToBase;
				case StatType.Speed:
					return gameConfig.StatsSpeedBaseValue * gearConfig.SpeedRatioToBase;
				case StatType.Armour:
					return gameConfig.StatsArmorBaseValue * gearConfig.ArmorRatioToBase;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
		}

		/// <summary>
		/// Calculates a base power value (i.e. speed / health) from a weapon. This is without any NFT attributes.
		/// </summary>
		private static FP CalculateBaseWeaponPower(QuantumWeaponConfig weaponConfig, QuantumGameConfig gameConfig)
		{
			return gameConfig.StatsPowerBaseValue * weaponConfig.PowerRatioToBase;
		}

		private static FP CalculateModifiedStatValue(Equipment equipment, StatCalculationData data)
		{
			FP modifiedValue = 0;
			var baseValueForRarity = data.Value * Pow(data.RarityMultiplier, (uint) equipment.Rarity);

			// Apply rarity
			modifiedValue += baseValueForRarity;

			// Apply level step
			modifiedValue += baseValueForRarity * data.LevelStepMultiplier * (equipment.Level - 1);

			// Apply grade
			modifiedValue += baseValueForRarity * data.GradeStepMultiplier * ((uint) equipment.Grade);

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
			public readonly FP RarityMultiplier;
			public readonly FP LevelStepMultiplier;
			public readonly FP GradeStepMultiplier;

			public StatCalculationData(FP value, FP rarityMultiplier, FP levelStepMultiplier, FP gradeStepMultiplier)
			{
				Value = value;
				RarityMultiplier = rarityMultiplier;
				LevelStepMultiplier = levelStepMultiplier;
				GradeStepMultiplier = gradeStepMultiplier;
			}
		}
	}
}