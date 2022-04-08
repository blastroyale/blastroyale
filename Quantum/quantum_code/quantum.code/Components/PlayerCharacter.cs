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
		                   uint trophies, GameId skin, Equipment playerWeapon, Equipment[] playerGear)
		{
			var blackboard = new AIBlackboardComponent();
			var kcc = new CharacterController3D();
			Player = playerRef;
			CurrentWeaponSlot = 0;
			Weapons[0] = new Equipment(GameId.Hammer, ItemRarity.Common, ItemAdjective.Cool, ItemMaterial.Bronze, 
			                           ItemManufacturer.Military, ItemFaction.Order, 1, 5);
			
			blackboard.InitializeBlackboardComponent(f, f.FindAsset<AIBlackboard>(BlackboardRef.Id));
			f.Unsafe.GetPointerSingleton<GameContainer>()->AddPlayer(f, playerRef, e, playerLevel, skin, trophies);
			kcc.Init(f, f.FindAsset<CharacterController3DConfig>(KccConfigRef.Id));

			f.Add(e, blackboard);
			f.Add(e, kcc);

			if (!f.WeaponConfigs.GetConfig(playerWeapon.GameId).IsMeleeWeapon)
			{
				AddWeapon(f, e, playerWeapon);
			}

			InitStats(f, e, playerGear);
			Spawn(f, e, spawnPosition, false);
		}

		/// <summary>
		/// Spawns the player with it's initial default values
		/// </summary>
		internal void Spawn(Frame f, EntityRef e, Transform3D spawnPosition, bool isRespawning)
		{
			var transform = f.Unsafe.GetPointer<Transform3D>(e);

			transform->Position = spawnPosition.Position;
			transform->Rotation = spawnPosition.Rotation;

			EquipSlotWeapon(f, e, CurrentWeaponSlot);

			f.Events.OnPlayerSpawned(Player, e, isRespawning);
			f.Events.OnLocalPlayerSpawned(Player, e, isRespawning);

			f.Remove<DeadPlayerCharacter>(e);

			// Init HFSM
			f.Add<HFSMAgent>(e);
			HFSMManager.Init(f, e, f.FindAsset<HFSMRoot>(HfsmRootRef.Id));
		}

		/// <summary>
		/// Sets the player alive for the very first time after they have been spawned.
		/// </summary>
		internal void Activate(Frame f, EntityRef e)
		{
			var targetable = new Targetable { Team = Player + (int) TeamType.TOTAL };
			
			f.Unsafe.GetPointer<Stats>(e)->SetCurrentHealth(f, e, EntityRef.None, FP._1);
			
			f.Add(e, targetable);
			f.Add<AlivePlayerCharacter>(e);

			f.Events.OnPlayerAlive(Player, e);
			f.Events.OnLocalPlayerAlive(Player, e);
		}

		/// <summary>
		/// Kills this <see cref="PlayerCharacter"/> and mark it as done for the session
		/// </summary>
		internal void Dead(Frame f, EntityRef e, PlayerRef killerPlayer, EntityRef attacker)
		{
			var deadPlayer = new DeadPlayerCharacter
			{
				TimeOfDeath = f.Time,
				Killer = killerPlayer,
				KillerEntity = attacker
			};
			
			f.Unsafe.GetPointer<Stats>(e)->SetCurrentHealth(f, e, attacker, FP._0);
			
			// If an entity has NavMeshPathfinder then we stop the movement in case an entity was moving
			if (f.Unsafe.TryGetPointer<NavMeshPathfinder>(e, out var navMeshPathfinder))
			{
				navMeshPathfinder->Stop(f, e, true);
			}

			if (f.RuntimeConfig.GameMode == GameMode.BattleRoyale)
			{
				f.Add<EntityDestroyer>(e);
			}

			f.Add(e, deadPlayer);

			f.Events.OnPlayerDead(Player, e);
			f.Events.OnLocalPlayerDead(Player, killerPlayer, attacker);

			f.Remove<Targetable>(e);
			f.Remove<HFSMAgent>(e);
			f.Remove<AlivePlayerCharacter>(e);
		}

		/// <summary>
		/// Adds a <paramref name="weapon"/> to the player's weapon slots
		/// </summary>
		internal void AddWeapon(Frame f, EntityRef e, Equipment weapon)
		{
			var slot = Weapons[1].IsValid && Weapons[1].GameId != weapon.GameId ? 2 : 1;

			Weapons[slot] = weapon;
			CurrentWeaponSlot = slot;

			GainAmmo(f, e, f.WeaponConfigs.GetConfig(weapon.GameId).InitialAmmoFilled);

			f.Events.OnLocalPlayerWeaponAdded(Player, e, weapon, slot);
		}

		/// <summary>
		/// Sets the player's weapon to the given <paramref name="slot"/>
		/// </summary>
		internal void EquipSlotWeapon(Frame f, EntityRef e, int slot)
		{
			CurrentWeaponSlot = slot;

			var blackboard = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			var weapon = CurrentWeapon;
			var weaponConfig = f.WeaponConfigs.GetConfig(weapon.GameId);
			var stats = f.Unsafe.GetPointer<Stats>(e);
			var power = QuantumStatCalculator.CalculateStatValue(weapon.Rarity, weaponConfig.PowerRatioToBase,
			                                                     weapon.Level, f.GameConfig, StatType.Power);

			stats->Values[(int) StatType.Power] = new StatData(power, power, StatType.Power);
			blackboard->Set(f, nameof(QuantumWeaponConfig.AimTime), weaponConfig.AimTime);
			blackboard->Set(f, nameof(QuantumWeaponConfig.AttackCooldown), weaponConfig.AttackCooldown);
			blackboard->Set(f, nameof(QuantumWeaponConfig.AimingMovementSpeed), weaponConfig.AimingMovementSpeed);
			blackboard->Set(f, Constants.HasMeleeWeaponKey, weaponConfig.IsMeleeWeapon);

			f.Events.OnPlayerWeaponChanged(Player, e, weapon);
			f.Events.OnLocalPlayerWeaponChanged(Player, e, weapon);

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
			maxAmmo = f.WeaponConfigs.GetConfig(Weapons[CurrentWeaponSlot].GameId).MaxAmmo;

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

		private void InitStats(Frame f, EntityRef e, Equipment[] playerGear)
		{
			QuantumStatCalculator.CalculateStats(f, playerGear, out var armour, out var health, out var speed);

			health += f.GameConfig.PlayerDefaultHealth;
			speed += f.GameConfig.PlayerDefaultSpeed;

			f.Add(e, new Stats(health, 0, speed, armour, f.GameConfig.PlayerDefaultInterimArmour));
		}
	}
}
