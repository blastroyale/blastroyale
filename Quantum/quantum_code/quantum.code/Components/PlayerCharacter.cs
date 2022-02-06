using System;
using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct PlayerCharacter
	{
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
		internal void Init(Frame f, EntityRef e, PlayerRef playerRef, Transform3D spawnPosition,
		                   uint playerLevel, GameId skin, Equipment playerWeapon, Equipment[] playerGear)
		{
			var blackboard = new AIBlackboardComponent();
			
			Player = playerRef;
			DefaultWeapon = playerWeapon;
			
			blackboard.InitializeBlackboardComponent(f, f.FindAsset<AIBlackboard>(BlackboardRef.Id));
			f.Unsafe.GetPointerSingleton<GameContainer>()->AddPlayer(f, playerRef, e, playerLevel, skin);
			
			f.Add(e, blackboard);
			
			InitStats(f, e, playerGear);
			Spawn(f, e, spawnPosition, false);
		}

		/// <summary>
		/// Spawns the player with it's initial default values
		/// </summary>
		internal void Spawn(Frame f, EntityRef e, Transform3D spawnPosition, bool isRespawning)
		{
			var spawnPlayer = new SpawnPlayerCharacter { EndSpawnTime = f.Time + SpawnTime };
			var transform = f.Unsafe.GetPointer<Transform3D>(e);
			
			transform->Position = spawnPosition.Position;
			transform->Rotation = spawnPosition.Rotation;
			
			SetWeapon(f, e, DefaultWeapon.GameId, DefaultWeapon.Rarity, DefaultWeapon.Level);
			
			f.Events.OnPlayerSpawned(Player, e, isRespawning);
			f.Events.OnLocalPlayerSpawned(Player, e, isRespawning, DefaultWeapon.GameId);
			
			f.Remove<DeadPlayerCharacter>(e);
			f.Add(e, spawnPlayer);
		}

		/// <summary>
		/// Sets the player alive for the very first time after they have been spawned.
		/// </summary>
		internal void Activate(Frame f, EntityRef e)
		{
			var targetable = new Targetable { Team = Player + (int) TeamType.TOTAL };
			
			f.Unsafe.GetPointer<Stats>(e)->SetCurrentHealth(f, e, FP._1);
			
			f.Add(e, targetable);
			f.Add<HFSMAgent>(e);
			f.Add<AlivePlayerCharacter>(e);
			
			HFSMManager.Init(f, e, f.FindAsset<HFSMRoot>(HfsmRootRef.Id));
			StatusModifiers.AddStatusModifierToEntity(f, e, StatusModifierType.Shield, f.GameConfig.PlayerAliveShieldDuration);
			
			f.Events.OnPlayerAlive(Player, e);
			f.Events.OnLocalPlayerAlive(Player, e);
			
			f.Remove<SpawnPlayerCharacter>(e);
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
			
			f.Unsafe.GetPointer<Stats>(e)->SetCurrentHealth(f, e, FP._0);
			
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
		/// Set's the player's current weapon to the given <paramref name="weaponGameId"/> and data
		/// </summary>
		internal void SetWeapon(Frame f, EntityRef e, GameId weaponGameId, ItemRarity rarity, uint level, 
		                      FPVector3 projectileSpawnOffset = new FPVector3())
		{
			var weapon = new Weapon();
			var blackboard = f.Get<AIBlackboardComponent>(e);
			var weaponConfig = f.WeaponConfigs.GetConfig(weaponGameId);
			var power = QuantumStatCalculator.CalculateStatValue(rarity, weaponConfig.PowerRatioToBase, level, 
			                                                     f.GameConfig, StatType.Power);
			
			blackboard.Set(f, nameof(QuantumWeaponConfig.AimTime), weaponConfig.AimTime);
			blackboard.Set(f, nameof(QuantumWeaponConfig.AttackCooldown), weaponConfig.AttackCooldown);
			blackboard.Set(f, Constants.AmmoKey, weaponConfig.InitialAmmo);
			
			weapon.WeaponId = weaponConfig.Id;
			weapon.MaxAmmo = weaponConfig.MaxAmmo;
			weapon.AttackCooldown = weaponConfig.AttackCooldown;
			weapon.LastAttackTime = f.Time;
			weapon.AttackRange = weaponConfig.AttackRange;
			weapon.AttackAngle = weaponConfig.AttackAngle;
			weapon.ProjectileSpeed = weaponConfig.ProjectileSpeed;
			weapon.SplashRadius = weaponConfig.SplashRadius;
			weapon.AimingMovementSpeed = weaponConfig.AimingMovementSpeed;
			weapon.ProjectileSpawnOffset = projectileSpawnOffset;
			
			for (var specialIndex = 0; specialIndex < Constants.MAX_SPECIALS; specialIndex++)
			{
				var specialId = weaponConfig.Specials[specialIndex];

				if (specialId == default)
				{
					continue;
				}
				
				var specialConfig = f.SpecialConfigs.GetConfig(specialId);
				
				weapon.Specials[specialIndex] = new Special(f, specialConfig);
			}
			
			f.Unsafe.GetPointer<Stats>(e)->Values[(int) StatType.Power] = new StatData(power, power, StatType.Power);
			
			if (f.Has<Weapon>(e))
			{
				f.Events.OnPlayerWeaponChanged(Player, e, weaponGameId);
				f.Events.OnLocalPlayerWeaponChanged(Player, e, weaponGameId);
			}
			
			f.Set(e, weapon);
		}
		
		/// <summary>
		/// Requests the total amount of ammo the <paramref name="e"/> player has
		/// </summary>
		public int GetAmmoAmount(Frame f, EntityRef e)
		{
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			
			return bb->GetInteger(f, Constants.AmmoKey);
		}

		/// <summary>
		/// Requests if the current weapon equipped by the player is empty of ammo or not.
		/// </summary>
		/// <remarks>
		/// It will be always false for melee weapons. Use <see cref="IsMeleeWeapon"/> to double check the state.
		/// </remarks>
		public bool IsAmmoEmpty(Frame f, EntityRef e)
		{
			return GetAmmoAmount(f, e) == 0;
		}

		/// <summary>
		/// Requests if the current weapon equipped by the player is a melee weapon or not
		/// </summary>
		public bool IsMeleeWeapon(Frame f, EntityRef e)
		{
			return GetAmmoAmount(f, e) < 0;
		}
		
		/// <summary>
		/// Gives the given ammo <paramref name="amount"/> to this <paramref name="e"/> player's entity
		/// </summary>
		internal void GainAmmo(Frame f, EntityRef e, uint amount)
		{
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			var ammo = bb->GetInteger(f, Constants.AmmoKey);
			
			// Do not do "gain" for infinite ammo weapons
			if (ammo < 0)
			{
				return;
			}

			var maxAmmo = f.Get<Weapon>(e).MaxAmmo;
			var finalAmmo = Math.Min(maxAmmo, ammo + (int) amount);

			bb->Set(f, Constants.AmmoKey, finalAmmo);

			f.Events.OnPlayerAmmoChanged(Player, e, ammo, finalAmmo, maxAmmo);
			f.Events.OnLocalPlayerAmmoChanged(Player, e, ammo, finalAmmo, maxAmmo);
		}

		private void InitStats(Frame f, EntityRef e, Equipment[] playerGear)
		{
			var kcc = new CharacterController3D();

			kcc.Init(f, f.FindAsset<CharacterController3DConfig>(KccConfigRef.Id));
			QuantumStatCalculator.CalculateStats(f, playerGear, out var armour, out var health, out var speed);

			health += f.GameConfig.PlayerDefaultHealth;
			speed += f.GameConfig.PlayerDefaultSpeed;
			kcc.MaxSpeed = speed;

			f.Add(e, kcc);
			f.Add(e, new Stats(health, 0, speed, armour, f.GameConfig.PlayerDefaultInterimArmour));
		}
	}
}