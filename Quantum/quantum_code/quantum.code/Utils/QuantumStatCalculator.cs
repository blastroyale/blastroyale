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
		public static void CalculateStats(Frame f, Equipment weapon, FixedArray<Equipment> gear, 
		                                  out int armour, out int health, out FP speed, out FP power, 
										  out FP attackRange, out FP pickupSpeed)
		{
			var statConfigs = f.StatConfigs.Dictionary;
			
			health = 0;
			speed = FP._0;
			armour = 0;
			power = FP._0;
			attackRange = FP._0;
			pickupSpeed = FP._0;

			if (weapon.IsValid())
			{
				var wc = f.WeaponConfigs.GetConfig(weapon.GameId);
				var besc = f.BaseEquipmentStatConfigs.GetConfig(weapon.GameId);
				var esc = f.EquipmentStatConfigs.GetConfig(weapon);
				var emsc = f.EquipmentMaterialStatConfigs.GetConfig(weapon);
				
				health += CalculateWeaponStat(wc, statConfigs[StatType.Health], besc, esc, emsc, weapon).AsInt;
				speed += CalculateWeaponStat(wc, statConfigs[StatType.Speed], besc, esc, emsc, weapon);
				armour += CalculateWeaponStat(wc, statConfigs[StatType.Armour], besc, esc, emsc, weapon).AsInt;
				power += CalculateWeaponStat(wc, statConfigs[StatType.Power], besc, esc, emsc, weapon);
				attackRange += CalculateWeaponStat(wc, statConfigs[StatType.AttackRange], besc, esc, emsc, weapon);
				pickupSpeed += CalculateWeaponStat(wc, statConfigs[StatType.PickupSpeed], besc, esc, emsc, weapon);
			}

			for (var i = 0; i < gear.Length; i++)
			{
				var item = gear[i];
				
				if (!item.IsValid())
				{
					continue;
				}
				
				var besc = f.BaseEquipmentStatConfigs.GetConfig(item.GameId);
				var esc = f.EquipmentStatConfigs.GetConfig(item);
				var emsc = f.EquipmentMaterialStatConfigs.GetConfig(item);
				
				health += CalculateStat(statConfigs[StatType.Health], besc, esc, emsc, item).AsInt;
				speed += CalculateStat(statConfigs[StatType.Speed], besc, esc, emsc, item);
				armour += CalculateStat(statConfigs[StatType.Armour], besc, esc, emsc, item).AsInt;
				power += CalculateStat(statConfigs[StatType.Power], besc, esc, emsc, item);
				attackRange += CalculateStat(statConfigs[StatType.AttackRange], besc, esc, emsc, item);
				pickupSpeed += CalculateStat(statConfigs[StatType.PickupSpeed], besc, esc, emsc, item);
			}
		}

		/// <summary>
		/// Calculates the <paramref name="equipment"/> stats based on all Weapon <see cref="Equipment"/> stat configs.
		/// </summary>
		public static FP CalculateWeaponStat(QuantumWeaponConfig weaponConfig, QuantumStatConfig statConfig, 
		                                     QuantumBaseEquipmentStatConfig baseStatConfig,
		                                     QuantumEquipmentStatConfig equipmentStatConfig, 
		                                     QuantumEquipmentMaterialStatConfig materialStatConfig, Equipment equipment)
		{
			var attributeValue = CalculateStat(statConfig, baseStatConfig, equipmentStatConfig, materialStatConfig, equipment);

			if (statConfig.StatType == StatType.Power)
			{
				attributeValue *= weaponConfig.PowerToDamageRatio;
			}

			return attributeValue;
		}

		/// <summary>
		/// Calculates the <paramref name="equipment"/> stats based on all <see cref="Equipment"/> stat configs.
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