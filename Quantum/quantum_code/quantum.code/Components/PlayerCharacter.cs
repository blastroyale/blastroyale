using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;

namespace Quantum
{
	public class PlayerCharacterSetup
	{
		public EntityRef e;
		public PlayerRef playerRef;
		public Transform2D spawnPosition;
		public uint playerLevel;
		public uint trophies;
		public int teamId = -1; // This cannot be 0 for a valid value, component initialization will fail at TeamSystem
		public List<Modifier> modifiers = null;
		public uint minimumHealth = 0;
	}

	public unsafe partial struct PlayerCharacter
	{
		/// <summary>
		/// Requests the current weapon of player character
		/// </summary>
		public Equipment CurrentWeapon => SelectedWeaponSlot->Weapon;

		/// <summary>
		/// Requests the current weapon slot of player character
		/// </summary>
		public WeaponSlot* SelectedWeaponSlot => WeaponSlots.GetPointer(CurrentWeaponSlot);

		/// <summary>
		/// Spawns this <see cref="PlayerCharacter"/> with all the necessary data.
		/// </summary>
		internal void Init(Frame f, PlayerCharacterSetup setup)
		{
			var blackboard = new AIBlackboardComponent();

			var transform = f.Unsafe.GetPointer<Transform2D>(setup.e);

			Player = setup.playerRef;
			TeamId = setup.teamId;
			CurrentWeaponSlot = 0;
			LastNoInputTimeSnapshot = FP._0;
			transform->Position = setup.spawnPosition.Position;

			var weaponSlot = WeaponSlots.GetPointer(Constants.WEAPON_INDEX_DEFAULT);
			weaponSlot->Weapon = Equipment.Create(f, GameId.Hammer, EquipmentRarity.Common, 1);
			weaponSlot->MagazineShotCount = f.WeaponConfigs.GetConfig(CurrentWeapon.GameId).MagazineSize;

			// This makes the entity debuggable in BotSDK. Access debugger inspector from circuit editor and see
			// a list of all currently registered entities and their states.
			// BotSDKDebuggerSystem.AddToDebugger(setup.e);

			blackboard.InitializeBlackboardComponent(f, f.FindAsset<AIBlackboard>(BlackboardRef.Id));
			f.Unsafe.GetPointerSingleton<GameContainer>()->AddPlayer(f, setup);
			var kcc = new TopDownController();
			kcc.Init();

			var speedUpMutatorExists = f.Context.Mutators.HasFlagFast(Mutator.SpeedUp);
			kcc.MaxSpeed = speedUpMutatorExists ? kcc.MaxSpeed * Constants.MUTATOR_SPEEDUP_AMOUNT : kcc.MaxSpeed;
			f.Add(setup.e, kcc);
			f.Add(setup.e, blackboard);
			f.AddOrGet<Stats>(setup.e, out var stats);
			if (setup.modifiers != null)
			{
				foreach (var modifier in setup.modifiers)
				{
					stats->AddModifier(f, setup.e, modifier);
				}
			}

			stats->MinimumHealth = (int)setup.minimumHealth;

			f.Add<HFSMAgent>(setup.e);
			HFSMManager.Init(f, setup.e, f.FindAsset<HFSMRoot>(HfsmRootRef.Id));

			f.Unsafe.GetPointer<PhysicsCollider2D>(setup.e)->Enabled = false;
		}

		/// <summary>
		/// Spawns the player with it's initial default values
		/// </summary>
		internal void Spawn(Frame f, EntityRef e)
		{
			// Replenish weapon slots
			for (var i = Constants.WEAPON_INDEX_DEFAULT + 1; i < WeaponSlots.Length; i++)
			{
				if (WeaponSlots[i].Weapon.IsValid())
				{
					continue;
				}

				WeaponSlots[i] = default;
			}

			var isRespawning = f.Unsafe.GetPointerSingleton<GameContainer>()->PlayersData[Player].DeathCount > 0;
			var pi = f.Unsafe.GetPointer<PlayerInventory>(e);
			if (isRespawning)
			{
				for (var i = 0; i < pi->Specials.Length; i++)
				{
					var special = pi->Specials[i];

					special.AvailableTime = f.Time + special.InitialCooldown;

					pi->Specials[i] = special;
				}

				EquipSlotWeapon(f, e, Constants.WEAPON_INDEX_DEFAULT);
			}
			else
			{
				SetSlotWeapon(f, e, Constants.WEAPON_INDEX_DEFAULT);
			}

			f.Events.OnPlayerSpawned(Player, e, isRespawning);
			f.Events.OnLocalPlayerSpawned(Player, e, isRespawning);

			f.Remove<DeadPlayerCharacter>(e);
		}

