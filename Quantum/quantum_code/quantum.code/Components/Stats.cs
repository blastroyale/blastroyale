using System;
using Photon.Deterministic;
using Quantum.Collections;
using Quantum.Systems;

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
		public Stats(FP baseHealth, FP basePower, FP baseSpeed, FP baseArmour, FP maxShields, FP startingShields,
					 FP baseRange, FP basePickupSpeed, FP baseAmmoCapacity, int minimumHealth)
		{
			CurrentHealth = baseHealth.AsInt;
			CurrentShield = 0;
			CurrentAmmoPercent = 0;
			CurrentStatusModifierDuration = FP._0;
			CurrentStatusModifierEndTime = FP._0;
			CurrentStatusModifierType = StatusModifierType.None;
			IsImmune = false;
			ModifiersPtr = Ptr.Null;
			SpellEffectsPtr = Ptr.Null;
			MinimumHealth = minimumHealth;

			Values[(int)StatType.Health] = new StatData(baseHealth, baseHealth, StatType.Health);
			Values[(int)StatType.Shield] = new StatData(maxShields, startingShields, StatType.Shield);
			Values[(int)StatType.Power] = new StatData(basePower, basePower, StatType.Power);
			Values[(int)StatType.Speed] = new StatData(baseSpeed, baseSpeed, StatType.Speed);
			Values[(int)StatType.Armour] = new StatData(baseArmour, baseArmour, StatType.Armour);
			Values[(int)StatType.AttackRange] = new StatData(baseRange, baseRange, StatType.AttackRange);
			Values[(int)StatType.PickupSpeed] = new StatData(basePickupSpeed, basePickupSpeed, StatType.PickupSpeed);
			Values[(int)StatType.AmmoCapacity] = new StatData(baseAmmoCapacity, baseAmmoCapacity, StatType.AmmoCapacity);
		}

		/// <summary>
		/// Requests the <see cref="StatData"/> represented by the given <paramref name="stat"/>
		/// </summary>
		public StatData GetStatData(StatType stat)
		{
			return Values[(int)stat];
		}

		public static FP GetStat(Frame f, EntityRef entity, StatType stat)
		{
			if (!f.TryGet<Stats>(entity, out var stats))
			{
				return FP._0;
			}

			return stats[stat].StatValue;
		}

		/// <summary>
		/// Returns a range from 1.0 to 0.0 according to player health ratio
		/// </summary>
		public static FP HealthRatio(in EntityRef e, Frame f)
		{
			var health = Stats.GetStatData(f, e, StatType.Health);
			return (health.StatValue / health.BaseValue) * FP._100;
		}

		public static StatData GetStatData(Frame f, in EntityRef entity, StatType stat)
		{
			if (!f.TryGet<Stats>(entity, out var stats))
			{
				return default;
			}

			return stats[stat];
		}

		public StatData this[StatType stat]
		{
			get => GetStatData(stat);
		}

		public bool IsConsumableStatFilled(ConsumableType type)
		{
			switch (type)
			{
				case ConsumableType.Health:
					return CurrentHealth == GetStatData(StatType.Health).StatValue;
				case ConsumableType.Shield:
					return CurrentShield == GetStatData(StatType.Shield).StatValue;
				case ConsumableType.ShieldCapacity:
					return GetStatData(StatType.Shield).BaseValue ==
						GetStatData(StatType.Shield).StatValue &&
						CurrentShield == GetStatData(StatType.Shield).StatValue;
				case ConsumableType.Ammo:
					return CurrentAmmoPercent == 1;
				case ConsumableType.Energy:
					// I don't wanna deal with frames and players and energy is never filled anyway
					// return playerCharacter.GetEnergyLevel(f) == f.GameConfig.PlayerMaxEnergyLevel;
					return false;
			}

			return false;
		}

		public bool HasShield()
		{
			return CurrentShield > 0;
		}

		/// <summary>
		/// Removes all modifiers, removes immunity, resets health and shields
		/// </summary>
		internal void ResetStats(Frame f, Equipment weapon, Equipment[] gear, EntityRef e)
		{
			CurrentStatusModifierDuration = FP._0;
			CurrentStatusModifierEndTime = FP._0;
			CurrentStatusModifierType = StatusModifierType.None;
			CurrentShield = 0;
			IsImmune = false;

			var modifiersList = f.ResolveList(Modifiers);
			foreach (var modifier in modifiersList)
			{
				// We won't remove modifiers that are meant to stay forever.
				if (modifier.Duration != FP.MaxValue)
				{
					modifiersList.Remove(modifier);
				}
			}

			CalculateStatsFrom(f, ref weapon, gear, e);

			CurrentHealth = GetStatData(StatType.Health).StatValue.AsInt;
			if (CurrentAmmoPercent == 0)
			{
				CurrentAmmoPercent = Constants.INITIAL_AMMO_FILLED;
			}
		}

		/// <summary>
		/// Adds a new <paramref name="modifier"/> to this <paramref name="entity"/>'s stats data
		/// </summary>
		internal void AddModifier(Frame f, EntityRef entity, Modifier modifier)
		{
			ApplyModifierUpdate(&modifier, false);
			f.ResolveList(Modifiers).Add(modifier);

			f.Events.OnStatModifierAdded(entity);
		}

		/// <summary>
		/// Removes a modifier in the given modifiers list <paramref name="index"/> to this <paramref name="entity"/>'s stats data
		/// </summary>
		internal void RemoveModifier(Frame f, EntityRef entity, int index)
		{
			var list = f.ResolveList(Modifiers);
			var modifier = list[index];

			ApplyModifierUpdate(&modifier, true);

			list.RemoveAt(index);

			f.Events.OnStatModifierRemoved(entity);
		}

		/// <summary>
		/// Returns the current amount of ammo in whole display numbers rather than a percentage
		/// </summary>
		public int GetCurrentAmmo()
		{
			return FPMath.CeilToInt(CurrentAmmoPercent * GetStatData(StatType.AmmoCapacity).StatValue.AsInt);
		}

		/// <summary>
		/// Adds ammo to your pool where <paramref name="amount"/> is the % gain
		/// </summary>
		internal void GainAmmoPercent(Frame f, EntityRef e, FP amount)
		{
			var player = f.Unsafe.GetPointer<PlayerCharacter>(e);
			SetCurrentAmmo(f, player, e, CurrentAmmoPercent + amount);
		}

		/// <summary>
		/// Reduces the amount of ammo you have by <paramref name="numShots"/>
		/// </summary>
		internal void ReduceAmmo(Frame f, EntityRef e, int numShots)
		{
			var player = f.Unsafe.GetPointer<PlayerCharacter>(e);
			var weapon = f.WeaponConfigs.GetConfig(player->CurrentWeapon.GameId);

			// Do not do reduce if your weapon does not consume ammo
			if (weapon.MaxAmmo != -1)
			{
				// If it's a bot then don't reduce ammo below threshold
				if (f.Unsafe.TryGetPointer<BotCharacter>(e, out _) && CurrentAmmoPercent < Constants.BOT_AMMO_REDUCE_THRESHOLD)
				{
					return;
				}

				SetCurrentAmmo(f, player, e, (GetCurrentAmmo() - numShots) / GetStatData(StatType.AmmoCapacity).StatValue);
			}
		}

		/// <summary>
		/// Set's the <paramref name="player"/>'s ammo count to <paramref name="value"/> clamped between 0 and 1
		/// </summary>
		internal void SetCurrentAmmo(Frame f, PlayerCharacter* player, EntityRef e, FP value)
		{
			var previousAmmo = CurrentAmmoPercent;
			var maxAmmo = GetStatData(StatType.AmmoCapacity).StatValue.AsInt;
			var magSize = player->SelectedWeaponSlot->MagazineSize;
			var currentMag = player->SelectedWeaponSlot->MagazineShotCount;

			CurrentAmmoPercent = FPMath.Clamp(value, FP._0, FP._1);

			if (CurrentAmmoPercent != previousAmmo)
			{
				f.Events.OnPlayerAmmoChanged(player->Player, e, GetCurrentAmmo(), maxAmmo, currentMag, magSize);
			}
		}

		/// <summary>
		/// Gives the given shields <paramref name="amount"/> to this <paramref name="entity"/> and notifies the change.
		/// </summary>
		internal void GainShield(Frame f, EntityRef entity, int amount)
		{
			SetCurrentShield(f, entity, CurrentShield + amount, GetStatData(StatType.Shield).StatValue.AsInt);
		}

		/// <summary>
		/// Increases the max <paramref name="amount"/> of shield to this <paramref name="entity"/> and notifies the change.
		/// </summary>
		internal void GainShieldCapacity(Frame f, EntityRef entity, int amount)
		{
			var statData = GetStatData(StatType.Shield);
			var previousShieldCapacity = statData.StatValue;
			var maxShieldCapacity = statData.BaseValue;
			var modifierPower = (FP)amount / maxShieldCapacity;
			var newCapacityValue = previousShieldCapacity + (maxShieldCapacity * modifierPower);

			if (previousShieldCapacity.AsInt == maxShieldCapacity.AsInt)
			{
				SetCurrentShield(f, entity, CurrentShield + amount, previousShieldCapacity.AsInt);
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
				OpType = OperationType.Multiply,
				Power = modifierPower,
				Duration = FP.MaxValue,
				StartTime = FP._0,
				IsNegative = false
			};

			AddModifier(f, entity, modifier);
			SetCurrentShield(f, entity, CurrentShield + amount, previousShieldCapacity.AsInt);
		}

		/// <summary>
		/// Gives this entity the health based on the given `<paramref name="spell"/> 
		/// </summary>
		internal void GainHealth(Frame f, EntityRef entity, Spell* spell)
		{
			if (f.Has<EntityDestroyer>(entity) || f.Has<DeadPlayerCharacter>(entity))
			{
				return;
			}

			SetCurrentHealth(f, entity, (int)(CurrentHealth + spell->PowerAmount));
		}

		/// <summary>
		/// Instant kill an entity
		/// </summary>
		public void Kill(Frame f, EntityRef entityRef, bool canKnockOut = false)
		{
			Kill(f, entityRef, EntityRef.None, canKnockOut);
		}

		/// <summary>
		/// Instant kill an entity
		/// </summary>
		public void Kill(Frame f, EntityRef entityRef, EntityRef attacker, bool canKnockOut = false)
		{
			var spell = Spell.CreateInstant(f, entityRef, attacker, EntityRef.None, 9999, 0, FPVector3.Zero, 0);
			spell.Id = canKnockOut ? Spell.InstantKill : Spell.InstantKillWithoutKnockingOut;
			ReduceHealth(f, entityRef, &spell);
		}

		/// <summary>
		/// Reduces the health of this <paramref name="entity"/> based on the given <paramref name="spell"/> data
		/// </summary>
		internal void ReduceHealth(Frame f, EntityRef entity, Spell* spell)
		{
			if (f.Has<EntityDestroyer>(entity) || f.Has<DeadPlayerCharacter>(entity))
			{
				return;
			}

			var previousHealth = CurrentHealth;
			var previousShield = CurrentShield;
			var maxHealth = GetStatData(StatType.Health).StatValue.AsInt;
			var maxShield = GetStatData(StatType.Shield).StatValue.AsInt;
			var armour = GetStatData(StatType.Armour).StatValue.AsInt;

			var totalDamage = Math.Max(0, ((FP._1 - (armour / FP._100)) * spell->PowerAmount).AsInt);

			var damageAmount = totalDamage;

			// Knocked out players always take the same amount of damage
			// This code should not be here, but stats system will be deprecated very soon(TM)
			// And knocked out damage should be controlled by the revive system, it only overwrites projectile hits
			if (ReviveSystem.OverwriteDamage(f, entity, spell, maxHealth, ref damageAmount))
			{
				previousShield = 0;
			}

			var shieldDamageAmount = 0;

			if (!spell->IsInstantKill() && (IsImmune || totalDamage <= 0))
			{
				f.Events.OnDamageBlocked(entity);
				return;
			}


			// If there's shields and we do not ignore shields in damage, then we reduce it first
			if (previousShield > 0 && !spell->IgnoreShield && !spell->IsInstantKill())
			{
				shieldDamageAmount = Math.Min(previousShield, damageAmount);
				damageAmount = FPMath.Max(0, damageAmount - shieldDamageAmount).AsInt;

				// We don't do any damage to health if a player had at least 1 shields
				if (QuantumFeatureFlags.SHIELD_CRACKING)
				{
					damageAmount = 0;
				}

				SetCurrentShield(f, entity, previousShield - shieldDamageAmount, GetStatData(StatType.Shield).StatValue.AsInt);
			}


			if (f.TryGet<PlayerCharacter>(spell->Attacker, out var attacker))
			{
				f.Events.OnPlayerAttackHit(attacker.Player, spell->Attacker, attacker.TeamId, spell->Victim,
					spell->OriginalHitPosition, (uint)totalDamage, previousShield > 0);
			}

			f.Events.OnEntityDamaged(spell, totalDamage, shieldDamageAmount, Math.Min(previousHealth, damageAmount),
				previousHealth, maxHealth, previousShield, maxShield);

			if (damageAmount <= 0)
			{
				return;
			}

			AttackerSetCurrentHealth(f, entity, spell, previousHealth - damageAmount);
		}

		private void SetCurrentShield(Frame f, EntityRef entity, int amount, int previousShieldCapacity)
		{
			var previousShield = CurrentShield;
			var currentShieldCapacity = GetStatData(StatType.Shield).StatValue.AsInt;

			CurrentShield = amount > currentShieldCapacity ? currentShieldCapacity : amount;

			if (CurrentShield != previousShield || previousShieldCapacity != currentShieldCapacity)
			{
				f.Events.OnShieldChanged(entity, previousShield, CurrentShield, previousShieldCapacity, currentShieldCapacity);
			}
		}

		private void AttackerSetCurrentHealth(Frame f, EntityRef entity, Spell* spell, int amount)
		{
			var previousHealth = CurrentHealth;

			SetCurrentHealth(f, entity, amount);

			if (CurrentHealth != previousHealth && spell->Attacker != EntityRef.None)
			{
				f.Signals.HealthChangedFromAttacker(entity, spell->Attacker, previousHealth);
			}

			if (CurrentHealth == 0)
			{
				// If the player can be knocked out do not kill him yet
				if (spell->Id != Spell.InstantKillWithoutKnockingOut && ReviveSystem.KnockOutPlayer(f, entity, spell))
				{
					return;
				}

				ReviveSystem.OverwriteKiller(f, entity, spell);

				f.Signals.HealthIsZeroFromAttacker(entity, spell->Attacker, spell->Id == Spell.HeightDamageId);
				f.Events.OnHealthIsZeroFromAttacker(entity, spell->Attacker, amount, GetStatData(StatType.Health).StatValue.AsInt);
			}
		}

		private void SetCurrentHealth(Frame f, EntityRef e, int amount)
		{
			var previousHealth = CurrentHealth;
			var maxHealth = GetStatData(StatType.Health).StatValue.AsInt;

			CurrentHealth = Math.Min(maxHealth, amount);
			CurrentHealth = Math.Max(CurrentHealth, MinimumHealth);

			if (CurrentHealth != previousHealth)
			{
				f.Events.OnHealthChanged(e, previousHealth, CurrentHealth, maxHealth);
			}
		}

		public void SetCurrentHealthPercentage(Frame f, EntityRef e, FP percentage)
		{
			var health = FPMath.RoundToInt((FP)MaxHealth * percentage);
			SetCurrentHealth(f, e, health);
		}

		private int CalculateStatsFrom(Frame f, ref Equipment weapon, Equipment[] gear, EntityRef e)
		{
			var maxShields = f.GameConfig.PlayerMaxShieldCapacity.Get(f);
			var startingShields = f.GameConfig.PlayerStartingShieldCapacity.Get(f);
			var modifiers = f.ResolveList(Modifiers);
			var weaponConfig = f.WeaponConfigs.GetConfig(weapon.GameId);

			GetLoadoutStats(f, ref weapon, gear, e, out var armour, out var health, out var speed, out var power,
				out var attackRange, out var pickupSpeed, out var ammoCapacity, out var shieldCapacity);

			var might = QuantumStatCalculator.GetTotalMight(f.GameConfig, ref weapon, gear);

			//TODO: Move default (health, speed, shields) values into StatData configs
			health += f.GameConfig.PlayerDefaultHealth.Get(f);
			speed += f.GameConfig.PlayerDefaultSpeed.Get(f);

			maxShields += shieldCapacity.AsInt;
			startingShields += shieldCapacity.AsInt;

			// Melee weapons ignore Attack Range & ammo capacity bonuses, sticking to base weapon value
			attackRange = weaponConfig.IsMeleeWeapon ? weaponConfig.AttackRange : attackRange + weaponConfig.AttackRange;
			ammoCapacity = weaponConfig.IsMeleeWeapon ? weaponConfig.MaxAmmo : ammoCapacity + weaponConfig.MaxAmmo;

			Values[(int)StatType.Health] = new StatData(health, health, StatType.Health);
			Values[(int)StatType.Shield] = new StatData(maxShields, startingShields, StatType.Shield);
			Values[(int)StatType.Power] = new StatData(power, power, StatType.Power);
			Values[(int)StatType.Speed] = new StatData(speed, speed, StatType.Speed);
			Values[(int)StatType.Armour] = new StatData(armour, armour, StatType.Armour);
			Values[(int)StatType.AttackRange] = new StatData(attackRange, attackRange, StatType.AttackRange);
			Values[(int)StatType.PickupSpeed] = new StatData(pickupSpeed, pickupSpeed, StatType.PickupSpeed);
			Values[(int)StatType.AmmoCapacity] = new StatData(ammoCapacity, ammoCapacity, StatType.AmmoCapacity);

			foreach (var modifier in modifiers)
			{
				ApplyModifierUpdate(&modifier, false);
			}

			return might;
		}


		/// <summary>
		/// Refresh the <paramref name="player"/> stats based on the given loadout data
		/// </summary>
		internal void RefreshEquipmentStats(Frame f, PlayerRef player, EntityRef e, Equipment weapon, Equipment[] gear)
		{
			var previousStats = this;
			var previousMaxHeath = GetStatData(StatType.Health).StatValue.AsInt;
			var previousMaxShield = GetStatData(StatType.Shield).StatValue.AsInt;
			var might = CalculateStatsFrom(f, ref weapon, gear, e);

			var newMaxHealth = GetStatData(StatType.Health).StatValue.AsInt;
			var newHealthAmount = Math.Min(CurrentHealth + Math.Max(newMaxHealth - previousMaxHeath, 0), newMaxHealth);

			// Adapts the player health & shield if new equipment changes player's max HP or shields capacity
			SetCurrentHealth(f, e, newHealthAmount);
			SetCurrentShield(f, e, CurrentShield, previousMaxShield);

			f.Events.OnPlayerEquipmentStatsChanged(player, e, previousStats, this, might);
		}

		private void GetLoadoutStats(Frame f, ref Equipment weapon, Equipment[] gear, EntityRef e, out int armour,
									 out int health, out FP speed, out FP power, out FP attackRange, out FP pickupSpeed,
									 out FP ammoCapacity, out FP shieldCapacity)
		{
			QuantumStatCalculator.CalculateWeaponStats(f, weapon, out armour, out health, out speed, out power,
				out attackRange, out pickupSpeed, out ammoCapacity, out shieldCapacity);

			for (var i = 0; i < gear.Length; i++)
			{
				if (!gear[i].IsValid())
				{
					continue;
				}

				QuantumStatCalculator.CalculateGearStats(f, ref gear[i], out var armour2, out var health2, out var speed2,
					out var power2, out var attackRange2, out var pickupSpeed2,
					out var ammoCapacity2, out var shieldCapacity2);

				health += health2;
				speed += speed2;
				armour += armour2;
				power += power2;
				attackRange += attackRange2;
				pickupSpeed += pickupSpeed2;
				ammoCapacity += ammoCapacity2;
				shieldCapacity += shieldCapacity2;
			}
		}

		private void ApplyModifierUpdate(Modifier* modifier, bool toRemove)
		{
			var statData = Values[(int)modifier->Type];
			var multiplier = modifier->IsNegative ? -1 : 1;

			var additiveValue = modifier->OpType switch
			{
				OperationType.Add      => modifier->Power * multiplier,
				OperationType.Multiply => statData.BaseValue * modifier->Power * multiplier,
				_                      => statData.BaseValue * modifier->Power * multiplier
			};

			if (modifier->Type != StatType.Speed)
			{
				additiveValue = FPMath.CeilToInt(additiveValue);
			}

			statData.StatValue += toRemove ? additiveValue * -FP._1 : additiveValue;

			Values[(int)modifier->Type] = statData;
		}


		#region Helpers

		public int MaxHealth => GetStatData(StatType.Health).StatValue.AsInt;

		#endregion
	}
}