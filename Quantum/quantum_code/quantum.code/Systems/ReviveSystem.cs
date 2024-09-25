using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// System handling knockout logic, this contains knocking out the player when he dies, knockout collider, and reviving the player.
	/// </summary>
	public unsafe class ReviveSystem : SystemMainThreadFilter<ReviveSystem.KnockedOutFilter>, ISignalOnComponentRemoved<KnockedOut>,
									   ISignalPlayerDead, ISignalGameEnded,
									   ISignalOnFeetCollisionEnter, ISignalOnFeetCollisionLeft
	{
		public struct KnockedOutFilter
		{
			public EntityRef Entity;
			public KnockedOut* KnockedOut;
			public Transform2D* Transform;
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

		public static ReviveEntry GetConfigForKnockedOut(Frame f, KnockedOut* knockedout)
		{
			return GetReviveConfig(f, knockedout->ConfigIndex);
		}

		#endregion

		#region Revive

		public override void Update(Frame f, ref KnockedOutFilter filter)
		{
			// Move revive collider with player
			var colliderTransform2D = f.Unsafe.GetPointer<Transform2D>(filter.KnockedOut->ColliderEntity);
			colliderTransform2D->Position = filter.Transform->Position;

			if (f.ResolveHashSet(filter.KnockedOut->PlayersReviving).Count > 0)
			{
				if (filter.KnockedOut->EndRevivingAt != FP._0 && filter.KnockedOut->EndRevivingAt <= f.Time)
				{
					var reviveHealthPercentage = GetConfigForKnockedOut(f, filter.KnockedOut).LifePercentageOnRevived;
					RevivePlayer(f, filter.Entity, filter.Stats, reviveHealthPercentage);
				}

				return;
			}


			if (filter.KnockedOut->NextDamageAt <= f.Time)
			{
				// Do damage
				var config = GetConfigForKnockedOut(f, filter.KnockedOut);
				filter.KnockedOut->NextDamageAt = f.Time + config.DamageTickInterval;
				var spell = new Spell
				{
					Id = Spell.KnockedOut,
					Victim = filter.Entity,
					Attacker = filter.KnockedOut->KnockedOutBy,
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


		public void OnFeetCollisionEnter(Frame f, EntityRef entity, EntityRef collidedWith, FPVector2 point)
		{
			if (!f.Unsafe.TryGetPointer<KnockedOutCollider>(collidedWith, out var knockedOutCollider))
			{
				return;
			}

			// It needs to be a player to revivew
			// Knocked out players cant revive other players
			if (!f.Has<PlayerCharacter>(entity) || f.Has<KnockedOut>(entity))
			{
				return;
			}

			// needs to be in the same team
			if (!TeamSystem.HasSameTeam(f, entity, knockedOutCollider->KnockedOutEntity))
			{
				return;
			}

			var knockedOut = f.Unsafe.GetPointer<KnockedOut>(knockedOutCollider->KnockedOutEntity);
			var reviving = f.ResolveHashSet(knockedOut->PlayersReviving);
			if (reviving.Count == 0)
			{
				var config = GetConfigForKnockedOut(f, knockedOut);
				knockedOut->EndRevivingAt = f.Time + config.TimeToRevive;
				// Why this mess exists?
				// Because I want to allow the bar to slowly decrease when a player leaves a collider, i have considered summing and decreasing a value per frame
				// but this is more performant, since we don't need to do anything on update loop, but the cost is readability
				if (knockedOut->BackAtZero > f.Time)
				{
					knockedOut->EndRevivingAt -= knockedOut->BackAtZero - f.Time;
				}

				f.Signals.OnPlayerStartReviving(knockedOutCollider->KnockedOutEntity);
				f.Events.OnPlayerStartReviving(knockedOutCollider->KnockedOutEntity);
			}

			reviving.Add(entity);
		}

		public void OnFeetCollisionLeft(Frame f, EntityRef entity, EntityRef collidedWith)
		{
			if (!f.Unsafe.TryGetPointer<KnockedOutCollider>(collidedWith, out var knockedOutCollider)
				|| !f.Unsafe.TryGetPointer<KnockedOut>(knockedOutCollider->KnockedOutEntity, out var knockedOut)
			   )
			{
				return;
			}

			StopRevivingPlayer(f, knockedOut, knockedOutCollider->KnockedOutEntity, entity);
		}

		public void OnRemoved(Frame f, EntityRef entity, KnockedOut* component)
		{
			f.Add<EntityDestroyer>(component->ColliderEntity);
		}

		public void GameEnded(Frame f)
		{
			ReviveAllKnockedOutPlayers(f);
			f.SystemDisable<ReviveSystem>();
		}

		public void PlayerDead(Frame f, PlayerRef playerDead, EntityRef entityDead)
		{
			var teamsAlive = new HashSet<int>();
			foreach (var (entity, _) in f.Unsafe.GetComponentBlockIterator<AlivePlayerCharacter>())
			{
				if (f.Unsafe.TryGetPointer<PlayerCharacter>(entity, out var t) && !IsKnockedOut(f, entity))
				{
					teamsAlive.Add(t->TeamId);
				}
			}

			foreach (var (entity, knockedOut) in f.Unsafe.GetComponentBlockIterator<KnockedOut>())
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
				f.Unsafe.GetPointer<Stats>(entity)->Kill(f, entity, knockedOut->KnockedOutBy);
			}
		}

		private static void RevivePlayer(Frame f, EntityRef entityRef, Stats* stats, FP lifePercentage)
		{
			// Revive player
			f.Remove<KnockedOut>(entityRef);
			stats->SetCurrentHealthPercentage(f, entityRef, lifePercentage);
			f.Events.OnPlayerRevived(entityRef);
			f.Signals.OnPlayerRevived(entityRef);
			f.Unsafe.GetPointer<Revivable>(entityRef)->RecoverMoveSpeedAfter =
				f.Time + GetConfig(f).ReviveAnimationDuration;
		}

		private static void ReviveAllKnockedOutPlayers(Frame f)
		{
			var players = f.Filter<Stats, KnockedOut, AlivePlayerCharacter>();
			while (players.NextUnsafe(out var entity, out var stats, out _, out _))
			{
				RevivePlayer(f, entity, stats, FP._1);
			}
		}

		#endregion

		#region Helpers

		public static bool IsKnockedOut(Frame f, EntityRef player)
		{
			return f.Has<KnockedOut>(player);
		}

		public static bool IsBeingRevived(Frame f, EntityRef player)
		{
			if (f.Unsafe.TryGetPointer<KnockedOut>(player, out var knockedOut))
			{
				return f.ResolveHashSet(knockedOut->PlayersReviving).Count > 0;
			}

			return false;
		}

		// ReSharper disable once UnusedMember.Global
		public static FP CalculateRevivePercentage(Frame f, KnockedOut* knockedOut)
		{
			var config = GetConfigForKnockedOut(f, knockedOut);
			// Player is being revived
			if (f.ResolveHashSet(knockedOut->PlayersReviving).Count > 0)
			{
				var timeLeftToRevive = knockedOut->EndRevivingAt - f.Time;
				return FP._1 - (timeLeftToRevive / config.TimeToRevive);
			}

			// Bar is still decreasing
			if (knockedOut->BackAtZero > f.Time)
			{
				var timeLeftDecreasing = knockedOut->BackAtZero - f.Time;
				return timeLeftDecreasing / config.TimeToRevive / config.ProgressDownSpeedMultiplier;
			}

			return FP._0;
		}

		#endregion

		#region Overwrites

		/// <summary>
		/// This is called at every time any player takes damage
		/// It is used to change the damage to the knockedout player
		/// </summary>
		public static bool OverwriteDamage(Frame f, EntityRef damaged, Spell* spell, int maxHealth, ref int damage)
		{
			if (
				spell->IsInstantKill() ||
				!f.Unsafe.TryGetPointer<KnockedOut>(damaged, out var knockedout))
			{
				return false;
			}


			if (spell->Id != Spell.KnockedOut)
			{
				damage = FPMath.CeilToInt((FP)damage * GetConfigForKnockedOut(f, knockedout).DamagePerShot);
				return true;
			}

			return false;
		}

		/// <summary>
		/// This is called everytime a player health reaches 0
		/// Returning true will skip the dead logic
		/// </summary>
		public static bool KnockOutPlayer(Frame f, EntityRef playerEntityRef, Spell* spell)
		{
			if (!f.Unsafe.TryGetPointer<Revivable>(playerEntityRef, out var revivable))
			{
				return false;
			}

			if (!CanBeKnockedOut(f, playerEntityRef, revivable))
			{
				return false;
			}

			f.Add<KnockedOut>(playerEntityRef, out var knockedOutComponent);
			knockedOutComponent->ConfigIndex = revivable->TimesKnockedOut;
			var config = GetConfigForKnockedOut(f, knockedOutComponent);
			var stats = f.Unsafe.GetPointer<Stats>(playerEntityRef);
			var transform = f.Unsafe.GetPointer<Transform2D>(playerEntityRef);
			stats->SetCurrentHealthPercentage(f, playerEntityRef, config.LifePercentageOnKnockedOut);
			knockedOutComponent->NextDamageAt = f.Time + config.DamageTickInterval;
			// Create collider to detect revive
			knockedOutComponent->ColliderEntity = f.Create();
			knockedOutComponent->KnockedOutBy = spell->Attacker;

			var shape2d = Shape2D.CreateCircle(config.ReviveColliderRange);
			var colliderEntity = knockedOutComponent->ColliderEntity;
			f.Add(colliderEntity, Transform2D.Create(transform->Position));
			f.Add(colliderEntity,
				PhysicsCollider2D.Create(f, shape2d, null, true, f.Context.TargetPlayerTriggersLayerIndex));
			f.Add(colliderEntity, new KnockedOutCollider()
			{
				KnockedOutEntity = playerEntityRef
			});
			f.Physics2D.SetCallbacks(colliderEntity,
				CallbackFlags.OnDynamicTriggerEnter | CallbackFlags.OnDynamicTriggerExit);
			f.Events.OnPlayerKnockedOut(spell->Attacker, playerEntityRef);
			f.Signals.OnPlayerKnockedOut(playerEntityRef);
			CheckIsRevivingOthers(f, playerEntityRef);
			revivable->TimesKnockedOut++;
			return true;
		}

		private static void StopRevivingPlayer(Frame f, KnockedOut* knockedOut, EntityRef knockedOutEntity,
											   EntityRef revivingEntity)
		{
			var resolveHashSet = f.ResolveHashSet(knockedOut->PlayersReviving);
			if (!resolveHashSet.Contains(revivingEntity)) return;
			resolveHashSet.Remove(revivingEntity);
			if (resolveHashSet.Count == 0)
			{
				var config = GetConfigForKnockedOut(f, knockedOut);
				// Progress on leave
				var timeReviving = config.TimeToRevive - (knockedOut->EndRevivingAt - f.Time);
				knockedOut->BackAtZero = f.Time + (timeReviving * config.ProgressDownSpeedMultiplier);
				// Reset damage timer 
				knockedOut->NextDamageAt = f.Time + config.DamageTickInterval;

				f.Signals.OnPlayerStopReviving(knockedOutEntity);
				f.Events.OnPlayerStopReviving(knockedOutEntity);
			}
		}


		private static void CheckIsRevivingOthers(Frame f, EntityRef knockedOutEntity)
		{
			if (!f.Unsafe.TryGetPointer<TeamMember>(knockedOutEntity, out var teamMember))
			{
				return;
			}

			foreach (var teamMate in f.ResolveHashSet(teamMember->TeamMates))
			{
				if (!f.Unsafe.TryGetPointer<KnockedOut>(teamMate, out var teammateKnockedOut))
				{
					continue;
				}

				if (f.ResolveHashSet(teammateKnockedOut->PlayersReviving).Contains(knockedOutEntity))
				{
					// The entities here are correct (they are not reversed)
					StopRevivingPlayer(f, teammateKnockedOut, teamMate, knockedOutEntity);
				}
			}
		}


		private static bool CanBeKnockedOut(Frame f, EntityRef entityRef, Revivable* revivable)
		{
			if (GetConfig(f).FullyDisable)
			{
				return false;
			}

			// reviving disabled
			if (f.Context.Mutators.HasFlagFast(Mutator.DisableRevive))
			{
				return false;
			}

			// Already knockedout
			if (f.Has<KnockedOut>(entityRef))
			{
				return false;
			}

			if (TeamSystem.GetAliveTeamMembersAmount(f, entityRef, false) == 0)
			{
				return false;
			}

			var maxRevives = GetConfig(f).PerGameMode.Get(f).AllowedRevives.Count;
			if (revivable->TimesKnockedOut >= maxRevives)
			{
				return false;
			}

			return true;
		}


		/// <summary>
		/// Called everytime a player dies, except when he got knockedout in that frame
		/// </summary>
		public static void OverwriteKiller(Frame f, EntityRef entity, Spell* spell)
		{
			// Gives the kill to the player who knockedout the damaged
			if (f.Unsafe.TryGetPointer<KnockedOut>(entity, out var knockedOut))
			{
				// if the player killhimself give the kill to the player who shot him, otherwise give the kill to the player who knockedout him
				if (knockedOut->KnockedOutBy != EntityRef.None && knockedOut->KnockedOutBy != entity)
				{
					spell->Attacker = knockedOut->KnockedOutBy;
				}
			}
		}

		public static void OverwriteMaxMoveSpeed(Frame f, EntityRef player, ref FP maxMoveSpeed)
		{
			byte configIndex = 255;
			if (f.Unsafe.TryGetPointer<KnockedOut>(player, out var knockedOut))
			{
				configIndex = knockedOut->ConfigIndex;
			}

			if (f.Unsafe.TryGetPointer<Revivable>(player, out var revivable) &&
				revivable->RecoverMoveSpeedAfter > f.Time)
			{
				configIndex = (byte)(revivable->TimesKnockedOut - 1);
			}

			if (configIndex != 255)
			{
				maxMoveSpeed *= GetReviveConfig(f, configIndex).MoveSpeedMultiplier;
			}
		}

		#endregion
	}
}