		public bool IsAfk(Frame f)
		{
			return f.Time - LastNoInputTimeSnapshot > f.GameConfig.NoInputKillTime;
		}

		/// <summary>
		/// Sets the player alive for the very first time after they have been spawned.
		/// </summary>
		internal void Activate(Frame f, EntityRef e)
		{
			var targetable = new Targetable { Team = TeamId };
			var stats = f.Unsafe.GetPointer<Stats>(e);

			stats->ResetStats(f, CurrentWeapon, Array.Empty<Equipment>(), e);

			var maxHealth = FPMath.RoundToInt(stats->GetStatData(StatType.Health).StatValue);
			var currentHealth = stats->CurrentHealth;

			f.Add(e, targetable);
			f.Add<AlivePlayerCharacter>(e);

			f.Events.OnPlayerAlive(Player, e, currentHealth, FPMath.RoundToInt(maxHealth));
			f.Events.OnLocalPlayerAlive(Player, e, currentHealth, FPMath.RoundToInt(maxHealth));

			f.Unsafe.GetPointer<PhysicsCollider2D>(e)->Enabled = true;
		}

		/// <summary>
		/// Kills this <see cref="PlayerCharacter"/> and mark it as done for the session
		/// </summary>
		internal void Dead(Frame f, EntityRef e, EntityRef attacker, QBoolean fromRoofDamage)
		{
			f.TryGet<PlayerCharacter>(attacker, out var killerPlayer);

			f.Unsafe.GetPointer<PhysicsCollider2D>(e)->Enabled = false;

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


			f.Add<EntityDestroyer>(e);

			f.Add(e, deadPlayer);
			f.Remove<Targetable>(e);
			f.Remove<AlivePlayerCharacter>(e);

			if (killerPlayer.Player.IsValid)
			{
				f.Signals.PlayerKilledPlayer(Player, e, killerPlayer.Player, attacker);
				f.Events.OnPlayerKilledPlayer(Player, killerPlayer.Player);
			}

			var equipmentData = new EquipmentEventData();
			equipmentData.CurrentWeapon = CurrentWeapon;
			f.Events.OnPlayerDead(Player, e, attacker, f.Has<PlayerCharacter>(attacker), equipmentData);
			f.Events.OnLocalPlayerDead(Player, killerPlayer.Player, attacker, fromRoofDamage);
			f.Signals.PlayerDead(Player, e);

			if (f.Unsafe.TryGetPointer<HFSMAgent>(e, out var agent))
			{
				HFSMManager.TriggerEvent(f, &agent->Data, e, Constants.DEAD_EVENT);
			}

			if (RealPlayer)
			{
				f.ServerCommand(Player, QuantumServerCommand.EndOfGameRewards);
			}
		}
		

		/// <summary>
		/// Adds a <paramref name="weapon"/> to the player's weapon slots
		/// </summary>
		internal void AddWeapon(Frame f, EntityRef e, in Equipment weapon, bool primary)
		{
			var weaponConfig = f.WeaponConfigs.GetConfig(weapon.GameId);
			var slot = GetWeaponEquipSlot(f, weapon, primary);
			var primaryWeapon = WeaponSlots[Constants.WEAPON_INDEX_PRIMARY].Weapon;

			if (primaryWeapon.IsValid() && weapon.GameId == primaryWeapon.GameId &&
				weapon.Rarity > primaryWeapon.Rarity)
			{
				slot = Constants.WEAPON_INDEX_PRIMARY;
			}

			// Optionally drop the weapon if there's a different weapon in a slot
			// In Looting 2.0 we don't drop the gun on the floor
			if (f.Context.MapConfig.LootingVersion != 2 &&
				f.Context.GameModeConfig.DropWeaponOnPickup &&
				WeaponSlots[slot].Weapon.IsValid() &&
				WeaponSlots[slot].Weapon.GameId != weapon.GameId)
			{
				var dropPosition = f.Unsafe.GetPointer<Transform2D>(e)->Position;
				Collectable.DropEquipment(f, WeaponSlots[slot].Weapon, dropPosition, 0, true, 1);
			}

			var targetSlot = WeaponSlots.GetPointer(slot);
			targetSlot->MagazineShotCount = weaponConfig.MagazineSize;
			targetSlot->ReloadTime = weaponConfig.ReloadTime;
			targetSlot->MagazineSize = weaponConfig.MagazineSize;
			WeaponSlots[slot].Weapon = weapon;

			f.Events.OnPlayerWeaponAdded(Player, e, weapon, slot);

			EquipSlotWeapon(f, e, slot);
		}

