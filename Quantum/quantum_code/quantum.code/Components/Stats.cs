using System;
using Photon.Deterministic;
using Quantum.Collections;

namespace Quantum
{
	public unsafe partial struct StatData
	{
		public StatData(FP baseValue, FP statValue, StatType statType)
		{
			BaseValue = baseValue;
			StatValue = statValue;
			Type = statType;
		}
	}

	public unsafe partial struct Stats
	{
		/// <summary>
		/// Requests the entity's current might
		/// </summary>
		public int TotalMight => QuantumStatCalculator.GetTotalMight(GetStatData(StatType.Armour).BaseValue,
		                                                             GetStatData(StatType.Health).BaseValue,
		                                                             GetStatData(StatType.Speed).BaseValue,
		                                                             GetStatData(StatType.Power).BaseValue);
		
		public Stats(FP baseHealth, FP basePower, FP baseSpeed, FP baseArmour, FP maxShields, FP startingShields)
		{
			CurrentHealth = baseHealth.AsInt;
			CurrentShield = 0;
			CurrentStatusModifierDuration = FP._0;
			CurrentStatusModifierEndTime = FP._0;
			CurrentStatusModifierType = StatusModifierType.None;
			IsImmune = false;
			ModifiersPtr = Ptr.Null;
			SpellEffectsPtr = Ptr.Null;

			Values[(int) StatType.Health] = new StatData(baseHealth, baseHealth, StatType.Health);
			Values[(int) StatType.Shield] = new StatData(maxShields, startingShields, StatType.Shield);
			Values[(int) StatType.Power] = new StatData(basePower, basePower, StatType.Power);
			Values[(int) StatType.Speed] = new StatData(baseSpeed, baseSpeed, StatType.Speed);
			Values[(int) StatType.Armour] = new StatData(baseArmour, baseArmour, StatType.Armour);
		}

		/// <summary>
		/// Requests the <see cref="StatData"/> represented by the given <paramref name="stat"/>
		/// </summary>
		public StatData GetStatData(StatType stat)
		{
			return Values[(int) stat];
		}

		/// <summary>
		/// Removes all modifiers, removes immunity, resets health and shields
		/// </summary>
		internal void ResetStats(Frame f, Equipment weapon, FixedArray<Equipment> gear)
		{
			CurrentStatusModifierDuration = FP._0;
			CurrentStatusModifierEndTime = FP._0;
			CurrentStatusModifierType = StatusModifierType.None;
			CurrentShield = 0;
			IsImmune = false;
			
			f.ResolveList(Modifiers).Clear();
			RefreshStats(f, weapon, gear);
			
			CurrentHealth = GetStatData(StatType.Health).StatValue.AsInt;
		}
		
		/// <summary>
		/// Refresh the <paramref name="player"/> stats based on the given loadout data
		/// </summary>
		internal void RefreshEquipmentStats(Frame f, PlayerRef player, EntityRef e, Equipment weapon, FixedArray<Equipment> gear)
		{
			var previousStats = this;
			var previousMaxHeath = GetStatData(StatType.Health).StatValue.AsInt;
			var previousMaxShield = GetStatData(StatType.Shield).StatValue.AsInt;

			RefreshStats(f, weapon, gear);
			
			var newMaxHealth = GetStatData(StatType.Health).StatValue.AsInt;
			var newMaxShield = GetStatData(StatType.Shield).StatValue.AsInt;
			var newHealthAmount = Math.Min(CurrentHealth + Math.Max(newMaxHealth - previousMaxHeath, 0), newMaxHealth);
			var newShieldAmount = Math.Min(CurrentShield + Math.Max(newMaxShield - previousMaxShield, 0), newMaxShield);

			// Adapts the player health & shield if new equipment changes player's HP
			SetCurrentHealth(f, e, newHealthAmount);
			SetCurrenShield(f, e, newShieldAmount, previousMaxShield);

			f.Events.OnPlayerEquipmentStatsChanged(player, e, previousStats, this);
		}

		/// <summary>
		/// Adds a new <paramref name="modifier"/> to this <paramref name="entity"/>'s stats data
		/// </summary>
		internal void AddModifier(Frame f, EntityRef entity, Modifier modifier)
		{
			ApplyModifierUpdate(modifier, false);
			f.ResolveList(Modifiers).Add(modifier);

			f.Events.OnStatModifierAdded(entity, modifier);
		}

