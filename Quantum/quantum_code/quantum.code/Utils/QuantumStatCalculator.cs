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
		/// Requests the <see cref="Equipment"/> stats based on the given <paramref name="item"/>
		/// </summary>
		public static void CalculateWeaponStats(Frame f, Equipment item, out int armour, out int health, out FP speed, 
		                                        out FP power, out FP attackRange, out FP pickupSpeed, out FP ammoCapacity,
		                                        out FP shieldCapacity)
		{
			if (!item.IsValid() || !item.IsWeapon())
			{
				health = 0;
				speed = FP._0;
				armour = 0;
				power = FP._0;
				attackRange = FP._0;
				pickupSpeed = FP._0;
				ammoCapacity = FP._0;
				shieldCapacity = FP._0;
				return;
			}
			
			var statConfigs = f.StatConfigs.Dictionary;
			var wc = f.WeaponConfigs.GetConfig(item.GameId);
			var besc = f.BaseEquipmentStatConfigs.GetConfig(item.GameId);
			var esc = f.EquipmentStatConfigs.GetConfig(item);
			var emsc = f.EquipmentMaterialStatConfigs.GetConfig(item);
			
			health = CalculateWeaponStat(wc, statConfigs[StatType.Health], besc, esc, emsc, item).AsInt;
			speed = CalculateWeaponStat(wc, statConfigs[StatType.Speed], besc, esc, emsc, item);
			armour = CalculateWeaponStat(wc, statConfigs[StatType.Armour], besc, esc, emsc, item).AsInt;
			power = CalculateWeaponStat(wc, statConfigs[StatType.Power], besc, esc, emsc, item);
			attackRange = CalculateWeaponStat(wc, statConfigs[StatType.AttackRange], besc, esc, emsc, item);
			pickupSpeed = CalculateWeaponStat(wc, statConfigs[StatType.PickupSpeed], besc, esc, emsc, item);
			ammoCapacity = CalculateWeaponStat(wc, statConfigs[StatType.AmmoCapacity], besc, esc, emsc, item);
			shieldCapacity = CalculateWeaponStat(wc, statConfigs[StatType.Shield], besc, esc, emsc, item);
		}
		/// <summary>
		/// Requests the <see cref="Equipment"/> stats based on the given <paramref name="item"/>
		/// </summary>
		public static void CalculateGearStats(Frame f, Equipment item, out int armour, out int health, out FP speed, 
		                                      out FP power, out FP attackRange, out FP pickupSpeed, out FP ammoCapacity,
		                                      out FP shieldCapacity)
		{
			if (!item.IsValid())
			{
				health = 0;
				speed = FP._0;
				armour = 0;
				power = FP._0;
				attackRange = FP._0;
				pickupSpeed = FP._0;
				ammoCapacity = FP._0;
				shieldCapacity= FP._0;
				return;
			}
			
			var statConfigs = f.StatConfigs.Dictionary;
			var besc = f.BaseEquipmentStatConfigs.GetConfig(item.GameId);
			var esc = f.EquipmentStatConfigs.GetConfig(item);
			var emsc = f.EquipmentMaterialStatConfigs.GetConfig(item);
			
			health = CalculateGearStat(statConfigs[StatType.Health], besc, esc, emsc, item).AsInt;
			speed = CalculateGearStat(statConfigs[StatType.Speed], besc, esc, emsc, item);
			armour = CalculateGearStat(statConfigs[StatType.Armour], besc, esc, emsc, item).AsInt;
			power = CalculateGearStat(statConfigs[StatType.Power], besc, esc, emsc, item);
			attackRange = CalculateGearStat(statConfigs[StatType.AttackRange], besc, esc, emsc, item);
			pickupSpeed = CalculateGearStat(statConfigs[StatType.PickupSpeed], besc, esc, emsc, item);
			ammoCapacity = CalculateGearStat(statConfigs[StatType.AmmoCapacity], besc, esc, emsc, item);
			shieldCapacity = CalculateGearStat(statConfigs[StatType.Shield], besc, esc, emsc, item);
		}

		/// <summary>
		/// Requests the total might for the give stats
		/// </summary>
		public static int GetTotalMight(IReadOnlyDictionary<StatType, QuantumStatConfig> statConfigs, FP armour, FP health, 
		                                FP speed, FP power, FP attackRange, FP pickupSpeed, FP ammoCapacity, FP shieldCapacity)
		{
			return FPMath.RoundToInt(armour * statConfigs[StatType.Armour].ConversionToMightRate
			                         + health * statConfigs[StatType.Health].ConversionToMightRate
			                         + speed * statConfigs[StatType.Speed].ConversionToMightRate
			                         + power * statConfigs[StatType.Power].ConversionToMightRate
			                         + attackRange * statConfigs[StatType.AttackRange].ConversionToMightRate
			                         + pickupSpeed * statConfigs[StatType.PickupSpeed].ConversionToMightRate
			                         + ammoCapacity * statConfigs[StatType.AmmoCapacity].ConversionToMightRate
			                         + shieldCapacity * statConfigs[StatType.Shield].ConversionToMightRate);
		}

		/// <summary>
		/// Calculates the <paramref name="equipment"/> stats based on all Weapon <see cref="Equipment"/> stat configs.
		/// </summary>
		public static FP CalculateWeaponStat(QuantumWeaponConfig weaponConfig, QuantumStatConfig statConfig, 
		                                     QuantumBaseEquipmentStatConfig baseStatConfig,
		                                     QuantumEquipmentStatConfig equipmentStatConfig, 
		                                     QuantumEquipmentMaterialStatConfig materialStatConfig, Equipment equipment)
		{
			//TODO: make a second method that calls the frame in order to get game modde dependant stats
			
			var attributeValue = CalculateGearStat(statConfig, baseStatConfig, equipmentStatConfig, materialStatConfig, equipment);

			if (statConfig.StatType == StatType.Power)
			{
				attributeValue *= weaponConfig.PowerToDamageRatio;
			}

			return attributeValue;
		}

		/// <summary>
		/// Calculates the <paramref name="equipment"/> stats based on all <see cref="Equipment"/> stat configs.
		/// </summary>
		public static FP CalculateGearStat(QuantumStatConfig statConfig, QuantumBaseEquipmentStatConfig baseStatConfig,
		                               QuantumEquipmentStatConfig equipmentStatConfig, 
		                               QuantumEquipmentMaterialStatConfig materialStatConfig, Equipment equipment)
		{
			var statRatio = equipmentStatConfig.GetValue(statConfig.StatType) + materialStatConfig.GetValue(statConfig.StatType);
			var attributeValue = CalculateAttributeStatValue(statConfig, baseStatConfig.GetValue(statConfig.StatType), 
			                                                 statRatio, equipment);

			return statConfig.CeilToInt ? FPMath.CeilToInt(attributeValue) : attributeValue;
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