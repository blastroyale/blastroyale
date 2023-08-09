using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Photon.Deterministic;
using Quantum.Systems.Bots;
using static Quantum.RngSessionExtension;

namespace Quantum.Systems.Bots
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="BotCharacter"/>
	/// </summary>
	public unsafe class BotCharacterSystem : SystemMainThread,
											 ISignalHealthChangedFromAttacker,
											 ISignalAllPlayersSpawned, ISignalOnNavMeshWaypointReached, ISignalOnNavMeshSearchFailed, ISignalOnComponentRemoved<BotCharacter>
	{
		private BotSetup _botSetup = new BotSetup();
		private BattleRoyaleBot _battleRoyaleBot = new BattleRoyaleBot();
		private WanderAndShootBot _wanderAndShootBot = new WanderAndShootBot();
		private BotUpdateGlobalContext _updateContext = new BotUpdateGlobalContext();
		
		public struct BotCharacterFilter
		{
			public EntityRef Entity;
			public Transform3D* Transform;
			public BotCharacter* BotCharacter;
			public PlayerCharacter* PlayerCharacter;
			public AlivePlayerCharacter* AlivePlayerCharacter;
			public NavMeshPathfinder* NavMeshAgent;
			public CharacterController3D* Controller;
		}

		/// <inheritdoc />
		public void AllPlayersSpawned(Frame f)
		{
			var players = f.GetAllPlayerDatas();
			if (!players.Any())
			{
				return; // no players no game
			}

			var averagePlayerTrophies = Convert.ToUInt32(
				Math.Round(
					players
						.Average(p => p.PlayerTrophies)));
			_botSetup.InitializeBots(f, averagePlayerTrophies);
		}

		public override void Update(Frame f)
		{
			var it = f.Unsafe.FilterStruct<BotCharacterFilter>();
			it.UseCulling = true; 
			var filter = default(BotCharacterFilter);

			var botCtx = CreateGlobalContext(f);
			while (it.Next(&filter))
			{
				Update(f, botCtx, ref filter);
			}
		}

		private BotUpdateGlobalContext CreateGlobalContext(Frame f)
		{
			var circleCenter = FPVector2.Zero;
			var circleRadius = FP._0;
			var circleIsShrinking = false;
			var circleTargetCenter = FPVector2.Zero;
			var circleTargetRadius = FP._0;
			var circleTimeToShrink = FP._0;
			if (f.Unsafe.TryGetPointerSingleton<ShrinkingCircle>(out var circle))
			{
				circle->GetMovingCircle(f, out circleCenter, out circleRadius);
				circleIsShrinking = circle->ShrinkingStartTime <= f.Time;
				circleTargetCenter = circle->TargetCircleCenter;
				circleTargetRadius = circle->TargetRadius;
				circleTimeToShrink = circle->ShrinkingStartTime - f.Time;
			}
			_updateContext.circleCenter = circleCenter;
			_updateContext.circleRadius = circleRadius;
			_updateContext.circleIsShrinking = circleIsShrinking;
			_updateContext.circleTargetCenter = circleTargetCenter;
			_updateContext.circleTargetRadius = circleTargetRadius;
			_updateContext.circleTimeToShrink = circleTimeToShrink;
			return _updateContext;
		}

		/// <inheritdoc />
		private void Update(Frame f, BotUpdateGlobalContext botCtx, ref BotCharacterFilter filter)
		{
			if (QuantumFeatureFlags.FREEZE_BOTS) return;
			// If it's a deathmatch game mode and a bot is dead then we process respawn behaviour
			if (f.Context.GameModeConfig.BotRespawn && f.TryGet<DeadPlayerCharacter>(filter.Entity, out var deadBot))
			{
				// If the bot is dead and it's not yet the time to respawn then we skip the update
				if (f.Time < deadBot.TimeOfDeath + f.GameConfig.PlayerRespawnTime)
				{
					return;
				}

				var agent = f.Unsafe.GetPointer<HFSMAgent>(filter.Entity);
				HFSMManager.TriggerEvent(f, &agent->Data, filter.Entity, Constants.RespawnEvent);
			}

			// If a bot is not alive OR a bot is stunned 
			// then we don't go further with the behaviour
			if (!f.Has<AlivePlayerCharacter>(filter.Entity) || f.Has<Stun>(filter.Entity) ||
				// Hack to prevent the bots to act while player's camera animation is still playing
				f.Time < f.GameConfig.PlayerRespawnTime)
			{
				return;
			}

			// Don't do anything until grounded
			if (!filter.Controller->Grounded)
			{
				// Grounding is handled by skydiving if it exists; otherwise we need to call "Move" so gravity does its job
				if (!f.Context.GameModeConfig.SkydiveSpawn)
				{
					filter.Controller->Move(f, filter.Entity, FPVector3.Zero);
				}
				return;
			}

			if (!filter.BotCharacter->SpeedResetAfterLanding)
			{
				filter.BotCharacter->SpeedResetAfterLanding = true;

				// We call stop aiming once here to set the movement speed to a proper stat value
				filter.StopAiming(f);
			}

						
			// Distribute bot processing in 15 frames
			if (filter.BotCharacter->BotNameIndex % 15 == f.Number % 15)
			{
				return;
			}
			
			bool isTakingCircleDamage = filter.AlivePlayerCharacter->TakingCircleDamage;
			if (filter.BotCharacter->Target.IsValid && isTakingCircleDamage)
			{
				filter.ClearTarget(f);
			}
			else
			{
				// The bot will always try to keep aiming at his target
				// but if his target gets out of range, the target will get cleared
				filter.UpdateAimTarget(f);
			}

			// Bots look for others to shoot at not on every frame
			// It only does that when it does not have a target as thats costly on CPU
			// it can change targets if it gets damaged by another player closer to the bot
			if (!filter.BotCharacter->Target.IsValid && f.Time > filter.BotCharacter->NextLookForTargetsToShootAtTime)
			{
				filter.FindEnemiesToShootAt(f);
			}

			// Static bots don't move so no need to process anything else
			if (filter.BotCharacter->BehaviourType == BotBehaviourType.Static) return;
			if (filter.BotCharacter->BehaviourType == BotBehaviourType.WanderAndShoot)
			{
				_wanderAndShootBot.Update(f, ref filter, botCtx);
				return;
			}
			_battleRoyaleBot.Update(f, ref filter, isTakingCircleDamage, botCtx);
		}

		public void OnRemoved(Frame f, EntityRef entity, BotCharacter* component)
		{
			f.FreeHashSet(component->InvalidMoveTargets);
			component->InvalidMoveTargets = default;
		}

		public void OnNavMeshWaypointReached(Frame f, EntityRef entity, FPVector3 waypoint, Navigation.WaypointFlag waypointFlags, ref bool resetAgent)
		{
			BotLogger.LogAction(entity, $"Navmesh path ({waypointFlags.ToString()}) reached");

			if (!f.Unsafe.TryGetPointer<BotCharacter>(entity, out var bot)) return;
			
			// Every waypoint, if its not going towards consumable, bot re-evaluates his life
			if (bot->MoveTarget == entity || !bot->MoveTarget.IsValid)
			{
				bot->ResetTargetWaypoint(f);
			}
		}

		public void OnNavMeshSearchFailed(Frame f, EntityRef entity, ref bool resetAgent)
		{
			BotLogger.LogAction(entity, "pathfinding failed");

			if (f.Unsafe.TryGetPointer<BotCharacter>(entity, out var bot))
			{
				if (bot->MoveTarget != EntityRef.None && bot->MoveTarget != entity)
				{
					var invalid = f.ResolveHashSet(bot->InvalidMoveTargets);
					invalid.Add(bot->MoveTarget);
				}
				bot->ResetTargetWaypoint(f);
			}
		}

		/// <summary>
		/// If you attack a bot, he might get mad at you
		/// </summary>
		public void HealthChangedFromAttacker(Frame f, EntityRef entity, EntityRef attacker, int previousHealth)
		{
			if (!f.Unsafe.TryGetPointer<BotCharacter>(entity, out var bot)) return;
			if (attacker == bot->Target) return;
			if (f.Time < bot->NextLookForTargetsToShootAtTime) return;
			if (!f.Unsafe.TryGetPointer<Transform3D>(attacker, out var attackerLocation)) return;
			if (!f.Unsafe.TryGetPointer<Transform3D>(entity, out var botLocation)) return;
			if (!f.Unsafe.TryGetPointer<Transform3D>(bot->Target, out var targetLocation)) return;

			// If player attacks a bot that has no target, the bot will try to answer
			if (!bot->Target.IsValid)
			{
				 if (!bot->TryUseSpecials(f.Unsafe.GetPointer<PlayerCharacter>(entity), entity, f))
				{
					// If the bastard is shooting me from a longer range i can shoot him but i can see him
					if (QuantumHelpers.HasLineOfSight(f, botLocation->Position, attackerLocation->Position, out _))
					{
						var botMaxRange =  bot->GetMaxWeaponRange(entity, f);;
						// If enemy is not further than twice my range ill go for him
						botMaxRange *= botMaxRange;
						if (FPVector2.DistanceSquared(botLocation->Position.XZ, attackerLocation->Position.XZ) <
							botMaxRange * 2)
						{
							BotLogger.LogAction(entity, $"Going to kick {attacker} ass for shooting me from distance");
							if (BotMovement.MoveToLocation(f, entity, targetLocation->Position)) bot->SetHasWaypoint(entity, f);
							return;
						}
					}
				}
				return;
			}
		
			// If the attacker is closer to the bot than the current bot target, 50% swap chance
			if (f.RNG->NextBool() && FPVector2.DistanceSquared(botLocation->Position.XZ, attackerLocation->Position.XZ) < FPVector2.DistanceSquared(botLocation->Position.XZ, targetLocation->Position.XZ))
			{
				BotLogger.LogAction(entity, "Changing attacker to nearby attacker");
				bot->SetAttackTarget(entity, f, attacker);
			}
			bot->SetSearchForEnemyDelay(f);
		}
	}
}