		/// <summary>
		/// Sets the player's weapon to the given <paramref name="slot"/>
		/// </summary>
		internal void EquipSlotWeapon(Frame f, EntityRef e, int slot)
		{
			SetSlotWeapon(f, e, slot);
			HFSMManager.TriggerEvent(f, e, Constants.CHANGE_WEAPON_EVENT);
		}

		/// <summary>
		/// Requests if entity <paramref name="e"/> has ammo left or not
		/// </summary>
		public bool IsAmmoEmpty(Frame f, EntityRef e, bool includeMag = true)
		{
			return f.Unsafe.GetPointer<Stats>(e)->CurrentAmmoPercent == 0
				&& !HasMeleeWeapon(f, e)
				&& (!includeMag || SelectedWeaponSlot->MagazineShotCount == 0);
		}

		/// <summary>
		/// Subtracts one shot from the magazine of the weapon that entity <paramref name="e"/> player currently has equipped
		/// </summary>
		public void ReduceMag(Frame f, EntityRef e)
		{
			// No need to process for melee
			if (CurrentWeaponSlot == 0)
			{
				return;
			}

			var slot = SelectedWeaponSlot;
			var stats = f.Unsafe.GetPointer<Stats>(e);

			// reduce magazine count if your weapon uses a magazine
			if (slot->MagazineShotCount > 0 && slot->MagazineSize > 0)
			{
				slot->MagazineShotCount -= 1;
				f.Events.OnPlayerAmmoChanged(Player, e, stats->GetCurrentAmmo(),
					f.WeaponConfigs.GetConfig(CurrentWeapon.GameId).MaxAmmo, slot->MagazineShotCount, slot->MagazineSize);
			}
			else // reduce ammo directly if your weapon does not use an ammo count
			{
				stats->ReduceAmmo(f, e, 1);
			}

			if (stats->GetCurrentAmmo() <= 0 && slot->MagazineShotCount <= 0)
			{
				f.Unsafe.GetPointer<PlayerCharacter>(e)->EquipSlotWeapon(f, e, 0);
			}
		}

		/// <summary>
		/// Requests the state of the player if is skydiving or not
		/// </summary>
		public bool IsSkydiving(Frame f, EntityRef e)
		{
			return f.Unsafe.TryGetPointer<AIBlackboardComponent>(e, out var bb) && bb->GetBoolean(f, Constants.IS_SKYDIVING);
		}

		/// <summary>
		/// Requests if the current weapon equipped by the player is a melee weapon or not
		/// </summary>
		public static bool HasMeleeWeapon(Frame f, EntityRef e)
		{
			return f.Unsafe.GetPointer<AIBlackboardComponent>(e)->GetBoolean(f, Constants.HAS_MELEE_WEAPON_KEY);
		}

		public bool HasGoldenWeapon()
		{
			var slot = WeaponSlots[Constants.WEAPON_INDEX_PRIMARY];
			if (slot.Weapon.IsValid())
			{
				return slot.Weapon.Material == EquipmentMaterial.Golden;
			}

			return false;
		}

		private int GetWeaponEquipSlot(Frame f, in Equipment weapon, bool primary)
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

			//If we are only firing one shot, burst interval is 0
			var burstCooldown = weaponConfig.NumberOfBursts > 1 ? weaponConfig.BurstGapTime : 0;

			blackboard->Set(f, nameof(QuantumWeaponConfig.TapCooldown), weaponConfig.TapCooldown);
			blackboard->Set(f, nameof(QuantumWeaponConfig.AttackCooldown), weaponConfig.AttackCooldown);
			blackboard->Set(f, nameof(QuantumWeaponConfig.AimingMovementSpeed), weaponConfig.AimingMovementSpeed);
			blackboard->Set(f, nameof(QuantumWeaponConfig.NumberOfBursts), weaponConfig.NumberOfBursts);
			blackboard->Set(f, nameof(QuantumWeaponConfig.ReloadTime), weaponConfig.ReloadTime);
			blackboard->Set(f, nameof(QuantumWeaponConfig.MagazineSize), weaponConfig.MagazineSize);
			blackboard->Set(f, Constants.HAS_MELEE_WEAPON_KEY, weaponConfig.IsMeleeWeapon);
			blackboard->Set(f, Constants.BURST_TIME_DELAY, burstCooldown);

			var stats = f.Unsafe.GetPointer<Stats>(e);
			var gear = Array.Empty<Equipment>();
			stats->RefreshEquipmentStats(f, Player, e, CurrentWeapon, gear);

			f.Events.OnPlayerWeaponChanged(Player, e, slot);
			f.Events.OnPlayerAmmoChanged(Player, e, stats->GetCurrentAmmo(),
				weaponConfig.MaxAmmo, SelectedWeaponSlot->MagazineShotCount, SelectedWeaponSlot->MagazineSize);

			return weaponConfig;
		}
	}
}