		/// <summary>
		/// Removes a modifier in the given modifiers list <paramref name="index"/> to this <paramref name="entity"/>'s stats data
		/// </summary>
		internal void RemoveModifier(Frame f, EntityRef entity, int index)
		{
			var list = f.ResolveList(Modifiers);
			var modifier = list[index];
			
			ApplyModifierUpdate(modifier, true);
			
			list.RemoveAt(index);

			f.Events.OnStatModifierRemoved(entity, modifier);
		}

		/// <summary>
		/// Gives the given shields <paramref name="amount"/> to this <paramref name="entity"/> and notifies the change.
		/// </summary>
		internal void GainShield(Frame f, EntityRef entity, int amount)
		{
			SetCurrenShield(f, entity, CurrentShield + amount, GetStatData(StatType.Shield).StatValue.AsInt);
		}

		/// <summary>
		/// Increases the max <paramref name="amount"/> of shield to this <paramref name="entity"/> and notifies the change.
		/// </summary>
		internal void GainShieldCapacity(Frame f, EntityRef entity, int amount)
		{
			var statData = GetStatData(StatType.Shield);
			var previousShieldCapacity = statData.StatValue;
			var maxShieldCapacity = statData.BaseValue;
			var modifierPower = (FP) amount / maxShieldCapacity;
			var newCapacityValue = previousShieldCapacity + (maxShieldCapacity * modifierPower);

			if (previousShieldCapacity.AsInt == maxShieldCapacity.AsInt)
			{
				return;
			}

			if (newCapacityValue > maxShieldCapacity)
			{
				modifierPower = (maxShieldCapacity - previousShieldCapacity) / maxShieldCapacity;
			}

			var modifier = new Modifier
			{
				Id = ++f.Global->ModifierIdCount,
				Type = StatType.Shield,
				Power = modifierPower,
				Duration = FP.MaxValue,
				StartTime = FP._0,
				IsNegative = false
			};

			AddModifier(f, entity, modifier);
			SetCurrenShield(f, entity, CurrentShield + amount, previousShieldCapacity.AsInt);
		}

		/// <summary>
		/// Gives this entity the health based on the given `<paramref name="spell"/> 
		/// </summary>
		internal void GainHealth(Frame f, EntityRef entity, Spell spell)
		{
			if (f.Has<EntityDestroyer>(entity))
			{
				return;
			}
			
			SetCurrentHealth(f, entity, (int) (CurrentHealth + spell.PowerAmount));
		}

		/// <summary>
		/// Reduces the health of this <paramref name="entity"/> based on the given <paramref name="spell"/> data
		/// </summary>
		internal void ReduceHealth(Frame f, EntityRef entity, Spell spell)
		{
			if (f.Has<EntityDestroyer>(entity))
			{
				return;
			}
			
			var previousHealth = CurrentHealth;
			var previousShield = CurrentShield;
			var maxHealth = GetStatData(StatType.Health).StatValue.AsInt;
			var maxShield = GetStatData(StatType.Shield).StatValue.AsInt;
			var armour = GetStatData(StatType.Armour).StatValue.AsInt;
			var totalDamage = Math.Max((int)spell.PowerAmount - armour, 0);
			var damageAmount = totalDamage;
			var shieldDamageAmount = 0;

			if (IsImmune)
			{
				f.Events.OnDamageBlocked(entity);
				return;
			}

			// If there's shields then we reduce it first
			// and if the damage is bigger than shields then we proceed to remove health as well
			if (previousShield > 0)
			{
				shieldDamageAmount = Math.Min(previousShield, damageAmount);
				damageAmount -= shieldDamageAmount;
				
				SetCurrenShield(f, entity, previousShield - shieldDamageAmount, GetStatData(StatType.Shield).StatValue.AsInt);
			}

			f.Events.OnPlayerDamaged(spell, totalDamage, shieldDamageAmount, Math.Min(previousHealth, damageAmount), 
			                         previousHealth, maxHealth, previousShield, maxShield);

			if (damageAmount <= 0)
			{
				return;
			}

			AttackerSetCurrentHealth(f, entity, spell.Attacker, previousHealth - damageAmount);
		}

