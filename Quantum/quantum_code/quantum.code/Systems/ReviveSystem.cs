using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// System handling revive logic, this contains knocking out the player when he dies, revive collider, and reviving the player.
	/// </summary>
	public unsafe class ReviveSystem : SystemMainThreadFilter<ReviveSystem.WoundedFilter>, ISignalOnTriggerEnter3D,
									   ISignalOnTriggerExit3D, ISignalOnComponentRemoved<Wounded>, ISignalPlayerDead
	{
		public struct WoundedFilter
		{
			public EntityRef Entity;
			public Wounded* Wounded;
			public Transform3D* Transform;
			public Stats* Stats;
		}

		#region Config

		private static QuantumReviveConfigs GetConfig(Frame f)
		{
			var reviveConfigs = f.FindAsset<QuantumReviveConfigs>(f.RuntimeConfig.ReviveConfigs.Id);
			return reviveConfigs;
		}

		private static ReviveEntry GetReviveConfig(Frame f, byte index)
		{
			var configs = GetConfig(f);
			var gamemodeConfig = configs.PerGameMode.Get(f);
			return gamemodeConfig.AllowedRevives[index];
		}

		public static ReviveEntry GetConfigForWounded(Frame f, Wounded* wounded)
		{
			return GetReviveConfig(f, wounded->WoundedConfigIndex);
		}

		#endregion

		#region Revive

		public override void Update(Frame f, ref WoundedFilter filter)
		{
			// Move revive collider with player
			var colliderTransform3D = f.Unsafe.GetPointer<Transform3D>(filter.Wounded->ColliderEntity);
			colliderTransform3D->Position = filter.Transform->Position;

			if (f.ResolveHashSet(filter.Wounded->PlayersReviving).Count > 0)
			{
				if (filter.Wounded->EndRevivingAt <= f.Time)
				{
					// Revive player
					var reviveHealthPercentage = GetConfigForWounded(f, filter.Wounded).LifePercentageOnRevived;
					f.Remove<Wounded>(filter.Entity);
					f.Events.OnPlayerRevived(filter.Entity);
					filter.Stats->SetCurrentHealthPercentage(f, filter.Entity, reviveHealthPercentage);
				}

				// Someone is reviving him so no damage
				return;
			}


			if (filter.Wounded->NextDamageAt <= f.Time)
			{
				// Do damage
				var config = GetConfigForWounded(f, filter.Wounded);
				filter.Wounded->NextDamageAt = f.Time + config.DamageTickInterval;
				var spell = new Spell
				{
					Id = Spell.Wounded,
					Victim = filter.Entity,
					Attacker = filter.Wounded->WoundedBy,
					SpellSource = EntityRef.None,
					Cooldown = FP._0,
					EndTime = FP._0,
					NextHitTime = FP._0,
					OriginalHitPosition = filter.Transform->Position,
					PowerAmount = (uint)FPMath.RoundToInt((FP)filter.Stats->MaxHealth * config.DamagePerTick),
					KnockbackAmount = 0,
					TeamSource = 0
				};
				filter.Stats->ReduceHealth(f, filter.Entity, &spell);
			}
		}


		public void OnTriggerEnter3D(Frame f, TriggerInfo3D info)
		{
			if (!f.Unsafe.TryGetPointer<WoundedCollider>(info.Entity, out var woundedCollider))
			{
				return;
			}

			// It needs to be a player to revivew
			// Wounded players cant revive other players
			if (!f.Has<PlayerCharacter>(info.Other) || f.Has<Wounded>(info.Other))
			{
				return;
			}

			// needs to be in the same team
			if (!TeamSystem.HasSameTeam(f, info.Other, woundedCollider->WoundedEntity))
			{
				return;
			}

			var wounded = f.Unsafe.GetPointer<Wounded>(woundedCollider->WoundedEntity);
			var reviving = f.ResolveHashSet(wounded->PlayersReviving);
			if (reviving.Count == 0)
			{
				var config = GetConfigForWounded(f, wounded);
				wounded->EndRevivingAt = f.Time + config.TimeToRevive;
				// Why this mess exists?
				// Because I want to allow the bar to slowly decrease when a player leaves a collider, i have considered summing and decreasing a value per frame
				// but this is more performant, since we don't need to do anything on update loop, but the cost is readability
				if (wounded->BackAtZero > f.Time)
				{
					wounded->EndRevivingAt -= wounded->BackAtZero - f.Time;
				}

				f.Events.OnPlayerStartReviving(woundedCollider->WoundedEntity);
			}

			reviving.Add(info.Other);
		}


		public void OnTriggerExit3D(Frame f, ExitInfo3D info)
		{
			if (!f.Unsafe.TryGetPointer<WoundedCollider>(info.Entity, out var woundedCollider))
			{
				return;
			}

			var wounded = f.Unsafe.GetPointer<Wounded>(woundedCollider->WoundedEntity);
			var resolveHashSet = f.ResolveHashSet(wounded->PlayersReviving);
			resolveHashSet.Remove(info.Other);
			if (resolveHashSet.Count == 0)
			{
				var config = GetConfigForWounded(f, wounded);
				// Progress on leave
				var timeReviving = config.TimeToRevive - (wounded->EndRevivingAt - f.Time);
				wounded->BackAtZero = f.Time + timeReviving;
				// Reset damage timer 
				wounded->NextDamageAt = f.Time + config.DamageTickInterval;
				f.Events.OnPlayerStopReviving(woundedCollider->WoundedEntity);
			}
		}


		public void OnRemoved(Frame f, EntityRef entity, Wounded* component)
		{
			f.Destroy(component->ColliderEntity);
		}

		public void PlayerDead(Frame f, PlayerRef playerDead, EntityRef entityDead)
		{
			var teamsAlive = new HashSet<int>();
			foreach (var (entity, _) in f.Unsafe.GetComponentBlockIterator<AlivePlayerCharacter>())
			{
				if (f.Unsafe.TryGetPointer<PlayerCharacter>(entity, out var t) && !IsWounded(f, entity))
				{
					teamsAlive.Add(t->TeamId);
				}
			}

			foreach (var (entity, wounded) in f.Unsafe.GetComponentBlockIterator<Wounded>())
			{
				if (!f.Unsafe.TryGetPointer<PlayerCharacter>(entity, out var t))
				{
					continue;
				}

				if (teamsAlive.Contains(t->TeamId))
				{
					continue;
				}

				// KILL THEM ALL
				f.Unsafe.GetPointer<Stats>(entity)->Kill(f, entity, wounded->WoundedBy);
			}
		}

		#endregion

		#region Helpers

		public static bool IsWounded(Frame f, EntityRef player)
		{
			return f.Has<Wounded>(player);
		}

		// ReSharper disable once UnusedMember.Global
		public static FP CalculateRevivePercentage(Frame f, Wounded* wounded)
		{
			var timeToRevive = GetConfigForWounded(f, wounded).TimeToRevive;
			// Player is being revived
			if (f.ResolveHashSet(wounded->PlayersReviving).Count > 0)
			{
				var timeLeftToRevive = wounded->EndRevivingAt - f.Time;
				return FP._1 - (timeLeftToRevive / timeToRevive);
			}

			// Bar is still decreasing
			if (wounded->BackAtZero > f.Time)
			{
				var timeLeftDecreasing = wounded->BackAtZero - f.Time;
				return timeLeftDecreasing / timeToRevive;
			}

			return FP._0;
		}

		#endregion

		#region Overwrites

		/// <summary>
		/// This is called at every time any player takes damage
		/// It is used to change the damage to the wounded player
		/// </summary>
		public static bool OverwriteDamage(Frame f, EntityRef damaged, Spell* spell, int maxHealth, ref int damage)
		{
			if (
				spell->IsInstantKill() ||
				!f.Unsafe.TryGetPointer<Wounded>(damaged, out var wounded))
			{
				return false;
			}


			if (spell->Id != Spell.Wounded)
			{
				damage = ((FP)maxHealth * GetConfigForWounded(f, wounded).DamagePerShot).AsInt;
				return true;
			}

			return false;
		}

		/// <summary>
		/// This is called everytime a player health reaches 0
		/// Returning true will skip the dead logic
		/// </summary>
		public static bool WoundPlayer(Frame f, EntityRef playerEntityRef, Spell* spell)
		{
			if (!f.Unsafe.TryGetPointer<Revivable>(playerEntityRef, out var revivable))
			{
				return false;
			}

			if (!CanBeWounded(f, playerEntityRef, revivable))
			{
				return false;
			}

			f.Add<Wounded>(playerEntityRef, out var woundedComponent);
			woundedComponent->WoundedConfigIndex = revivable->TimesWounded;
			var config = GetConfigForWounded(f, woundedComponent);
			var stats = f.Unsafe.GetPointer<Stats>(playerEntityRef);
			var transform = f.Unsafe.GetPointer<Transform3D>(playerEntityRef);
			stats->SetCurrentHealthPercentage(f, playerEntityRef, config.LifePercentageOnWounded);
			woundedComponent->NextDamageAt = f.Time + config.DamageTickInterval;
			// Create collider to detect revive
			woundedComponent->ColliderEntity = f.Create();
			woundedComponent->WoundedBy = spell->Attacker;

			var shape3D = Shape3D.CreateSphere(config.ReviveColliderRange);
			var colliderEntity = woundedComponent->ColliderEntity;
			f.Add(colliderEntity, Transform3D.Create(transform->Position));
			f.Add(colliderEntity, PhysicsCollider3D.Create(f, shape3D, null, true, f.Context.TargetPlayerTriggersLayerIndex));
			f.Add(colliderEntity, new WoundedCollider()
			{
				WoundedEntity = playerEntityRef
			});
			f.Physics3D.SetCallbacks(colliderEntity, CallbackFlags.OnDynamicTriggerEnter | CallbackFlags.OnDynamicTriggerExit);
			f.Events.OnPlayerWounded(playerEntityRef);
			revivable->TimesWounded++;
			return true;
		}

		public static bool CanBeWounded(Frame f, EntityRef entityRef, Revivable* revivable)
		{
			if (GetConfig(f).FullyDisable)
			{
				return false;
			}

			// reviving disabled
			if (f.Context.TryGetMutatorByType(MutatorType.DisableRevive, out _))
			{
				return false;
			}

			// Already wounded
			if (f.Has<Wounded>(entityRef))
			{
				return false;
			}

			if (TeamSystem.GetAliveTeamMembersAmount(f, entityRef, false) == 0)
			{
				return false;
			}

			var maxRevives = GetConfig(f).PerGameMode.Get(f).AllowedRevives.Count;
			if (revivable->TimesWounded >= maxRevives)
			{
				return false;
			}

			return true;
		}


		/// <summary>
		/// Called everytime a player dies, except when he got wounded in that frame
		/// </summary>
		public static void OverwriteKiller(Frame f, EntityRef entity, Spell* spell)
		{
			// Gives the kill to the player who wounded the damaged
			if (f.Unsafe.TryGetPointer<Wounded>(entity, out var wounded))
			{
				// if the player killhimself give the kill to the player who shot him, otherwise give the kill to theplayer who wounded him
				if (wounded->WoundedBy != EntityRef.None && wounded->WoundedBy != entity)
				{
					spell->Attacker = wounded->WoundedBy;
				}
			}
		}

		public static void OverwriteMaxMoveSpeed(Frame f, EntityRef player, ref FP maxMoveSpeed)
		{
			if (f.Unsafe.TryGetPointer<Wounded>(player, out var wounded))
			{
				maxMoveSpeed *= GetConfigForWounded(f, wounded).MoveSpeedMultiplier;
			}
		}

		#endregion
	}
}