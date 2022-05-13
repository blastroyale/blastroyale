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

				health += CalculateGearStat(gameConfig, gearConfig, item, StatType.Health).AsInt;
				speed += CalculateGearStat(gameConfig, gearConfig, item, StatType.Speed);
				armour += CalculateGearStat(gameConfig, gearConfig, item, StatType.Armour).AsInt;
			}
		}

		/// <summary>
		/// Calculates a single stat for a single gear item.
		/// </summary>
		public static FP CalculateGearStat(QuantumGameConfig gameConfig, QuantumGearConfig gearConfig,
		                                   Equipment equipment, StatType stat)
		{
			var baseHealth = CalculateBaseGearStat(gearConfig, gameConfig, stat);
			return ApplyModifiers(baseHealth, equipment, gameConfig, stat);
		}

		/// <summary>
		/// Calculates the weapon power based on weapon stats and NFT config.
		/// </summary>
		public static FP CalculateWeaponPower(QuantumGameConfig gameConfigs, QuantumWeaponConfig weaponConfig,
		                                      Equipment weapon)
		{
			var basePower = CalculateBaseWeaponPower(weaponConfig, gameConfigs);
			return ApplyModifiers(basePower, weapon, gameConfigs, StatType.Power);
		}

		/// <inheritdoc cref="CalculateWeaponPower(Quantum.QuantumGameConfig,Quantum.QuantumWeaponConfig,Quantum.Equipment)"/>
		public static FP CalculateWeaponPower(Frame f, Equipment weapon)
		{
			return CalculateWeaponPower(f.GameConfig, f.WeaponConfigs.GetConfig(weapon.GameId), weapon);
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