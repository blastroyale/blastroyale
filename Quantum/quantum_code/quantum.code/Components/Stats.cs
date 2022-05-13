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
		public Stats(FP baseHealth, FP basePower, FP baseSpeed, FP baseArmour, FP maxInterimArmour, FP startingInterimArmour)
		{
			CurrentHealth = baseHealth.AsInt;
			CurrentInterimArmour = 0;
			CurrentStatusModifierDuration = FP._0;
			CurrentStatusModifierEndTime = FP._0;
			CurrentStatusModifierType = StatusModifierType.None;
			IsImmune = false;
			ModifiersPtr = Ptr.Null;
			SpellEffectsPtr = Ptr.Null;

			Values[(int) StatType.Health] = new StatData(baseHealth, baseHealth, StatType.Health);
			Values[(int) StatType.InterimArmour] = new StatData(maxInterimArmour, startingInterimArmour, StatType.InterimArmour);
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

			for (var i = list.Count - 1; i > -1; i--)
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
		/// Sets the entity interim armour based on the given <paramref name="amount"/>
		/// </summary>
		internal void SetInterimArmour(Frame f, EntityRef entity, EntityRef attacker, int amount)
		{
			var previousInterimArmour = CurrentInterimArmour;
			var CurrentShieldCapacity = Values[(int)StatType.InterimArmour].StatValue.AsInt;

			CurrentInterimArmour = amount > CurrentShieldCapacity
									   ? CurrentShieldCapacity
									   : amount;
			
			if (CurrentInterimArmour != previousInterimArmour)
			{
				f.Events.OnInterimArmourChanged(entity, attacker, previousInterimArmour, CurrentInterimArmour,
												CurrentShieldCapacity);
			}
		}


		
		/// <summary>
		/// Gives the given interim armour <paramref name="amount"/> to this <paramref name="entity"/> and notifies the change.
		/// This interim armour gain was induced by the given <paramref name="attacker"/>.
		/// If the given <paramref name="attacker"/> equals <seealso cref="EntityRef.None"/> or invalid, then it is dead
		/// or non existent anymore.
		/// </summary>
		internal void GainInterimArmour(Frame f, EntityRef entity, EntityRef attacker, int amount)
		{
			SetInterimArmour(f, entity, attacker, CurrentInterimArmour + amount);
		}

		/// <summary>
		/// Adds <paramref name="amount"/> of interim armour capacity as a stat modifier as <paramref name="entity"/> and notifies the change.
		/// This shield capacity gain was induced by the given <paramref name="attacker"/>.
		/// If the given <paramref name="attacker"/> equals <seealso cref="EntityRef.None"/> or invalid, then it is dead
		/// or non existent anymore.
		/// </summary>
		internal void IncreaseShieldCapacity(Frame f, EntityRef entity, EntityRef attacker, int amount)
		{

			var interimArmour = Values[(int)StatType.InterimArmour];
			var maxShieldCapacity = interimArmour.BaseValue.AsInt;
			var modifierId = ++f.Global->ModifierIdCount;

			if (interimArmour.StatValue.AsInt == maxShieldCapacity)
				return;

			var modValue = interimArmour.StatValue.AsInt + (maxShieldCapacity * (FP)amount / (FP)maxShieldCapacity);
			if (modValue > maxShieldCapacity)
			{
				amount = (modValue.AsInt) - maxShieldCapacity;
			}

			var capacityModifer = new Modifier
			{
				Id = modifierId,
				Type = StatType.InterimArmour,
				Power = (FP)amount / (FP)maxShieldCapacity,
				Duration = FP.MaxValue,
				EndTime = FP.MaxValue,
				IsNegative = false
			};

			AddModifier(f, capacityModifer);
			interimArmour = Values[(int)StatType.InterimArmour];
			if (interimArmour.StatValue > maxShieldCapacity)
			{
				interimArmour.StatValue = maxShieldCapacity;
			}

			f.Events.OnInterimArmourChanged(entity, attacker, CurrentInterimArmour, CurrentInterimArmour,
												interimArmour.StatValue.AsInt);
		}

		/// <summary>
		/// Sets the entity health based on the given <paramref name="percentage"/> (between 0 - 1)
		/// </summary>
		internal void SetCurrentHealthPercentage(Frame f, EntityRef entity, EntityRef attacker, FP percentage)
		{
			var maxHealth = GetStatData(StatType.Health).StatValue;

			SetCurrentHealth(f, entity, attacker, FPMath.RoundToInt(maxHealth * FPMath.Clamp01(percentage)));
		}

		/// <summary>
		/// Sets the entity health based on the given <paramref name="amount"/> (between 0 - max health)
		/// </summary>
		internal void SetCurrentHealth(Frame f, EntityRef entity, EntityRef attacker, int amount)
		{
			var previousHealth = CurrentHealth;
			var maxHealth = GetStatData(StatType.Health).StatValue.AsInt;

			CurrentHealth = Math.Min(maxHealth, amount);
			CurrentHealth = Math.Max(CurrentHealth, 0);

			if (CurrentHealth != previousHealth && attacker != EntityRef.None)
			{
				f.Events.OnHealthChanged(entity, attacker, previousHealth, CurrentHealth, maxHealth);
				f.Signals.HealthChanged(entity, attacker, previousHealth);
			}
		}

		/// <summary>
		/// Gives the given health <paramref name="amount"/> to this <paramref name="entity"/> and notifies the change.
		/// This health gain was induced by the given <paramref name="attacker"/>.
		/// If the given <paramref name="attacker"/> equals <seealso cref="EntityRef.None"/> or invalid, then it is dead
		/// or non existent anymore.
		/// </summary>
		internal void GainHealth(Frame f, EntityRef entity, EntityRef attacker, uint amount)
		{
			SetCurrentHealth(f, entity, attacker, (int) (CurrentHealth + amount));
		}

		/// <summary>
		/// Reduces the given health <paramref name="damageAmount"/> to this <paramref name="entity"/> and notifies the change.
		/// First reduces the entity's armour before reducing it's health
		/// </summary>
		internal void ReduceHealth(Frame f, EntityRef entity, EntityRef attacker, uint damageAmount)
		{
			var currentDamageAmount = (int) damageAmount;
			var previousHealth = CurrentHealth;
			var maxHealth = Values[(int) StatType.Health].StatValue.AsInt;
			var previousInterimArmour = CurrentInterimArmour;
			var CurrentShieldCapacity = Values[(int)StatType.InterimArmour].StatValue.AsInt;

			if (IsImmune)
			{
				return;
			}

			// If there's Interim Armour then we reduce it first
			// and if the damage is bigger than armour then we proceed to remove health as well
			if (previousInterimArmour > 0)
			{
				CurrentInterimArmour = Math.Max(previousInterimArmour - currentDamageAmount, 0);
				currentDamageAmount = Math.Max(currentDamageAmount - previousInterimArmour, 0);

				f.Events.OnInterimArmourChanged(entity, attacker, previousInterimArmour, CurrentInterimArmour,
				                                CurrentShieldCapacity);
			}

			if (f.TryGet<PlayerCharacter>(entity, out var playerCharacter))
			{
				var armourDamage = damageAmount - (uint) currentDamageAmount;
				var healthDamage = (uint) currentDamageAmount;

				f.Events.OnPlayerDamaged(playerCharacter.Player, entity, attacker, armourDamage,
				                         healthDamage, damageAmount, maxHealth, CurrentShieldCapacity);
				f.Events.OnLocalPlayerDamaged(playerCharacter.Player, entity, attacker, armourDamage,
				                              healthDamage, damageAmount, maxHealth, CurrentShieldCapacity);
			}

			if (currentDamageAmount <= 0)
			{
				return;
			}

			SetCurrentHealth(f, entity, attacker, previousHealth - currentDamageAmount);

			if (CurrentHealth == 0)
			{
				f.Events.OnHealthIsZero(entity, attacker, (int) damageAmount, maxHealth);
				f.Signals.HealthIsZero(entity, attacker);
			}
		}

		/// <summary>
		/// Removes all modifiers, removes immunity, resets health and interim armour
		/// </summary>
		internal void ResetStats(Frame f, EntityRef entity)
		{
			CurrentStatusModifierDuration = FP._0;
			CurrentStatusModifierEndTime = FP._0;
			CurrentStatusModifierType = StatusModifierType.None;
			IsImmune = false;
			
			var list = f.ResolveList(Modifiers);
			for (var i = list.Count - 1; i > -1; i--)
			{
				RemoveModifier(f, list[i]);
			}
			list.Clear();
			
			SetCurrentHealthPercentage(f, entity, EntityRef.None, FP._1);
			SetInterimArmour(f, entity, EntityRef.None, 0);
		}
	}
}