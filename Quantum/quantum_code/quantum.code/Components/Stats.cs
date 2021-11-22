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
		public Stats(FP baseHealth, FP basePower, FP baseSpeed, FP baseArmour, FP maxInterimArmour)
		{
			CurrentHealth = baseHealth.AsInt;
			CurrentInterimArmour = 0;
			CurrentStatusModifierDuration = FP._0;
			CurrentStatusModifierEndTime = FP._0;
			CurrentStatusModifierType = StatusModifierType.None;
			IsImmune = false;
			ModifiersPtr = Ptr.Null;

			Values[(int) StatType.Health] = new StatData(baseHealth, baseHealth, StatType.Health);
			Values[(int) StatType.InterimArmour] = new StatData(0, maxInterimArmour, StatType.InterimArmour);
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
		/// Removes an effect of modifier from the stats data
		/// </summary>
		internal void RemoveModifier(Frame f, Modifier modifier)
		{
			var statData = Values[(int) modifier.Type];
			var multiplier = modifier.IsNegative ? -1 : 1;

			statData.StatValue -= statData.BaseValue * modifier.Power * multiplier;
			Values[(int) modifier.Type] = statData;
		}

		/// <summary>
		/// Removes an effect of modifier by ID and also removes it from the list
		/// </summary>
		internal void RemoveModifier(Frame f, uint id)
		{
			var list = f.ResolveList(Modifiers);

			for (var i = list.Count - 1; i > -1 ; i--)
			{
				if (list[i].Id == id)
				{
					RemoveModifier(f, list[i]);
					list.RemoveAt(i);
					break;
				}
			}
		}
		
		/// <summary>
		/// Adds a new modifier to the stats data
		/// </summary>
		internal void AddModifier(Frame f, Modifier modifier)
		{
			var statData = Values[(int) modifier.Type];
			var multiplier = modifier.IsNegative ? -1 : 1;

			statData.StatValue += statData.BaseValue * modifier.Power * multiplier;
			Values[(int) modifier.Type] = statData;
			
			f.ResolveList(Modifiers).Add(modifier);
		}
		
		/// <summary>
		/// Gives the given interim armour <paramref name="amount"/> to this <paramref name="entity"/> and notifies the change.
		/// This interim armour gain was induced by the given <paramref name="attacker"/>.
		/// If the given <paramref name="attacker"/> equals <seealso cref="EntityRef.None"/> or invalid, then it is dead
		/// or non existent (Ex: consumable).
		/// </summary>
		internal void GainInterimArmour(Frame f, EntityRef entity, EntityRef attacker, int amount)
		{
			if (IsImmune)
			{
				return;
			}
			
			var previousInterimArmour = CurrentInterimArmour;
			var maxInterimArmour = Values[(int) StatType.InterimArmour].StatValue.AsInt;

			CurrentInterimArmour = CurrentInterimArmour + amount > maxInterimArmour ? maxInterimArmour : CurrentInterimArmour + amount;
			
			if (CurrentInterimArmour != previousInterimArmour)
			{
				f.Events.OnInterimArmourChanged(entity, attacker, previousInterimArmour, CurrentInterimArmour, maxInterimArmour);
			}
		}

		/// <summary>
		/// Sets the entity health based on the given <paramref name="percentage"/> (between 0 - 1)
		/// </summary>
		internal void SetCurrentHealth(Frame f, EntityRef entity, FP percentage)
		{
			var previousHealth = CurrentHealth;
			var maxHealth = Values[(int) StatType.Health].StatValue.AsInt;
			
			CurrentHealth = FPMath.RoundToInt(Values[(int) StatType.Health].StatValue * FPMath.Clamp01(percentage));
			
			f.Events.OnHealthChanged(entity, EntityRef.None, previousHealth, CurrentHealth, maxHealth);
		}
		
		/// <summary>
		/// Gives the given health <paramref name="amount"/> to this <paramref name="entity"/> and notifies the change.
		/// This health gain was induced by the given <paramref name="attacker"/>.
		/// If the given <paramref name="attacker"/> equals <seealso cref="EntityRef.None"/> or invalid, then it is dead
		/// or non existent (Ex: consumable).
		/// </summary>
		internal void GainHealth(Frame f, EntityRef entity, EntityRef attacker, int amount)
		{
			if (IsImmune)
			{
				return;
			}
			
			var previousHealth = CurrentHealth;
			var maxHealth = Values[(int) StatType.Health].StatValue.AsInt;

			CurrentHealth = CurrentHealth + amount > maxHealth ? maxHealth : CurrentHealth + amount;

			if (CurrentHealth != previousHealth)
			{
				f.Events.OnHealthChanged(entity, attacker, previousHealth, CurrentHealth, maxHealth);
				f.Signals.HealthChanged(entity, attacker, previousHealth);
			}
		}
		
		/// <summary>
		/// Reduces the given health <paramref name="damageAmount"/> to this <paramref name="entity"/> and notifies the change.
		/// This health gain was induced by the given <paramref name="attacker"/> from a specific given <paramref name="hitSource"/>.
		/// If the given <paramref name="attacker"/> equals <seealso cref="EntityRef.None"/> or invalid, then it is dead
		/// or non existent.
		/// The <paramref name="hitSource"/> is always the damage output source for this damage reduction
		/// </summary>
		internal void ReduceHealth(Frame f, EntityRef entity, EntityRef attacker, EntityRef hitSource, int damageAmount)
		{
			var amount = damageAmount;
			
			if (IsImmune)
			{
				return;
			}
			
			// If there's Interim Armour then we reduce it first
			// and if the damage is bigger than armour then we proceed to remove health as well
			if (CurrentInterimArmour > 0)
			{
				var previousInterimArmour = CurrentInterimArmour;
				var maxInterimArmour = Values[(int) StatType.InterimArmour].StatValue.AsInt;
				
				CurrentInterimArmour = amount > CurrentInterimArmour ? 0 : previousInterimArmour - amount;
				
				// Reduce the damage value on the amount of armour
				amount = previousInterimArmour < amount ? amount - previousInterimArmour : 0;
				
				f.Events.OnInterimArmourChanged(entity, attacker, previousInterimArmour, CurrentInterimArmour, maxInterimArmour);
			}

			if (amount <= 0)
			{
				return;
			}
			
			var previousHealth = CurrentHealth;
			var maxHealth = Values[(int) StatType.Health].StatValue.AsInt;
			var direction = f.Get<Transform3D>(entity).Position - f.Get<Transform3D>(attacker).Position;

			CurrentHealth = amount > CurrentHealth ? 0 : previousHealth - amount;
				
			if (CurrentHealth == previousHealth)
			{
				return;
			}
			
			f.Events.OnHealthChanged(entity, attacker, previousHealth, CurrentHealth, maxHealth);
			f.Signals.HealthChanged(entity, attacker, previousHealth);

			if (CurrentHealth == 0)
			{
				f.Events.OnHealthIsZero(entity, attacker, direction, damageAmount);
				f.Signals.HealthIsZero(entity, attacker);
			}
		}
	}
}