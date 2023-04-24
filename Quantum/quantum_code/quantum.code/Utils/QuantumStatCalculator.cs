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
		/// Requests the <see cref="Equipment"/> stats based on the given <paramref name="item"/>
		/// </summary>
		public static void CalculateWeaponStats(Frame f, Equipment item, out int armour, out int health, out FP speed, 
		                                        out FP power, out FP attackRange, out FP pickupSpeed, out FP ammoCapacity,
		                                        out FP shieldCapacity, uint bonusLevel)
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
			
			health = CalculateWeaponStat(ref wc, statConfigs[StatType.Health], ref besc, ref esc, item, bonusLevel).AsInt;
			speed = CalculateWeaponStat(ref wc, statConfigs[StatType.Speed], ref besc, ref esc, item, bonusLevel);
			armour = CalculateWeaponStat(ref wc, statConfigs[StatType.Armour], ref besc, ref esc, item, bonusLevel).AsInt;
			power = CalculateWeaponStat(ref wc, statConfigs[StatType.Power], ref besc, ref esc, item, bonusLevel);
			attackRange = CalculateWeaponStat(ref wc, statConfigs[StatType.AttackRange], ref besc, ref esc, item, bonusLevel);
			pickupSpeed = CalculateWeaponStat(ref wc, statConfigs[StatType.PickupSpeed], ref besc, ref esc, item, bonusLevel);
			ammoCapacity = CalculateWeaponStat(ref wc, statConfigs[StatType.AmmoCapacity], ref besc, ref esc, item, bonusLevel);
			shieldCapacity = CalculateWeaponStat(ref wc, statConfigs[StatType.Shield], ref besc, ref esc, item, bonusLevel);
		}
		
		/// <summary>
		/// Requests the <see cref="Equipment"/> stats based on the given <paramref name="item"/>
		/// </summary>
		public static void CalculateGearStats(Frame f, Equipment item, out int armour, out int health, out FP speed, 
		                                      out FP power, out FP attackRange, out FP pickupSpeed, out FP ammoCapacity,
		                                      out FP shieldCapacity, uint bonusLevel)
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

			health = CalculateGearStat(statConfigs[StatType.Health], ref besc, ref esc, item, bonusLevel).AsInt;
			speed = CalculateGearStat(statConfigs[StatType.Speed], ref besc, ref esc, item, bonusLevel);
			armour = CalculateGearStat(statConfigs[StatType.Armour], ref besc, ref esc, item, bonusLevel).AsInt;
			power = CalculateGearStat(statConfigs[StatType.Power], ref besc, ref esc, item, bonusLevel);
			attackRange = CalculateGearStat(statConfigs[StatType.AttackRange], ref besc, ref esc, item, bonusLevel);
			pickupSpeed = CalculateGearStat(statConfigs[StatType.PickupSpeed], ref besc, ref esc, item, bonusLevel);
			ammoCapacity = CalculateGearStat(statConfigs[StatType.AmmoCapacity], ref besc, ref esc, item, bonusLevel);
			shieldCapacity = CalculateGearStat(statConfigs[StatType.Shield], ref besc, ref esc, item, bonusLevel);
		}

		/// <summary>
		/// Requests the total might for the full equipment set
		/// </summary>
		public static int GetTotalMight(QuantumGameConfig gameConfig, Equipment weapon, FixedArray<Equipment> gear)
		{
			var might = GetMightOfItem(gameConfig, weapon);
			
			for (var i = 0; i < gear.Length; i++)
			{
				if (!gear[i].IsValid())
				{
					continue;
				}
				
				might += GetMightOfItem(gameConfig, gear[i]);
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
									+ (baseValue * levelM * (item.Level - 1)));
		}
		
		/// <summary>
		/// Calculates the <paramref name="equipment"/> stats based on all Weapon <see cref="Equipment"/> stat configs.
		/// </summary>
		public static FP CalculateWeaponStat(ref QuantumWeaponConfig weaponConfig, QuantumStatConfig statConfig, 
		                                     ref QuantumBaseEquipmentStatConfig baseStatConfig,
		                                     ref QuantumEquipmentStatConfig equipmentStatConfig, Equipment equipment, uint playerLevel = 0)
		{
			//TODO: make a second method that calls the frame in order to get game mode dependant stats
			
			var attributeValue = CalculateGearStat(statConfig, ref baseStatConfig, ref equipmentStatConfig, equipment, playerLevel);
			
			return statConfig.CeilToInt ? FPMath.CeilToInt(attributeValue) : attributeValue;
		}

		/// <summary>
		/// Calculates the <paramref name="equipment"/> stats based on all <see cref="Equipment"/> stat configs.
		/// </summary>
		public static FP CalculateGearStat(QuantumStatConfig statConfig, ref QuantumBaseEquipmentStatConfig baseStatConfig,
		                               ref QuantumEquipmentStatConfig equipmentStatConfig, Equipment equipment, uint playerLevel = 0)
		{
			var statRatio = equipmentStatConfig.GetValue(statConfig.StatType);
			var attributeValue = CalculateAttributeStatValue(ref statConfig, baseStatConfig.GetValue(statConfig.StatType), 
			                                                 statRatio, ref equipment, playerLevel);

			return statConfig.CeilToInt ? FPMath.CeilToInt(attributeValue) : attributeValue;
		}

		private static FP CalculateAttributeStatValue(ref QuantumStatConfig statConfig, FP ratioToBase, FP statRatio, ref Equipment equipment, uint playerLevel)
		{
			if (equipment.Level <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(equipment.Level), equipment.Level, "Equipment level cannot be <= 0");
			}
			
			var modifiedValue = FP._0;
			var value = (ratioToBase + statRatio) * statConfig.BaseValue;

			// Apply rarity multiplier
			modifiedValue += value * QuantumHelpers.PowFp(statConfig.RarityMultiplier, (uint) equipment.Rarity);
			
			// Should always be 0 when initial level (1) by design
			modifiedValue += value * statConfig.LevelStepMultiplier * ((equipment.Level - 1) + playerLevel);

			return modifiedValue;
		}
	}
}