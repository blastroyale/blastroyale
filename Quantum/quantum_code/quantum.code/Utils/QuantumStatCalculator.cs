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
			
			health = CalculateWeaponStat(wc, statConfigs[StatType.Health], besc, esc, item).AsInt;
			speed = CalculateWeaponStat(wc, statConfigs[StatType.Speed], besc, esc, item);
			armour = CalculateWeaponStat(wc, statConfigs[StatType.Armour], besc, esc, item).AsInt;
			power = CalculateWeaponStat(wc, statConfigs[StatType.Power], besc, esc, item);
			attackRange = CalculateWeaponStat(wc, statConfigs[StatType.AttackRange], besc, esc, item);
			pickupSpeed = CalculateWeaponStat(wc, statConfigs[StatType.PickupSpeed], besc, esc, item);
			ammoCapacity = CalculateWeaponStat(wc, statConfigs[StatType.AmmoCapacity], besc, esc, item);
			shieldCapacity = CalculateWeaponStat(wc, statConfigs[StatType.Shield], besc, esc, item);
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
			
			health = CalculateGearStat(statConfigs[StatType.Health], besc, esc, item).AsInt;
			speed = CalculateGearStat(statConfigs[StatType.Speed], besc, esc, item);
			armour = CalculateGearStat(statConfigs[StatType.Armour], besc, esc, item).AsInt;
			power = CalculateGearStat(statConfigs[StatType.Power], besc, esc, item);
			attackRange = CalculateGearStat(statConfigs[StatType.AttackRange], besc, esc, item);
			pickupSpeed = CalculateGearStat(statConfigs[StatType.PickupSpeed], besc, esc, item);
			ammoCapacity = CalculateGearStat(statConfigs[StatType.AmmoCapacity], besc, esc, item);
			shieldCapacity = CalculateGearStat(statConfigs[StatType.Shield], besc, esc, item);
		}

		/// <summary>
		/// Requests the total might for the full equipment set
		/// </summary>
		public static int GetTotalMight(QuantumGameConfig gameConfig, Equipment weapon, FixedArray<Equipment> gear)
		{
			var baseValue = gameConfig.MightBaseValue;
			var rarityM = gameConfig.MightRarityMultiplier;
			var levelM = gameConfig.MightLevelMultiplier;
			
			var might = FPMath.CeilToInt(baseValue * QuantumHelpers.PowFp(rarityM, (uint) weapon.Rarity)
										 + (baseValue * levelM * weapon.Level));
			
			for (var i = 0; i < gear.Length; i++)
			{
				if (!gear[i].IsValid())
				{
					continue;
				}
				
				might += FPMath.CeilToInt(baseValue * QuantumHelpers.PowFp(rarityM, (uint) gear[i].Rarity)
										  + (baseValue * levelM * gear[i].Level));
			}
			
			return might;
		}

		/// <summary>
		/// Requests the might for a single item
		/// </summary>
		public static int GetMightOfItem(QuantumGameConfig gameConfig, Equipment item)
		{
			var baseValue = gameConfig.MightBaseValue;
			var rarityM = gameConfig.MightRarityMultiplier;
			var levelM = gameConfig.MightLevelMultiplier;
			
			return FPMath.CeilToInt(baseValue * QuantumHelpers.PowFp(rarityM, (uint) item.Rarity)
									+ (baseValue * levelM * item.Level));
		}
		
		/// <summary>
		/// Calculates the <paramref name="equipment"/> stats based on all Weapon <see cref="Equipment"/> stat configs.
		/// </summary>
		public static FP CalculateWeaponStat(QuantumWeaponConfig weaponConfig, QuantumStatConfig statConfig, 
		                                     QuantumBaseEquipmentStatConfig baseStatConfig,
		                                     QuantumEquipmentStatConfig equipmentStatConfig, Equipment equipment)
		{
			//TODO: make a second method that calls the frame in order to get game modde dependant stats
			
			var attributeValue = CalculateGearStat(statConfig, baseStatConfig, equipmentStatConfig, equipment);
			
			return statConfig.CeilToInt ? FPMath.CeilToInt(attributeValue) : attributeValue;
		}

		/// <summary>
		/// Calculates the <paramref name="equipment"/> stats based on all <see cref="Equipment"/> stat configs.
		/// </summary>
		public static FP CalculateGearStat(QuantumStatConfig statConfig, QuantumBaseEquipmentStatConfig baseStatConfig,
		                               QuantumEquipmentStatConfig equipmentStatConfig, Equipment equipment)
		{
			var statRatio = equipmentStatConfig.GetValue(statConfig.StatType);
			var attributeValue = CalculateAttributeStatValue(statConfig, baseStatConfig.GetValue(statConfig.StatType), 
			                                                 statRatio, equipment);

			return statConfig.CeilToInt ? FPMath.CeilToInt(attributeValue) : attributeValue;
		}

		private static FP CalculateAttributeStatValue(QuantumStatConfig statConfig, FP ratioToBase, FP statRatio, Equipment equipment)
		{
			var modifiedValue = FP._0;
			var value = (ratioToBase + statRatio) * statConfig.BaseValue;

			// Apply rarity multiplier
			modifiedValue += value * QuantumHelpers.PowFp(statConfig.RarityMultiplier, (uint) equipment.Rarity);
			
			// Should always be 0 when initial level (1) by design
			modifiedValue += value * statConfig.LevelStepMultiplier * (equipment.Level - 1);

			return modifiedValue;
		}
	}
}