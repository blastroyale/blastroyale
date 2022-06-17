using System;
using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct PlayerCharacter
	{
		/// <summary>
		/// Requests the current weapon of player character
		/// </summary>
		public Equipment CurrentWeapon => Weapons[CurrentWeaponSlot];

		/// <summary>
		/// Marks that the player left the game
		/// </summary>
		public void PlayerLeft(Frame f, EntityRef e)
		{
			f.Add<EntityDestroyer>(e);

			f.Events.OnPlayerLeft(Player, e);
			f.Events.OnLocalPlayerLeft(Player);
		}

		/// <summary>
		/// Spawns this <see cref="PlayerCharacter"/> with all the necessary data.
		/// </summary>
		internal void Init(Frame f, EntityRef e, PlayerRef playerRef, Transform3D spawnPosition, uint playerLevel,
		                   uint trophies, GameId skin, Equipment[] startingEquipment, Equipment loadoutWeapon)
		{
			var blackboard = new AIBlackboardComponent();
			var kcc = new CharacterController3D();
			var transform = f.Unsafe.GetPointer<Transform3D>(e);

			Player = playerRef;
			CurrentWeaponSlot = 0;
			transform->Position = spawnPosition.Position;
			transform->Rotation = spawnPosition.Rotation;

			// I think this is silly, but "Hammer inherits ALL attributes from the Record" (record
			// being the attributes of your primary loadout weapon if you have one.
			if (loadoutWeapon.IsValid())
			{
				Weapons[Constants.WEAPON_INDEX_DEFAULT] = loadoutWeapon;
				Weapons[Constants.WEAPON_INDEX_DEFAULT].GameId = GameId.Hammer;
			}
			else
			{
				Weapons[Constants.WEAPON_INDEX_DEFAULT] = new Equipment(GameId.Hammer);
			}

			// This makes the entity debuggable in BotSDK. Access debugger inspector from circuit editor and see
			// a list of all currently registered entities and their states.
			//BotSDKDebuggerSystem.AddToDebugger(e);

			blackboard.InitializeBlackboardComponent(f, f.FindAsset<AIBlackboard>(BlackboardRef.Id));
			f.Unsafe.GetPointerSingleton<GameContainer>()->AddPlayer(f, playerRef, e, playerLevel, skin, trophies);
			kcc.Init(f, f.FindAsset<CharacterController3DConfig>(KccConfigRef.Id));

			f.Add(e, blackboard);
			f.Add(e, kcc);

			InitStats(f, e, startingEquipment);
			InitEquipment(f, e, startingEquipment);

			f.Add<HFSMAgent>(e);
			HFSMManager.Init(f, e, f.FindAsset<HFSMRoot>(HfsmRootRef.Id));

			f.Unsafe.GetPointer<PhysicsCollider3D>(e)->Enabled = false;
		}

		/// <summary>
		/// Spawns the player with it's initial default values
		/// </summary>
		internal void Spawn(Frame f, EntityRef e)
		{
			var isRespawning = f.GetSingleton<GameContainer>().PlayersData[Player].DeathCount > 0;

			if (isRespawning)
			{
				CurrentWeaponSlot = Constants.WEAPON_INDEX_DEFAULT;
			}

			EquipSlotWeapon(f, e, CurrentWeaponSlot, isRespawning);

			f.Events.OnPlayerSpawned(Player, e, isRespawning);
			f.Events.OnLocalPlayerSpawned(Player, e, isRespawning);

			f.Remove<DeadPlayerCharacter>(e);
		}

		/// <summary>
		/// Sets the player alive for the very first time after they have been spawned.
		/// </summary>
		internal void Activate(Frame f, EntityRef e)
		{
			var targetable = new Targetable {Team = Player + (int) TeamType.TOTAL};
			var stats = f.Unsafe.GetPointer<Stats>(e);

			stats->ResetStats(f, e);

			var maxHealth = FPMath.RoundToInt(stats->GetStatData(StatType.Health).StatValue);
			var currentHealth = stats->CurrentHealth;

			f.Add(e, targetable);
			f.Add<AlivePlayerCharacter>(e);

			f.Events.OnPlayerAlive(Player, e, currentHealth, FPMath.RoundToInt(maxHealth));
			f.Events.OnLocalPlayerAlive(Player, e, currentHealth, FPMath.RoundToInt(maxHealth));

			f.Unsafe.GetPointer<PhysicsCollider3D>(e)->Enabled = true;

			StatusModifiers.AddStatusModifierToEntity(f, e, StatusModifierType.Shield,
			                                          f.GameConfig.PlayerAliveShieldDuration);
		}

		/// <summary>
		/// Kills this <see cref="PlayerCharacter"/> and mark it as done for the session
		/// </summary>
		internal void Dead(Frame f, EntityRef e, PlayerRef killerPlayer, EntityRef attacker)
		{
			f.Unsafe.GetPointer<PhysicsCollider3D>(e)->Enabled = false;

			var deadPlayer = new DeadPlayerCharacter
			{
				TimeOfDeath = f.Time,
				Killer = killerPlayer,
				KillerEntity = attacker
			};

			f.Unsafe.GetPointer<Stats>(e)->SetCurrentHealthPercentage(f, e, attacker, FP._0);

			// If an entity has NavMeshPathfinder then we stop the movement in case an entity was moving
			if (f.Unsafe.TryGetPointer<NavMeshPathfinder>(e, out var navMeshPathfinder))
			{
				navMeshPathfinder->Stop(f, e, true);
			}

			if (f.Context.MapConfig.GameMode == GameMode.BattleRoyale)
			{
				f.Add<EntityDestroyer>(e);
			}

			f.Add(e, deadPlayer);

			f.Events.OnPlayerDead(Player, e);
			f.Events.OnLocalPlayerDead(Player, killerPlayer, attacker);

			f.Remove<Targetable>(e);
			f.Remove<AlivePlayerCharacter>(e);

			var agent = f.Unsafe.GetPointer<HFSMAgent>(e);
			HFSMManager.TriggerEvent(f, &agent->Data, e, Constants.DeadEvent);
		}

		/// <summary>
		/// Adds a <paramref name="weapon"/> to the player's weapon slots
		/// </summary>
		internal void AddWeapon(Frame f, EntityRef e, Equipment weapon, bool primary)
		{
			var slot = primary ? Constants.WEAPON_INDEX_PRIMARY : Constants.WEAPON_INDEX_SECONDARY;

			var primaryReplaced = false;
			var primaryWeapon = Weapons[Constants.WEAPON_INDEX_PRIMARY];
			if (primaryWeapon.IsValid() && weapon.GameId == primaryWeapon.GameId &&
			    weapon.Rarity > primaryWeapon.Rarity)
			{
				slot = Constants.WEAPON_INDEX_PRIMARY;
				primaryReplaced = true;
			}

			// In Battle Royale if there's a different weapon in a slot then we drop it
			if (f.Context.MapConfig.GameMode == GameMode.BattleRoyale && Weapons[slot].IsValid()
			                                                          && (Weapons[slot].GameId != weapon.GameId ||
			                                                              primaryReplaced))
			{
				var dropPosition = f.Get<Transform3D>(e).Position + FPVector3.Forward;
				Collectable.DropEquipment(f, Weapons[slot], dropPosition, 0);
			}

			Weapons[slot] = weapon;
			CurrentWeaponSlot = slot;

			GainAmmo(f, e, f.WeaponConfigs.GetConfig(weapon.GameId).InitialAmmoFilled.Get(f));

			f.Events.OnLocalPlayerWeaponAdded(Player, e, weapon, slot);
		}

		/// <summary>
		/// Sets the player's weapon to the given <paramref name="slot"/>
		/// </summary>
		internal void EquipSlotWeapon(Frame f, EntityRef e, int slot, bool triggerEvents = true)
		{
			CurrentWeaponSlot = slot;

			var blackboard = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			var weapon = CurrentWeapon;

			RefreshStats(f, e);

			var weaponConfig = f.WeaponConfigs.GetConfig(weapon.GameId);
			//the total time it takes for a burst to complete should be half of the weapon's cooldown
			//if we are only firing one shot, burst interval is 0
			var burstCooldown = weaponConfig.NumberOfBursts == 1 ? 
				0 : (weaponConfig.AttackCooldown /2) / weaponConfig.NumberOfBursts;

			blackboard->Set(f, nameof(QuantumWeaponConfig.AimTime), weaponConfig.AimTime);
			blackboard->Set(f, nameof(QuantumWeaponConfig.AttackCooldown), weaponConfig.AttackCooldown);
			blackboard->Set(f, nameof(QuantumWeaponConfig.AimingMovementSpeed), weaponConfig.AimingMovementSpeed);
			blackboard->Set(f, nameof(QuantumWeaponConfig.NumberOfBursts), weaponConfig.NumberOfBursts);
			blackboard->Set(f, Constants.HasMeleeWeaponKey, weaponConfig.IsMeleeWeapon);
			blackboard->Set(f, Constants.BurstTimeDelay, burstCooldown);

			if (triggerEvents)
			{
				f.Events.OnPlayerWeaponChanged(Player, e, weapon);
				f.Events.OnLocalPlayerWeaponChanged(Player, e, weapon, slot);
			}

			// TODO: Specials should have charges and remember charges used for each weapon
			for (var i = 0; i < Constants.MAX_SPECIALS; i++)
			{
				var specialId = weaponConfig.Specials[i];

				if (specialId == default)
				{
					continue;
				}

				var specialConfig = f.SpecialConfigs.GetConfig(specialId);

				Specials[i] = new Special(f, specialConfig);
			}
		}

		/// <summary>
		/// Requests the total amount of ammo the <paramref name="e"/> player has
		/// </summary>
		public int GetAmmoAmount(Frame f, EntityRef e, out int maxAmmo)
		{
			maxAmmo = f.WeaponConfigs.GetConfig(Weapons[CurrentWeaponSlot].GameId).MaxAmmo.Get(f);

			return FPMath.FloorToInt(GetAmmoAmountFilled(f, e) * maxAmmo);
		}

		/// <summary>
		/// Requests the total amount of ammo the <paramref name="e"/> player has
		/// </summary>
		public FP GetAmmoAmountFilled(Frame f, EntityRef e)
		{
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);

			return bb->GetFP(f, Constants.AmmoFilledKey);
		}

		/// <summary>
		/// Requests if the current weapon equipped by the player is empty of ammo or not.
		/// </summary>
		/// <remarks>
		/// It will be always false for melee weapons. Use <see cref="HasMeleeWeapon"/> to double check the state.
		/// </remarks>
		public bool IsAmmoEmpty(Frame f, EntityRef e)
		{
			return !HasMeleeWeapon(f, e) && GetAmmoAmountFilled(f, e) < FP.SmallestNonZero;
		}

		/// <summary>
		/// Requests if the current weapon equipped by the player is a melee weapon or not
		/// </summary>
		public bool HasMeleeWeapon(Frame f, EntityRef e)
		{
			return f.Get<AIBlackboardComponent>(e).GetBoolean(f, Constants.HasMeleeWeaponKey);
		}

		/// <summary>
		/// Adds the given ammo <paramref name="amount"/> of this <paramref name="e"/> player's entity
		/// </summary>
		internal void GainAmmo(Frame f, EntityRef e, FP amount)
		{
			var ammo = GetAmmoAmount(f, e, out var maxAmmo);
			var newAmmoFilled = FPMath.Min(GetAmmoAmountFilled(f, e) + amount, FP._1);
			var newAmmo = FPMath.FloorToInt(newAmmoFilled * maxAmmo);

			f.Unsafe.GetPointer<AIBlackboardComponent>(e)->Set(f, Constants.AmmoFilledKey, newAmmoFilled);

			if (HasMeleeWeapon(f, e) || ammo == newAmmo)
			{
				return;
			}

			f.Events.OnPlayerAmmoChanged(Player, e, ammo, newAmmo, maxAmmo);
			f.Events.OnLocalPlayerAmmoChanged(Player, e, ammo, newAmmo, maxAmmo);
		}

		/// <summary>
		/// Reduces the given ammo <paramref name="amount"/> of this <paramref name="e"/> player's entity
		/// </summary>
		internal void ReduceAmmo(Frame f, EntityRef e, uint amount)
		{
			// Do not do reduce for melee weapons or if your weapon is empty
			if (HasMeleeWeapon(f, e) || IsAmmoEmpty(f, e))
			{
				return;
			}

			var ammo = GetAmmoAmount(f, e, out var maxAmmo); // Gives back Int floored down (filledFP * maxAmmo)
			var newAmmo = Math.Max(ammo - (int) amount, 0);
			var currentAmmo = Math.Min(newAmmo, maxAmmo);
			var finalAmmoFilled = FPMath.Max(GetAmmoAmountFilled(f, e) - ((FP._1 / maxAmmo) * amount), FP._0);

			f.Unsafe.GetPointer<AIBlackboardComponent>(e)->Set(f, Constants.AmmoFilledKey, finalAmmoFilled);
			f.Events.OnPlayerAmmoChanged(Player, e, ammo, currentAmmo, maxAmmo);
			f.Events.OnLocalPlayerAmmoChanged(Player, e, ammo, currentAmmo, maxAmmo);
		}

		private void InitStats(Frame f, EntityRef e, Equipment[] equipment)
		{
			QuantumStatCalculator.CalculateStats(f, equipment, out var armour, out var health,
			                                     out var speed, out var power);

			f.Add(e, new Stats(f.GameConfig.PlayerDefaultHealth.Get(f) + health,
			                   power,
			                   f.GameConfig.PlayerDefaultSpeed.Get(f) + speed,
			                   armour,
			                   f.GameConfig.PlayerMaxShieldCapacity.Get(f),
			                   f.GameConfig.PlayerStartingShieldCapacity.Get(f)));
		}

		private void RefreshStats(Frame f, EntityRef e)
		{
			QuantumStatCalculator.CalculateStats(f, CurrentWeapon, Gear, out var armour, out var health,
			                                     out var speed,
			                                     out var power);

			health += f.GameConfig.PlayerDefaultHealth.Get(f);
			speed += f.GameConfig.PlayerDefaultSpeed.Get(f);

			var maxShields = f.GameConfig.PlayerMaxShieldCapacity.Get(f);
			var startingShields = f.GameConfig.PlayerStartingShieldCapacity.Get(f);
			
			var stats = f.Unsafe.GetPointer<Stats>(e);
			stats->Values[(int) StatType.Armour] = new StatData(armour, armour, StatType.Armour);
			stats->Values[(int) StatType.Health] = new StatData(health, health, StatType.Health);
			stats->Values[(int) StatType.Speed] = new StatData(speed, speed, StatType.Speed);
			stats->Values[(int) StatType.Power] = new StatData(power, power, StatType.Power);
			stats->Values[(int) StatType.Shield] = new StatData(maxShields, startingShields, StatType.Shield);
			stats->ApplyModifiers(f);
		}

		private void InitEquipment(Frame f, EntityRef e, Equipment[] equipment)
		{
			int gearIndex = 0;
			foreach (var item in equipment)
			{
				if (item.IsWeapon())
				{
					AddWeapon(f, e, item, true);
				}
				else
				{
					Gear[gearIndex++] = item;
				}
			}
		}
	}
}