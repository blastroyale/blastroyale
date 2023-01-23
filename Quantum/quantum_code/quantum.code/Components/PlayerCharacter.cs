using System;
using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct PlayerCharacter
	{
		/// <summary>
		/// Requests the current weapon of player character
		/// </summary>
		public Equipment CurrentWeapon => WeaponSlot->Weapon;
		
		/// <summary>
		/// Requests the current weapon slot of player character
		/// </summary>
		public WeaponSlot* WeaponSlot => WeaponSlots.GetPointer(CurrentWeaponSlot);

		/// <summary>
		/// Spawns this <see cref="PlayerCharacter"/> with all the necessary data.
		/// </summary>
		internal void Init(Frame f, EntityRef e, PlayerRef playerRef, Transform3D spawnPosition, uint playerLevel,
		                   uint trophies, GameId skin, GameId deathMarker, int teamId, Equipment[] startingEquipment, Equipment loadoutWeapon)
		{
			var blackboard = new AIBlackboardComponent();
			var kcc = new CharacterController3D();
			var transform = f.Unsafe.GetPointer<Transform3D>(e);

			Player = playerRef;
			TeamId = teamId < 0 ? Player + (int) TeamType.TOTAL : 1000 + teamId;
			CurrentWeaponSlot = 0;
			DroppedLoadoutFlags = 0;
			transform->Position = spawnPosition.Position;
			transform->Rotation = spawnPosition.Rotation;

			// The hammer should inherit ONLY the faction from your loadout weapon
			WeaponSlots[Constants.WEAPON_INDEX_DEFAULT].Weapon = Equipment.Create(GameId.Hammer, EquipmentRarity.Common, 1, f);
			if (loadoutWeapon.IsValid())
			{
				WeaponSlots[Constants.WEAPON_INDEX_DEFAULT].Weapon.Faction = loadoutWeapon.Faction;
			}

			var config = f.WeaponConfigs.GetConfig(CurrentWeapon.GameId);
			WeaponSlots.GetPointer(Constants.WEAPON_INDEX_DEFAULT)->MagazineShotCount = config.MagazineSize;

			if (f.Context.GameModeConfig.SpawnWithGear || f.Context.GameModeConfig.SpawnWithWeapon)
			{
				foreach (var item in startingEquipment)
				{
					Gear[GetGearSlot(item)] = item;
				}
			}

			// This makes the entity debuggable in BotSDK. Access debugger inspector from circuit editor and see
			// a list of all currently registered entities and their states.
			//BotSDKDebuggerSystem.AddToDebugger(e);

			blackboard.InitializeBlackboardComponent(f, f.FindAsset<AIBlackboard>(BlackboardRef.Id));
			f.Unsafe.GetPointerSingleton<GameContainer>()->AddPlayer(f, playerRef, e, playerLevel, skin, deathMarker, trophies);
			kcc.Init(f, f.FindAsset<CharacterController3DConfig>(KccConfigRef.Id));

			f.Add(e, blackboard);
			f.Add(e, kcc);
			
			f.Add<Stats>(e);
			f.Add<HFSMAgent>(e);
			HFSMManager.Init(f, e, f.FindAsset<HFSMRoot>(HfsmRootRef.Id));

			f.Unsafe.GetPointer<PhysicsCollider3D>(e)->Enabled = false;
		}

		/// <summary>
		/// Spawns the player with it's initial default values
		/// </summary>
		internal void Spawn(Frame f, EntityRef e)
		{
			// Replenish weapon slots
			for (var i = Constants.WEAPON_INDEX_DEFAULT + 1; i < WeaponSlots.Length; i++)
			{
				WeaponSlots[i] = default;
			}
			
			var isRespawning = f.GetSingleton<GameContainer>().PlayersData[Player].DeathCount > 0;
			if (isRespawning)
			{
				var defaultSlot = WeaponSlots.GetPointer(Constants.WEAPON_INDEX_DEFAULT);
				for (var i = 0; i < defaultSlot->Specials.Length; i++)
				{
					var special = defaultSlot->Specials[i];

					special.AvailableTime = f.Time + special.InitialCooldown;

					defaultSlot->Specials[i] = special;
				}
				
				EquipSlotWeapon(f, e, Constants.WEAPON_INDEX_DEFAULT);
			}
			else
			{
				var weaponConfig = SetSlotWeapon(f, e, Constants.WEAPON_INDEX_DEFAULT);
				var defaultSlot = WeaponSlots.GetPointer(Constants.WEAPON_INDEX_DEFAULT);
				
				for (var i = 0; i < defaultSlot->Specials.Length; i++)
				{
					var id = weaponConfig.Specials[i];
					
					defaultSlot->Specials[i] = id == default ? new Special() : new Special(f, id);
				}
			}

			f.Events.OnPlayerSpawned(Player, e, isRespawning);
			f.Events.OnLocalPlayerSpawned(Player, e, isRespawning);

			f.Remove<DeadPlayerCharacter>(e);
		}

		/// <summary>
		/// Sets the player alive for the very first time after they have been spawned.
		/// </summary>
		internal void Activate(Frame f, EntityRef e)
		{
			var targetable = new Targetable {Team = TeamId};
			var stats = f.Unsafe.GetPointer<Stats>(e);

			stats->ResetStats(f, CurrentWeapon, Gear);

			var maxHealth = FPMath.RoundToInt(stats->GetStatData(StatType.Health).StatValue);
			var currentHealth = stats->CurrentHealth;

			f.Add(e, targetable);
			f.Add<AlivePlayerCharacter>(e);

			f.Events.OnPlayerAlive(Player, e, currentHealth, FPMath.RoundToInt(maxHealth));
			f.Events.OnLocalPlayerAlive(Player, e, currentHealth, FPMath.RoundToInt(maxHealth));

			f.Unsafe.GetPointer<PhysicsCollider3D>(e)->Enabled = true;

			StatusModifiers.AddStatusModifierToEntity(f, e, StatusModifierType.Immunity,
			                                          f.GameConfig.PlayerAliveShieldDuration.Get(f));
		}

		/// <summary>
		/// Kills this <see cref="PlayerCharacter"/> and mark it as done for the session
		/// </summary>
		internal void Dead(Frame f, EntityRef e, EntityRef attacker)
		{
			f.TryGet<PlayerCharacter>(attacker, out var killerPlayer);
			
			f.Unsafe.GetPointer<PhysicsCollider3D>(e)->Enabled = false;

			var deadPlayer = new DeadPlayerCharacter
			{
				TimeOfDeath = f.Time,
				Killer = killerPlayer.Player,
				KillerEntity = attacker
			};

			// If an entity has NavMeshPathfinder then we stop the movement in case an entity was moving
			if (f.Unsafe.TryGetPointer<NavMeshPathfinder>(e, out var navMeshPathfinder))
			{
				navMeshPathfinder->Stop(f, e, true);
			}

			if (f.Context.GameModeConfig.Lives == 1)
			{
				f.Add<EntityDestroyer>(e);
			}

			f.Add(e, deadPlayer);
			f.Remove<Targetable>(e);
			f.Remove<AlivePlayerCharacter>(e);
			
			if (killerPlayer.Player.IsValid)
			{
				f.Signals.PlayerKilledPlayer(Player, e, killerPlayer.Player, attacker);
				f.Events.OnPlayerKilledPlayer(Player, killerPlayer.Player);
			}

			f.Events.OnPlayerDead(Player, e, attacker, f.Has<PlayerCharacter>(attacker));
			f.Events.OnLocalPlayerDead(Player, killerPlayer.Player, attacker);
			f.Signals.PlayerDead(Player, e);

			var agent = f.Unsafe.GetPointer<HFSMAgent>(e);
			HFSMManager.TriggerEvent(f, &agent->Data, e, Constants.DeadEvent);

			if (!f.Has<BotCharacter>(e))
			{
				f.Events.FireQuantumServerCommand(Player, QuantumServerCommand.EndOfGameRewards);
			}
		}

		/// <summary>
		/// Adds a <paramref name="weapon"/> to the player's weapon slots
		/// </summary>
		internal void AddWeapon(Frame f, EntityRef e, Equipment weapon, bool primary)
		{
			Assert.Check(weapon.IsWeapon(), weapon);

			var weaponConfig = f.WeaponConfigs.GetConfig(weapon.GameId);
			var initialAmmo = weaponConfig.InitialAmmoFilled.Get(f);
			var slot = GetWeaponEquipSlot(f, weapon, primary);
			var primaryWeapon = WeaponSlots[Constants.WEAPON_INDEX_PRIMARY].Weapon;
			var stats = f.Unsafe.GetPointer<Stats>(e);

			if (primaryWeapon.IsValid() && weapon.GameId == primaryWeapon.GameId &&
			    weapon.Rarity > primaryWeapon.Rarity)
			{
				slot = Constants.WEAPON_INDEX_PRIMARY;
			}

			// Optionally drop the weapon if there's a different weapon in a slot
			if (f.Context.GameModeConfig.DropWeaponOnPickup &&
			    WeaponSlots[slot].Weapon.IsValid() &&
			    WeaponSlots[slot].Weapon.GameId != weapon.GameId)
			{
				var dropPosition = f.Get<Transform3D>(e).Position + FPVector3.Forward;
				Collectable.DropEquipment(f, WeaponSlots[slot].Weapon, dropPosition, 0);
			}

			var targetSlot = WeaponSlots.GetPointer(slot);
			targetSlot->MagazineShotCount = weaponConfig.MagazineSize;
			targetSlot->ReloadTime = weaponConfig.ReloadTime;
			targetSlot->MagazineSize = weaponConfig.MagazineSize;
			targetSlot->AmmoCostPerShot = FPMath.Max(1, ((FP)f.GameConfig.PlayerDefaultAmmoCapacity.Get(f) / weaponConfig.MaxAmmo.Get(f))).AsInt;
			WeaponSlots[slot].Weapon = weapon;

			stats->GainAmmoPercent(f, e, FPMath.Max(0, initialAmmo - GetAmmoAmountFilled(f, e)));

			f.Events.OnLocalPlayerWeaponAdded(Player, e, weapon, slot);
			
			for (var i = 0; i < WeaponSlots[slot].Specials.Length; i++)
			{
				var id = weaponConfig.Specials[i];
				var special	= id == default ? new Special() : new Special(f, id);

				// If equipping a weapon of the same type, just increase the charges and keep the lowest recharge time
				if (weapon.GameId == primaryWeapon.GameId)
				{
					special.AvailableTime = FPMath.Min(WeaponSlots[slot].Specials[i].AvailableTime, special.AvailableTime);
				}

				WeaponSlots.GetPointer(slot)->Specials[i] = special;
			}

			EquipSlotWeapon(f, e, slot);
		}

		/// <summary>
		/// Sets the player's weapon to the given <paramref name="slot"/>
		/// </summary>
		internal void EquipSlotWeapon(Frame f, EntityRef e, int slot)
		{
			SetSlotWeapon(f, e, slot);
			f.Events.OnPlayerWeaponChanged(Player, e, slot);
			HFSMManager.TriggerEvent(f, e, Constants.ChangeWeaponEvent);
		}

		/// <summary>
		/// Tries to set the player's weapon to the given <paramref name="weaponGameId"/> that player already has
		/// </summary>
		internal bool TryEquipExistingWeaponId(Frame f, EntityRef e, GameId weaponGameId)
		{
			for (int i = 0; i < WeaponSlots.Length; i++)
			{
				if (WeaponSlots[i].Weapon.GameId == weaponGameId)
				{
					EquipSlotWeapon(f, e, i);
					return true;
				}
			}
			
			return false;
		}

		/// <summary>
		/// Equips a gear item to the correct gear slot (old one is replaced).
		/// </summary>
		internal void EquipGear(Frame f, EntityRef e, Equipment gear)
		{
			Assert.Check(!gear.IsWeapon(), gear);

			var gearSlot = GetGearSlot(gear);
			
			Gear[gearSlot] = gear;
			
			f.Unsafe.GetPointer<Stats>(e)->RefreshEquipmentStats(f, Player, e, CurrentWeapon, Gear);
			
			f.Events.OnPlayerGearChanged(Player, e, gear, gearSlot);
		}

		/// <summary>
		/// Requests the total amount of ammo the <paramref name="e"/> player has
		/// </summary>
		public FP GetAmmoAmountFilled(Frame f, EntityRef e)
		{
			var stats = f.Unsafe.GetPointer<Stats>(e);
			return stats->CurrentAmmo / stats->GetStatData(StatType.AmmoCapacity).StatValue;
		}

		/// <summary>
		/// Requests if entity <paramref name="e"/> has ammo left or not
		/// </summary>
		public bool IsAmmoEmpty(Frame f, EntityRef e, bool includeMag = true)
		{
			return f.Unsafe.GetPointer<Stats>(e)->CurrentAmmo == 0
				   && !HasMeleeWeapon(f, e)
				   && (!includeMag || WeaponSlot->MagazineShotCount == 0);
		}

		/// <summary>
		/// Subtracts one shot from the magazine of the weapon that entity <paramref name="e"/> player currently has equipped
		/// </summary>
		public void ReduceMag(Frame f, EntityRef e)
		{
			var slot = WeaponSlot;
			var stats = f.Unsafe.GetPointer<Stats>(e);
			var ammoCost = slot->AmmoCostPerShot;

			// reduce magazine count if your weapon uses a magazine
			if (slot->MagazineShotCount > 0 && slot->MagazineSize > 0)
			{
				slot->MagazineShotCount -= 1;
				f.Events.OnPlayerMagazineChanged(Player, e, slot->MagazineSize);
			}
			else // reduce ammo directly if your weapon does not use an ammo count
			{
				stats->ReduceAmmo(f, e, ammoCost);
			}
		}

		/// <summary>
		/// Requests the state of the player if is skydiving or not
		/// </summary>
		public bool IsSkydiving(Frame f, EntityRef e)
		{
			return f.Unsafe.TryGetPointer<AIBlackboardComponent>(e, out var bb) && bb->GetBoolean(f, Constants.IsSkydiving);
		}

		/// <summary>
		/// Requests if the current weapon equipped by the player is a melee weapon or not
		/// </summary>
		public bool HasMeleeWeapon(Frame f, EntityRef e)
		{
			return f.Get<AIBlackboardComponent>(e).GetBoolean(f, Constants.HasMeleeWeaponKey);
		}

		/// <summary>
		/// Checks if we dropped a specific piece of equipment (only checks by GameIdGroup).
		///
		/// This does not check if this item is actually in the loadout.
		/// </summary>
		public bool HasDroppedLoadoutItem(Equipment equipment)
		{
			var shift = GetGearSlot(equipment) + 1;
			return (DroppedLoadoutFlags & (1 << shift)) != 0;
		}

		/// <summary>
		/// Checks if we dropped a piece of equipment for specified slot.
		///
		/// This does not check if this item is actually in the loadout.
		/// </summary>
		public bool HasDroppedItemForSlot(int slotIndex)
		{
			var shift = slotIndex + 1;
			return (DroppedLoadoutFlags & (1 << shift)) != 0;
		}
		
		/// <summary>
		/// Returns the slot index of <paramref name="equipment"/> for <see cref="Gear"/>.
		/// </summary>
		public static int GetGearSlot(Equipment equipment)
		{
			return equipment.GetEquipmentGroup() switch
			{
				GameIdGroup.Weapon => Constants.GEAR_INDEX_WEAPON,
				GameIdGroup.Helmet => Constants.GEAR_INDEX_HELMET,
				GameIdGroup.Amulet => Constants.GEAR_INDEX_AMULET,
				GameIdGroup.Armor => Constants.GEAR_INDEX_ARMOR,
				GameIdGroup.Shield => Constants.GEAR_INDEX_SHIELD,
				_ => throw new NotSupportedException($"Could not find Gear index for GameId({equipment.GameId})")
			};
		}

		/// <summary>
		/// Returns the GameIdGroup index of <paramref name="slot"/> for <see cref="Gear"/>.
		/// </summary>
		public static GameIdGroup GetEquipmentGroupForSlot(int slot)
		{
			return slot switch
			{
				Constants.GEAR_INDEX_WEAPON => GameIdGroup.Weapon,
				Constants.GEAR_INDEX_HELMET => GameIdGroup.Helmet,
				Constants.GEAR_INDEX_AMULET => GameIdGroup.Amulet,
				Constants.GEAR_INDEX_ARMOR => GameIdGroup.Armor,
				Constants.GEAR_INDEX_SHIELD => GameIdGroup.Shield,
				_ => throw new NotSupportedException($"Could not find GameIdGroup for slot({slot})")
			};
		}

		/// <summary>
		/// Gets specific metadata around a specific loadout item.
		/// Can return null if the equipment is not part of the loadout.
		/// </summary>
		public EquipmentSimulationMetadata? GetLoadoutMetadata(Frame f, Equipment e)
		{
			var loadout = GetLoadout(f);
			for (var i = 0; i < loadout.Length; i++)
			{
				if (loadout[i].GameId == e.GameId) // only compare game id for speed
				{
					return f.GetPlayerData(Player)?.LoadoutMetadata[i];
				}
			}
			return null;
		}

		/// <summary>
		/// Requests the player's initial setup loadout
		/// </summary>
		public Equipment[] GetLoadout(Frame f)
		{
			return f.GetPlayerData(Player)?.Loadout;
		}
		
		/// <summary>
		/// Requests the player's weapon from initial setup loadout
		/// </summary>
		public Equipment GetLoadoutWeapon(Frame f)
		{
			return f.GetPlayerData(Player)?.Weapon ?? Equipment.None;
		}

		/// <summary>
		/// Sets that we dropped a specific piece of equipment (via GameIdGroup).
		///
		/// This does not check if this item is actually in the loadout.
		/// </summary>
		internal void SetDroppedLoadoutItem(Equipment equipment)
		{
			var shift = GetGearSlot(equipment) + 1;
			DroppedLoadoutFlags |= 1 << shift;
		}


		/// <summary>
		/// Checks if the player has this <paramref name="equipment"/> item equipped, based on it's
		/// GameId and Rarity (rarity of equipped item has to be higher).
		/// </summary>
		internal bool HasBetterWeaponEquipped(Equipment equipment)
		{
			for (int i = 0; i < WeaponSlots.Length; i++)
			{
				var weapon = WeaponSlots[i].Weapon;
				if (weapon.GameId == equipment.GameId && weapon.Rarity >= equipment.Rarity)
				{
					return true;
				}
			}

			return false;
		}

		private int GetWeaponEquipSlot(Frame f, Equipment weapon, bool primary)
		{
			if (f.Context.GameModeConfig.SingleSlotMode)
			{
				return Constants.WEAPON_INDEX_PRIMARY;
			}

			for (int i = 0; i < WeaponSlots.Length; i++)
			{
				var equippedWeapon = WeaponSlots[i].Weapon;
				if (weapon.GameId == equippedWeapon.GameId)
				{
					return i;
				}
			}

			return primary ? Constants.WEAPON_INDEX_PRIMARY : Constants.WEAPON_INDEX_SECONDARY;
		}

		private QuantumWeaponConfig SetSlotWeapon(Frame f, EntityRef e, int slot)
		{
			CurrentWeaponSlot = slot;

			var blackboard = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			var weaponConfig = f.WeaponConfigs.GetConfig(CurrentWeapon.GameId);
			//the total time it takes for a burst to complete should be divded by the burst_interval_divider
			//if we are only firing one shot, burst interval is 0
			var burstCooldown = weaponConfig.NumberOfBursts > 1 ? weaponConfig.AttackCooldown / Constants.BURST_INTERVAL_DIVIDER / (weaponConfig.NumberOfBursts - 1) : 0;

			blackboard->Set(f, nameof(QuantumWeaponConfig.TapCooldown), weaponConfig.TapCooldown);
			blackboard->Set(f, nameof(QuantumWeaponConfig.AttackCooldown), weaponConfig.AttackCooldown);
			blackboard->Set(f, nameof(QuantumWeaponConfig.AimingMovementSpeed), weaponConfig.AimingMovementSpeed);
			blackboard->Set(f, nameof(QuantumWeaponConfig.NumberOfBursts), weaponConfig.NumberOfBursts);
			blackboard->Set(f, nameof(QuantumWeaponConfig.ReloadTime), weaponConfig.ReloadTime);
			blackboard->Set(f, nameof(QuantumWeaponConfig.MagazineSize), weaponConfig.MagazineSize);
			blackboard->Set(f, Constants.HasMeleeWeaponKey, weaponConfig.IsMeleeWeapon);
			blackboard->Set(f, Constants.BurstTimeDelay, burstCooldown);
			
			f.Unsafe.GetPointer<Stats>(e)->RefreshEquipmentStats(f, Player, e, CurrentWeapon, Gear);

			return weaponConfig;
		}
	}
}