		private void SetCurrenShield(Frame f, EntityRef entity, int amount, int previousShieldCapacity)
		{
			var previousShield = CurrentShield;
			var currentShieldCapacity = GetStatData(StatType.Shield).StatValue.AsInt;

			CurrentShield = amount > currentShieldCapacity ? currentShieldCapacity : amount;
			
			if (CurrentShield != previousShield)
			{
				f.Events.OnShieldChanged(entity, previousShield, CurrentShield, previousShieldCapacity, currentShieldCapacity);
			}
		}

		private void AttackerSetCurrentHealth(Frame f, EntityRef entity, EntityRef attacker, int amount)
		{
			var previousHealth = CurrentHealth;

			SetCurrentHealth(f, entity, amount);

			if (CurrentHealth == previousHealth && attacker != EntityRef.None)
			{
				f.Events.OnDamageBlocked(entity);
			}

			if (CurrentHealth != previousHealth && attacker != EntityRef.None)
			{
				f.Signals.HealthChangedFromAttacker(entity, attacker, previousHealth);
			}

			if (CurrentHealth == 0)
			{
				f.Signals.HealthIsZeroFromAttacker(entity, attacker);
				f.Events.OnHealthIsZeroFromAttacker(entity, attacker, amount, GetStatData(StatType.Health).StatValue.AsInt);
			}
		}

		private void SetCurrentHealth(Frame f, EntityRef e, int amount)
		{
			var previousHealth = CurrentHealth;
			var maxHealth = GetStatData(StatType.Health).StatValue.AsInt;

			CurrentHealth = Math.Min(maxHealth, amount);
			CurrentHealth = Math.Max(CurrentHealth, 0);

			if (CurrentHealth != previousHealth)
			{
				f.Events.OnHealthChanged(e, previousHealth, CurrentHealth, maxHealth);
			}
		}

		private void RefreshStats(Frame f, Equipment weapon, FixedArray<Equipment> gear)
		{
			var maxShields = f.GameConfig.PlayerMaxShieldCapacity.Get(f);
			var startingShields = f.GameConfig.PlayerStartingShieldCapacity.Get(f);
			var modifiers = f.ResolveList(Modifiers);
			
			GetLoadoutStats(f, weapon, gear, out var armour, out var health, out var speed, out var power);
			
			health += f.GameConfig.PlayerDefaultHealth.Get(f);
			speed += f.GameConfig.PlayerDefaultSpeed.Get(f);
			
			Values[(int) StatType.Health] = new StatData(health, health, StatType.Health);
			Values[(int) StatType.Shield] = new StatData(maxShields, startingShields, StatType.Shield);
			Values[(int) StatType.Power] = new StatData(power, power, StatType.Power);
			Values[(int) StatType.Speed] = new StatData(speed, speed, StatType.Speed);
			Values[(int) StatType.Armour] = new StatData(armour, armour, StatType.Armour);

			foreach (var modifier in modifiers)
			{
				ApplyModifierUpdate(modifier, false);
			}
		}
		
		private void GetLoadoutStats(Frame f, Equipment weapon, FixedArray<Equipment> gear,
		                             out int armour, out int health, out FP speed, out FP power)
		{
			QuantumStatCalculator.CalculateWeaponStats(f, weapon, out armour, out health, out speed, out power);

			for (var i = 0; i < gear.Length; i++)
			{
				QuantumStatCalculator.CalculateGearStats(f, gear[i], out var armour2, out var health2, out var speed2, out var power2);
				
				health += health2;
				speed += speed2;
				armour += armour2;
				power += power2;
			}
		}

		private void ApplyModifierUpdate(Modifier modifier, bool toRemove)
		{
			var statData = Values[(int) modifier.Type];
			var multiplier = modifier.IsNegative ? -1 : 1;
			var additiveValue = statData.BaseValue * modifier.Power * multiplier;

			if (modifier.Type != StatType.Speed)
			{
				additiveValue = FPMath.CeilToInt(additiveValue);
			}

			statData.StatValue += toRemove ? additiveValue * -FP._1 : additiveValue;

			Values[(int) modifier.Type] = statData;
		}
	}
}