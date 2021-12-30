using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="PlayerCharacter"/> and it's dependent component states
	/// </summary>
	public unsafe class PlayerCharacterSystem : SystemMainThreadFilter<PlayerCharacterSystem.PlayerCharacterFilter>, 
	                                            ISignalOnComponentRemoved<PlayerCharacter>,
	                                            ISignalOnPlayerDataSet, ISignalPlayerKilledPlayer, ISignalHealthIsZero,
	                                            ISignalTargetChanged
	{
		public struct PlayerCharacterFilter
		{
			public EntityRef Entity;
			public Transform3D* Transform;
			public PlayerCharacter* Player;
		}
		
		/// <inheritdoc />
		public void OnRemoved(Frame f, EntityRef entity, PlayerCharacter* playerCharacter)
		{
			f.Unsafe.GetPointerSingleton<GameContainer>()->RemovePlayer(f, playerCharacter->Player);
		}

		/// <inheritdoc />
		public override void Update(Frame f, ref PlayerCharacterFilter filter)
		{
			ProcessSpawnPlayer(f, ref filter);
			ProcessPlayerDisconnect(f, ref filter);
			ProcessAlivePlayers(f, ref filter);
		}

		/// <inheritdoc />
		public void OnPlayerDataSet(Frame f, PlayerRef playerRef)
		{
			var spawnerTransform = QuantumHelpers.GetPlayerSpawnTransform(f);
			var playerData = f.GetPlayerData(playerRef);
			var playerEntity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.PlayerCharacterPrototype.Id));
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(playerEntity);
			
			playerCharacter->Init(f, playerEntity, playerRef, spawnerTransform.Component, playerData.PlayerLevel, 
			                      playerData.Skin, playerData.Weapon, playerData.Gear);
		}

		/// <inheritdoc />
		public void HealthIsZero(Frame f, EntityRef entity, EntityRef attacker)
		{
			if (f.Unsafe.TryGetPointer<PlayerCharacter>(entity, out var player))
			{
				if (f.TryGet<PlayerCharacter>(attacker, out var killer))
				{
					f.Signals.PlayerKilledPlayer(player->Player, entity, killer.Player, attacker);
					f.Events.OnPlayerKilledPlayer(player->Player, killer.Player);
				}
				
				// If it was not the player the killer then will save it as PlayerRef.None
				player->Dead(f, entity, killer.Player, attacker);
			}
		}

		/// <inheritdoc />
		public void TargetChanged(Frame f, EntityRef attacker, EntityRef target)
		{
			f.Events.OnTargetChanged(attacker, target);
			
			if (f.TryGet<PlayerCharacter>(attacker, out var playerCharacter) && !f.Has<BotCharacter>(attacker))
			{
				f.Events.OnLocalPlayerTargetChanged(playerCharacter.Player, attacker, target);
			}
		}

		/// <inheritdoc />
		public void PlayerKilledPlayer(Frame f, PlayerRef playerDead, EntityRef entityDead, PlayerRef playerKiller,
		                               EntityRef entityKiller)
		{
			var deathPosition = f.Get<Transform3D>(entityDead).Position;
			var armourDropChance = f.RNG->Next();
			var step = 0;
			
			// Try to drop Health pack
			if (f.RNG->Next() <= f.GameConfig.DeathDropHealthChance)
			{
				Collectable.DropCollectable(f, GameId.Health, deathPosition, step, false);

				step++;
			}
			
			// Try to drop InterimArmourLarge, if didn't work then try to drop InterimArmourSmall
			if (armourDropChance <= f.GameConfig.DeathDropInterimArmourLargeChance)
			{
				Collectable.DropCollectable(f, GameId.InterimArmourLarge, deathPosition, step, false);

				step++;
			}
			else if (armourDropChance <= f.GameConfig.DeathDropInterimArmourSmallChance + f.GameConfig.DeathDropInterimArmourLargeChance)
			{
				Collectable.DropCollectable(f, GameId.InterimArmourSmall, deathPosition, step, false);

				step++;
			}
			
			// Try to drop Weapon
			if (f.RNG->Next() <= f.GameConfig.DeathDropWeaponChance && f.TryGet<Weapon>(entityDead, out var weapon))
			{
				Collectable.DropCollectable(f, weapon.GameId, deathPosition, step, true);
			}
		}
		
		private void ProcessSpawnPlayer(Frame f, ref PlayerCharacterFilter filter)
		{
			if (f.TryGet<SpawnPlayerCharacter>(filter.Entity, out var spawnPlayer) && f.Time > spawnPlayer.EndSpawnTime)
			{
				filter.Player->Activate(f, filter.Entity);
			}
		}

		private void ProcessPlayerDisconnect(Frame f, ref PlayerCharacterFilter filter)
		{
			if (f.Has<BotCharacter>(filter.Entity))
			{
				return;
			}
			
			if ((f.GetPlayerInputFlags(filter.Player->Player) & DeterministicInputFlags.PlayerNotPresent) == 0)
			{
				filter.Player->DisconnectedDuration = 0;
				
				return;
			}
			
			filter.Player->DisconnectedDuration += f.DeltaTime;
				
			if (filter.Player->DisconnectedDuration > f.GameConfig.DisconnectedDestroySeconds)
			{
				filter.Player->PlayerLeft(f, filter.Entity);
			}
		}

		private void ProcessAlivePlayers(Frame f, ref PlayerCharacterFilter filter)
		{
			if (!f.TryGet<AlivePlayerCharacter>(filter.Entity, out var alivePlayer))
			{
				return;
			}

			ProcessWeaponMode(f, ref filter); // TODO: REMOVE
			ProcessWeaponReload(f, ref filter); // TODO: REMOVE
			ProcessWeaponSwitching(f, ref filter); // TODO: REMOVE

			// TODO: Rework charging attack with spells
			if (f.Unsafe.TryGetPointer<PlayerCharacterCharging>(filter.Entity, out var chargePlayer))
			{
				ProcessChargingPlayer(f, ref filter, chargePlayer);
				return;
			}

			ProcessPlayerInput(f, ref filter);
		}

		private void ProcessPlayerInput(Frame f, ref PlayerCharacterFilter filter)
		{
			// Do not process input if player is stunned
			if (f.Has<Stun>(filter.Entity))
			{
				return;
			}

			var input = f.GetPlayerInput(filter.Player->Player);
			var kcc = f.Unsafe.GetPointer<CharacterController3D>(filter.Entity);
			var rotation = FPVector2.Zero;
			var weapon = f.Get<Weapon>(filter.Entity);
			var moveVelocity = FPVector3.Zero;
			var bb = f.Get<AIBlackboardComponent>(filter.Entity);

			if (input->IsMoveButtonDown)
			{
				var speed = f.Get<Stats>(filter.Entity).Values[(int) StatType.Speed].StatValue;

				if (input->IsShootButtonDown)
				{
					speed *= weapon.AimingMovementSpeedMultiplier;
				}
				
				rotation = input->Direction;
				kcc->MaxSpeed = speed;
				moveVelocity = rotation.XOY * speed;
			}

			bb.Set(f, Constants.IsShootingKey, input->IsShootButtonDown);
			bb.Set(f, Constants.AimDirectionKey, input->AimingDirection);
			
			// We have to call "Move" method every frame, even with seemingly Zero velocity because any movement of CharacterController,
			// even the internal gravitational one, is being processed ONLY when we call the "Move" method
			kcc->Move(f, filter.Entity, moveVelocity);
			
			if (input->AimingDirection.SqrMagnitude > FP._0)
			{
				rotation = input->AimingDirection;
			}
			
			// TODO: either process the player's rotation on the BOT SDK or process in here, but not in both places
			// TODO: Remove this when shapes attacks are online
			if (bb.GetEntityRef(f, Constants.TARGET_BB_KEY).IsValid)
			{
				rotation = FPVector2.Zero;
			}

			if (rotation.SqrMagnitude > FP._0)
			{
				QuantumHelpers.LookAt2d(f, filter.Entity, rotation);
			}
		}

		private void ProcessWeaponMode(Frame f, ref PlayerCharacterFilter filter)
		{
			var weapon = f.Unsafe.GetPointer<Weapon>(filter.Entity);
			
			// Handles the healing mode on/off when it's time to switch
			for (var i = 0; i < weapon->Specials.Length; i++)
			{
				var specialPointer = weapon->Specials.GetPointer(i);
				
				if (f.Time >= specialPointer->HealingModeSwitchTime)
				{
					specialPointer->HealingModeSwitchTime = FP.MaxValue;
					weapon->IsHealing = !weapon->IsHealing;
					break;
				}
			}
		}

		private void ProcessWeaponReload(Frame f, ref PlayerCharacterFilter filter)
		{
			var weapon = f.Unsafe.GetPointer<Weapon>(filter.Entity);
			
			// Handles the reload type Never; If it Never reloads then we return and don't process any reloading
			if (weapon->ReloadType == ReloadType.Never)
			{
				return;
			}
			
			// Handles the reload skip for reload types that aren't always reloading
			if (weapon->Ammo < weapon->MaxAmmo && weapon->ReloadType != ReloadType.Always)
			{
				var input = f.GetPlayerInput(filter.Player->Player);
					
				if (weapon->ReloadType == ReloadType.Moving && !input->IsMoveButtonDown || 
				    weapon->ReloadType == ReloadType.NotMoving && input->IsMoveButtonDown)
				{
					weapon->NextCapacityIncreaseTime += f.DeltaTime;
					return;
				}
			}
				
			if (weapon->Ammo >= weapon->MaxAmmo || f.Time < weapon->NextCapacityIncreaseTime)
			{
				return;
			}
				
			weapon->Ammo += 1;
				
			if (weapon->Emptied && weapon->Ammo >= weapon->MinCapacityToShoot)
			{
				weapon->Emptied = false;
			}
				
			if (weapon->Ammo < weapon->MaxAmmo)
			{
				weapon->NextCapacityIncreaseTime = f.Time + weapon->OneCapacityReloadingTime;
			}
		}

		private void ProcessWeaponSwitching(Frame f, ref PlayerCharacterFilter filter)
		{
			var weapon = f.Unsafe.GetPointer<Weapon>(filter.Entity);
			
			// If a player already carries the Default weapon
			// OR if a weapon still has ammo then we don't switch
			if (weapon->GameId == Constants.DEFAULT_WEAPON_GAME_ID || !weapon->Emptied)
			{
				return;
			}
			
			// If it's a bot then we don't check specials and switch weapon right away
			// It's because bots cannot use or not allowed to use all types of specials
			if (f.Has<BotCharacter>(filter.Entity))
			{
				SetDefaultWeapon(f, filter.Entity);
				return;
			}
			
			// If it's not a bot then we check specials
			var specials = weapon->Specials;
			for (int i = 0; i < Constants.MAX_SPECIALS; i++)
			{
				// If any weapon's special is available then we don't switch
				if (specials[i].IsSpecialAvailable(f))
				{
					return;
				}
			}
			
			SetDefaultWeapon(f, filter.Entity);
		}
		
		private void ProcessChargingPlayer(Frame f, ref PlayerCharacterFilter filter, PlayerCharacterCharging* charge)
		{
			var startPos2d = charge->ChargeStartPos.XZ;
			var targetPos2d = charge->ChargeEndPos.XZ;
			var startPosY = charge->ChargeStartPos.Y;
			var targetPosY = charge->ChargeEndPos.Y;
			var lerpT = FPMath.Clamp01(FP._1 - ((charge->ChargeEndTime - f.Time) / charge->ChargeDuration));
			var nextPos2d = FPVector2.Lerp(startPos2d, targetPos2d, lerpT);
			var nextPosY = FPMath.Lerp(startPosY, targetPosY, lerpT);
			var nextPos = new FPVector3(nextPos2d.X, nextPosY, nextPos2d.Y);
				
			filter.Transform->Position = nextPos;
				
			if (f.Time > charge->ChargeEndTime)
			{
				f.Remove<PlayerCharacterCharging>(filter.Entity);
			}
		}

		private void SetDefaultWeapon(Frame f, EntityRef e)
		{
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(e);
			playerCharacter->SetWeapon(f, e, Constants.DEFAULT_WEAPON_GAME_ID, ItemRarity.Common, 1);
		}
	}
}