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
		public Stats(FP baseHealth, FP basePower, FP baseSpeed, FP baseArmour, FP maxShields, FP startingShields, 
		             FP baseRange, FP basePickupSpeed, FP baseAmmoCapacity)
		{
			CurrentHealth = baseHealth.AsInt;
			CurrentShield = 0;
			CurrentAmmo = 0;
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
			Values[(int)StatType.AttackRange] = new StatData(baseRange, baseRange, StatType.AttackRange);
			Values[(int)StatType.PickupSpeed] = new StatData(basePickupSpeed, basePickupSpeed, StatType.PickupSpeed);
			Values[(int)StatType.AmmoCapacity] = new StatData(baseAmmoCapacity, baseAmmoCapacity, StatType.AmmoCapacity);
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
			var might = RefreshStats(f, weapon, gear);

			var newMaxHealth = GetStatData(StatType.Health).StatValue.AsInt;
			var newMaxShield = GetStatData(StatType.Shield).StatValue.AsInt;
			var newHealthAmount = Math.Min(CurrentHealth + Math.Max(newMaxHealth - previousMaxHeath, 0), newMaxHealth);
			var newShieldAmount = Math.Min(CurrentShield + Math.Max(newMaxShield - previousMaxShield, 0), newMaxShield);

			// Adapts the player health & shield if new equipment changes player's HP
			SetCurrentHealth(f, e, newHealthAmount);
			SetCurrentShield(f, e, newShieldAmount, previousMaxShield);
			
			f.Events.OnPlayerEquipmentStatsChanged(player, e, previousStats, this, might);
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
		/// Adds ammo to your pool where <paramref name="amount"/> is a % of your total ammo
		/// </summary>
		internal void GainAmmoPercent(Frame f, EntityRef e,FP amount)
		{
			var maxAmmo = GetStatData(StatType.AmmoCapacity).StatValue.AsInt;
			var player = f.Get<PlayerCharacter>(e);
			SetCurrentAmmo(f, player, e, CurrentAmmo + (maxAmmo * amount).AsInt);
		}

		/// <summary>
		/// Reduces the given ammo count by <paramref name="amount"/> of this <paramref name="e"/> player's entity
		/// </summary>
		internal void ReduceAmmo(Frame f, EntityRef e, int amount)
		{
			var player = f.Get<PlayerCharacter>(e);
			var maxAmmo = GetStatData(StatType.AmmoCapacity).BaseValue.AsInt;
			var magShotCount = player.GetMagShotCount(f, player.CurrentWeaponSlot, out var magSize);

			// Only consume a shot from the magazine if it has a magazine size and there is still ammo left in the mag
			if (magSize > 0 && magShotCount > 0)
			{
				player.WeaponSlots.GetPointer(player.CurrentWeaponSlot)->MagazineShotCount -= 1;
			}

			// Do not do reduce for melee weapons or if your weapon does not consume ammo
			if (!player.HasMeleeWeapon(f, e) || maxAmmo > 1)
			{
				SetCurrentAmmo(f, player, e, CurrentAmmo - amount);
			}
		}

		/// <summary>
		/// Set's the <paramref name="player"/>'s ammo count to <paramref name="value"/> clamped between 0 and MaxAmmo
		/// </summary>
		internal void SetCurrentAmmo(Frame f, PlayerCharacter player, EntityRef e, int value, bool ignoreClamp = false)
		{
			var previousAmmo = CurrentAmmo;
			var maxAmmo = GetStatData(StatType.AmmoCapacity).BaseValue.AsInt;
			var magSize = f.WeaponConfigs.GetConfig(player.CurrentWeapon.GameId).MagazineSize;

			CurrentAmmo = ignoreClamp ? value : FPMath.Clamp(value, 0, maxAmmo);

			if (CurrentAmmo != previousAmmo)
			{
				f.Events.OnPlayerAmmoChanged(player.Player, e, previousAmmo, CurrentAmmo, maxAmmo, CurrentAmmo / maxAmmo, magSize);
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
			var modifierPower = (FP) amount / maxShieldCapacity;
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
		internal void GainHealth(Frame f, EntityRef entity, Spell spell)
		{
			if (f.Has<EntityDestroyer>(entity) || f.Has<DeadPlayerCharacter>(entity))
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
			if (f.Has<EntityDestroyer>(entity) || f.Has<DeadPlayerCharacter>(entity))
			{
				return;
			}
			
			var previousHealth = CurrentHealth;
			var previousShield = CurrentShield;
			var maxHealth = GetStatData(StatType.Health).StatValue.AsInt;
			var maxShield = GetStatData(StatType.Shield).StatValue.AsInt;
			var armour = GetStatData(StatType.Armour).StatValue.AsInt;
			var totalDamage = Math.Max(0, ((FP._1 - (armour / FP._100)) * spell.PowerAmount).AsInt);
			var damageAmount = totalDamage;
			var shieldDamageAmount = 0;

			if (IsImmune || totalDamage <= 0)
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
				
				SetCurrentShield(f, entity, previousShield - shieldDamageAmount, GetStatData(StatType.Shield).StatValue.AsInt);
			}

			f.Events.OnPlayerDamaged(spell, totalDamage, shieldDamageAmount, Math.Min(previousHealth, damageAmount), 
			                         previousHealth, maxHealth, previousShield, maxShield);

			if (damageAmount <= 0)
			{
				return;
			}

			AttackerSetCurrentHealth(f, entity, spell.Attacker, previousHealth - damageAmount);
		}

		private void SetCurrentShield(Frame f, EntityRef entity, int amount, int previousShieldCapacity)
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

		private int RefreshStats(Frame f, Equipment weapon, FixedArray<Equipment> gear)
		{
			var maxShields = f.GameConfig.PlayerMaxShieldCapacity.Get(f);
			var startingShields = f.GameConfig.PlayerStartingShieldCapacity.Get(f);
			var modifiers = f.ResolveList(Modifiers);
			var weaponConfig = f.WeaponConfigs.GetConfig(weapon.GameId);
			
			GetLoadoutStats(f, weapon, gear, out var armour, out var health, out var speed, out var power, 
			                out var attackRange, out var pickupSpeed, out var ammoCapacity, out var shieldCapacity);
			
			var might = QuantumStatCalculator.GetTotalMight(f.StatConfigs.Dictionary, armour, health,speed, 
			                                                power, attackRange, pickupSpeed, ammoCapacity, shieldCapacity);
			
			//TODO: Move default (health, speed, shields) values into StatData configs
			health += f.GameConfig.PlayerDefaultHealth.Get(f);
			speed += f.GameConfig.PlayerDefaultSpeed.Get(f);
			ammoCapacity += weaponConfig.MaxAmmo.Get(f);
			maxShields += shieldCapacity.AsInt;
			startingShields += shieldCapacity.AsInt;
			
			// Melee weapons ignore Attack Range bonuses, sticking to base weapon value
			attackRange = weaponConfig.IsMeleeWeapon ? weaponConfig.AttackRange : attackRange + weaponConfig.AttackRange;
			
			Values[(int) StatType.Health] = new StatData(health, health, StatType.Health);
			Values[(int) StatType.Shield] = new StatData(maxShields, startingShields, StatType.Shield);
			Values[(int) StatType.Power] = new StatData(power, power, StatType.Power);
			Values[(int) StatType.Speed] = new StatData(speed, speed, StatType.Speed);
			Values[(int) StatType.Armour] = new StatData(armour, armour, StatType.Armour);
			Values[(int) StatType.AttackRange] = new StatData(attackRange, attackRange, StatType.AttackRange);
			Values[(int) StatType.PickupSpeed] = new StatData(pickupSpeed, pickupSpeed, StatType.PickupSpeed);
			Values[(int) StatType.AmmoCapacity] = new StatData(ammoCapacity, ammoCapacity, StatType.AmmoCapacity);

			foreach (var modifier in modifiers)
			{
				ApplyModifierUpdate(modifier, false);
			}

			return might;
		}
		
		private void GetLoadoutStats(Frame f, Equipment weapon, FixedArray<Equipment> gear, out int armour, 
		                             out int health, out FP speed, out FP power, out FP attackRange, out FP pickupSpeed,
		                             out FP ammoCapacity, out FP shieldCapacity)
		{
			QuantumStatCalculator.CalculateWeaponStats(f, weapon, out armour, out health, out speed, out power, 
			                                           out attackRange, out pickupSpeed, out ammoCapacity, out shieldCapacity);

			for (var i = 0; i < gear.Length; i++)
			{
				QuantumStatCalculator.CalculateGearStats(f, gear[i], out var armour2, out var health2, out var speed2, 
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