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
			IsImmune = false;
			CurrentHealth = int.MaxValue;
			CurrentShield = 0;
			
			f.ResolveList(Modifiers).Clear();
			RefreshStats(f, weapon, gear);
		}

		/// <summary>
		/// Refresh the player stats with the given data
		/// </summary>
		internal void RefreshStats(Frame f, Equipment weapon, FixedArray<Equipment> gear)
		{
			var maxShields = f.GameConfig.PlayerMaxShieldCapacity.Get(f);
			var startingShields = f.GameConfig.PlayerStartingShieldCapacity.Get(f);
			var modifiers = f.ResolveList(Modifiers);
			
			QuantumStatCalculator.CalculateStats(f, weapon, gear, out var armour, out var health,
			                                     out var speed, out var power);
			
			health += f.GameConfig.PlayerDefaultHealth.Get(f);
			speed += f.GameConfig.PlayerDefaultSpeed.Get(f);
			
			CurrentHealth = Math.Min(CurrentHealth, health);
			CurrentShield = Math.Min(CurrentShield, maxShields);
			Values[(int) StatType.Health] = new StatData(health, health, StatType.Health);
			Values[(int) StatType.Shield] = new StatData(maxShields, startingShields, StatType.Shield);
			Values[(int) StatType.Power] = new StatData(power, power, StatType.Power);
			Values[(int) StatType.Speed] = new StatData(speed, speed, StatType.Speed);
			Values[(int) StatType.Armour] = new StatData(armour, armour, StatType.Armour);

			foreach (var modifier in modifiers)
			{
				ApplyModifier(modifier);
			}
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
			ApplyModifier(modifier);
			f.ResolveList(Modifiers).Add(modifier);
		}

		/// <summary>
		/// Sets the entity shields based on the given <paramref name="amount"/>
		/// </summary>
		internal void SetShields(Frame f, EntityRef entity, EntityRef attacker, int amount)
		{
			var previousShield = CurrentShield;
			var currentShieldCapacity = Values[(int)StatType.Shield].StatValue.AsInt;

			CurrentShield = amount > currentShieldCapacity
									   ? currentShieldCapacity
									   : amount;
			
			if (CurrentShield != previousShield)
			{
				f.Events.OnShieldChanged(entity, attacker, previousShield, CurrentShield,
				                         currentShieldCapacity, currentShieldCapacity);
			}
		}

		/// <summary>
		/// Gives the given shields <paramref name="amount"/> to this <paramref name="entity"/> and notifies the change.
		/// This shield gain was induced by the given <paramref name="attacker"/>.
		/// If the given <paramref name="attacker"/> equals <seealso cref="EntityRef.None"/> or invalid, then it is dead
		/// or non existent anymore.
		/// </summary>
		internal void GainShields(Frame f, EntityRef entity, EntityRef attacker, int amount)
		{
			SetShields(f, entity, attacker, CurrentShield + amount);
		}

		/// <summary>
		/// Adds <paramref name="amount"/> of shield capacity as a stat modifier as <paramref name="entity"/> and notifies the change.
		/// This shield capacity gain was induced by the given <paramref name="attacker"/>.
		/// If the given <paramref name="attacker"/> equals <seealso cref="EntityRef.None"/> or invalid, then it is dead
		/// or non existent anymore.
		/// </summary>
		internal void GainShieldCapacity(Frame f, EntityRef entity, EntityRef attacker, int amount)
		{
			var shield = Values[(int)StatType.Shield];
			var currentShieldCapacity = shield.StatValue;
			var maxShieldCapacity = shield.BaseValue;

			if (currentShieldCapacity.AsInt == maxShieldCapacity.AsInt)
			{
				return;
			}

			var modifierId = ++f.Global->ModifierIdCount;
			var modifierPower = (FP) amount / maxShieldCapacity;
			var newCapacityValue = currentShieldCapacity + (maxShieldCapacity * modifierPower);
			if (newCapacityValue > maxShieldCapacity)
			{
				newCapacityValue = maxShieldCapacity;
				modifierPower = (maxShieldCapacity - currentShieldCapacity) / maxShieldCapacity;
			}

			var capacityModifer = new Modifier
			{
				Id = modifierId,
				Type = StatType.Shield,
				Power = modifierPower,
				Duration = FP.MaxValue,
				StartTime = FP._0,
				IsNegative = false
			};

			AddModifier(f, capacityModifer);
			f.Events.OnShieldChanged(entity, attacker, CurrentShield, CurrentShield,
			                         currentShieldCapacity.AsInt, newCapacityValue.AsInt);
		}

		/// <summary>
		/// Sets the entity health based on the given <paramref name="percentage"/> (between 0 - 1)
		/// </summary>
		/// <remarks>
		/// IMPORTANT: Only use this call to update player health values without killing the player.
		/// To kill the player call <see cref="AttackerSetCurrentHealth"/> instead
		/// </remarks>
		internal void SetCurrentHealthPercentage(Frame f, EntityRef entity, FP percentage)
		{
			var maxHealth = GetStatData(StatType.Health).StatValue;

			SetCurrentHealth(f, entity, FPMath.RoundToInt(maxHealth * FPMath.Clamp01(percentage)));
		}

		/// <summary>
		/// Sets the entity health based on the given <paramref name="amount"/> (between 0 - max health)
		/// </summary>
		internal void AttackerSetCurrentHealth(Frame f, EntityRef entity, EntityRef attacker, int amount)
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

		/// <summary>
		/// Sets the entity health based on the given <paramref name="amount"/> (between 0 - max health)
		/// </summary>
		/// <remarks>
		/// IMPORTANT: Only use this call to update player health values without killing the player.
		/// To kill the player call <see cref="AttackerSetCurrentHealth"/> instead
		/// </remarks>
		internal void SetCurrentHealth(Frame f, EntityRef e, int amount)
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

		/// <summary>
		/// Gives this entity the health based on the given `<paramref name="spell"/> 
		/// </summary>
		internal void GainHealth(Frame f, Spell spell)
		{
			SetCurrentHealth(f, spell.Victim, (int) (CurrentHealth + spell.PowerAmount));
		}

		/// <summary>
		/// Gives the given health <paramref name="amount"/> to this <paramref name="entity"/> and notifies the change
		/// based on the given data
		/// </summary>
		internal void GainHealth(Frame f, EntityRef entity, uint amount)
		{
			SetCurrentHealth(f, entity, (int) (CurrentHealth + amount));
		}

		/// <summary>
		/// Reduces the health of this entity based on the given <paramref name="spell"/> data
		/// </summary>
		internal void ReduceHealth(Frame f, Spell spell)
		{
			var entity = spell.Victim;
			var previousHealth = CurrentHealth;
			var previousShield = CurrentShield;
			var maxHealth = GetStatData(StatType.Health).StatValue.AsInt;
			var currentShieldCapacity = GetStatData(StatType.Shield).StatValue.AsInt;
			var armour = GetStatData(StatType.Armour).StatValue.AsInt;
			var currentDamageAmount = Math.Max((int)spell.PowerAmount, 0);

			if (IsImmune)
			{
				f.Events.OnDamageBlocked(entity);
				return;
			}

			currentDamageAmount = Math.Max(currentDamageAmount - armour, 0);

			// If there's shields then we reduce it first
			// and if the damage is bigger than shields then we proceed to remove health as well
			if (previousShield > 0)
			{
				CurrentShield = Math.Max(previousShield - currentDamageAmount, 0);
				currentDamageAmount = Math.Max(currentDamageAmount - previousShield, 0);

				f.Events.OnShieldChanged(entity, spell.Attacker, previousShield, CurrentShield,
				                         currentShieldCapacity, currentShieldCapacity);
			}

			if (f.TryGet<PlayerCharacter>(entity, out var playerCharacter))
			{
				var shieldDamage = spell.PowerAmount - (uint) currentDamageAmount;
				var healthDamage = (uint) currentDamageAmount;

				f.Events.OnPlayerDamaged(playerCharacter.Player, entity, spell.Attacker, shieldDamage,
				                         healthDamage, spell.PowerAmount, maxHealth, currentShieldCapacity,
				                         spell.OriginalHitPosition);
				f.Events.OnLocalPlayerDamaged(playerCharacter.Player, entity, spell.Attacker, shieldDamage,
				                              healthDamage, spell.PowerAmount, maxHealth, currentShieldCapacity,
				                              spell.OriginalHitPosition);
			}

			if (currentDamageAmount <= 0)
			{
				return;
			}

			AttackerSetCurrentHealth(f, entity, spell.Attacker, previousHealth - currentDamageAmount);
		}

		private void ApplyModifier(Modifier modifier)
		{
			var statData = Values[(int) modifier.Type];
			var multiplier = modifier.IsNegative ? -1 : 1;
			var ceilToInt = false;
			var additiveValue = statData.BaseValue * modifier.Power * multiplier;

			switch (modifier.Type)
			{
				case StatType.Armour:
				case StatType.Health:
				case StatType.Power:
				case StatType.Shield:
					ceilToInt = true;
					break;
				case StatType.Speed:
					ceilToInt = false;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(modifier.Type), modifier.Type, null);
			}

			statData.StatValue += ceilToInt ? FPMath.CeilToInt(additiveValue) : additiveValue;

			Values[(int) modifier.Type] = statData;
		}
	}
}