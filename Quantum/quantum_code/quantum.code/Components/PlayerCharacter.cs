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
			
			f.Events.OnRemotePlayerLeft(Player, e);
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
			f.Events.OnRemotePlayerSpawned(Player, e, isRespawning);
			f.Events.OnLocalPlayerSpawned(Player, e, isRespawning);
			
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
			f.Events.OnRemotePlayerAlive(Player, e);
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
			
			f.Add(e, deadPlayer);

			f.Events.OnPlayerDead(Player, e);
			f.Events.OnRemotePlayerDead(Player, e);
			f.Events.OnLocalPlayerDead(Player, killerPlayer, attacker);
			
			f.Remove<Targetable>(e);
			f.Remove<HFSMAgent>(e);
			f.Remove<AlivePlayerCharacter>(e);
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

		public void SetWeapon(Frame f, EntityRef e, GameId weaponGameId, ItemRarity rarity, uint level)
		{
			var weapon = new Weapon();
			var config = f.WeaponConfigs.GetConfig(weaponGameId);
			var blackboard = f.Get<AIBlackboardComponent>(e);
			var weaponConfig = f.WeaponConfigs.GetConfig(weaponGameId);
			var power = QuantumStatCalculator.CalculateStatValue(rarity, weaponConfig.PowerRatioToBase, level, 
			                                                     f.GameConfig, StatType.Power);
			
			weapon.GameId = config.Id;
			weapon.Ammo = config.InitialCapacity;
			weapon.MaxAmmo = config.MaxCapacity;
			weapon.NextCapacityIncreaseTime = FP._0;
			weapon.OneCapacityReloadingTime = FP._1 / config.ReloadSpeed;
			weapon.AimingMovementSpeedMultiplier = config.AimingMovementSpeedMultiplier;
			weapon.BulletSpreadAngle = config.BulletSpreadAngle;
			weapon.ReloadType = config.ReloadType;
			weapon.MinCapacityToShoot = config.MinCapacityToShoot;
			weapon.Emptied = false;
			weapon.IsHealing = false;
			weapon.ProjectileHealingId = config.ProjectileHealingId;
			weapon.IsAutoShoot = config.IsAutoShoot;
			weapon.AttackCooldown = config.AttackCooldown;
			weapon.NextShotAllowedTime = f.Time;
			weapon.Range = config.ProjectileRange;
			weapon.AttackAngle = config.BulletSpreadAngle;
			weapon.SplashRadius = config.SplashRadius;
			
			for (var specialIndex = 0; specialIndex < Constants.MAX_SPECIALS; specialIndex++)
			{
				var specialId = config.Specials[specialIndex];
				var specialConfig = f.SpecialConfigs.QuantumConfigs.Find(special => special.Id == specialId);
				
				weapon.Specials[specialIndex] = new Special(f, specialConfig, specialIndex);
			}
			
			// Remove all current power ups from a character before adding new ones
			PowerUps.RemovePowerupsFromEntity(f, e);
			
			if (config.IsDiagonalshot)
			{
				PowerUps.AddPowerUpToEntity(f, e, GameId.Diagonalshot, weaponGameId);
			}
			
			if (config.IsMultishot)
			{
				PowerUps.AddPowerUpToEntity(f, e, GameId.Multishot, weaponGameId);
			}
			
			if (config.IsFrontshot)
			{
				PowerUps.AddPowerUpToEntity(f, e, GameId.Frontshot, weaponGameId);
			}
			
			blackboard.Set(f, nameof(QuantumWeaponConfig.AimTime), config.AimTime);
			blackboard.Set(f, nameof(QuantumWeaponConfig.AttackCooldown), config.AttackCooldown);
			blackboard.Set(f, Constants.WEAPON_TARGET_RANGE, config.TargetRange);
			blackboard.Set(f, nameof(QuantumWeaponConfig.TargetingType), (int)config.TargetingType);
			blackboard.Set(f, nameof(QuantumWeaponConfig.ProjectileSpeed), config.ProjectileSpeed);
			blackboard.Set(f, nameof(QuantumWeaponConfig.ProjectileRange), config.ProjectileRange);
			blackboard.Set(f, nameof(QuantumWeaponConfig.SplashRadius), config.SplashRadius);
			blackboard.Set(f, nameof(QuantumWeaponConfig.ProjectileStunDuration), config.ProjectileStunDuration);
			blackboard.Set(f, Constants.PROJECTILE_GAME_ID, (int)config.ProjectileId);
			blackboard.Set(f, nameof(Projectile), FP.FromRaw(Projectile.Id.Value));
			blackboard.Set(f, nameof(QuantumWeaponConfig.IsAutoShoot), config.IsAutoShoot);
			
			f.Unsafe.GetPointer<Stats>(e)->Values[(int) StatType.Power] = new StatData(power, power, StatType.Power);

			if (f.TryGet<Weapon>(e, out var previousWeapon))
			{
				var isSameWeapon = previousWeapon.GameId == weaponGameId;
				var resetCooldownSpecial = new bool[Constants.MAX_SPECIALS];
				
				for (var specialIndex = 0; specialIndex < Constants.MAX_SPECIALS; specialIndex++)
				{
					if (isSameWeapon && previousWeapon.Specials[specialIndex].Charges > 0)
					{
						resetCooldownSpecial[specialIndex] = false;
						weapon.Specials[specialIndex].ResetCooldownTime = previousWeapon.Specials[specialIndex].ResetCooldownTime;
						continue;
					}
					
					resetCooldownSpecial[specialIndex] = true;
				}
				
				f.Events.OnPlayerWeaponChanged(Player, e, weaponGameId);
				f.Events.OnLocalPlayerWeaponChanged(Player, e, weaponGameId, resetCooldownSpecial[0], resetCooldownSpecial[1]);
			}
			
			f.Set(e, weapon);
		}
	}
}