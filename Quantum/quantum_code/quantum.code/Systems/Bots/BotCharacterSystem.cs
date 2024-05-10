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
											 ISignalAllPlayersSpawned, ISignalOnNavMeshWaypointReached,
											 ISignalOnNavMeshSearchFailed, ISignalOnComponentRemoved<BotCharacter>,
											 ISignalOnPlayerRevived,
											 ISignalOnPlayerKnockedOut
	{
		private BotSetup _botSetup = new BotSetup();
		private BattleRoyaleBot _battleRoyaleBot = new BattleRoyaleBot();
		private WanderAndShootBot _wanderAndShootBot = new WanderAndShootBot();
		private StaticShootingBot _staticShootingBot = new StaticShootingBot();
		private BotUpdateGlobalContext _updateContext = new BotUpdateGlobalContext();

		public struct BotCharacterFilter
		{
			public EntityRef Entity;
			public Transform3D* Transform;
			public BotCharacter* BotCharacter;
			public PlayerCharacter* PlayerCharacter;
			public PlayerInventory* PlayerInventory;
			public AlivePlayerCharacter* AlivePlayerCharacter;
			public NavMeshPathfinder* NavMeshAgent;
			public CharacterController3D* Controller;
			public TeamMember* TeamMember;
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
			if (_updateContext.FrameNumber == f.Number) return _updateContext;

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

			_updateContext.FrameNumber = f.Number;
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

			// If a bot is not alive OR a bot is stunned 
			// then we don't go further with the behaviour
			if (!f.Has<AlivePlayerCharacter>(filter.Entity) || f.Has<Stun>(filter.Entity) ||
				// Hack to prevent the bots to act while player's camera animation is still playing
				f.Time < f.GameConfig.PlayerRespawnTime)
			{
				return;
			}

			// Don't do anything when skydiving
			if (filter.PlayerCharacter->IsSkydiving(f, filter.Entity))
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


			// Distribute bot processing in 30 frames
			if (filter.BotCharacter->BotNameIndex % 30 == f.Number % 30)
			{
				return;
			}

			if (filter.BotCharacter->IsMoveSpeedReseted && f.Unsafe.GetPointer<Revivable>(filter.Entity)->RecoverMoveSpeedAfter < f.Time)
			{
				filter.StopAiming(f);
				filter.BotCharacter->IsMoveSpeedReseted = false;
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
			if (filter.BotCharacter->BehaviourType == BotBehaviourType.StaticShooting)
			{
				_staticShootingBot.Update(f, ref filter);
			}

			if (filter.BotCharacter->BehaviourType == BotBehaviourType.WanderAndShoot)
			{
				_wanderAndShootBot.Update(f, ref filter);
				return;
			}


			_battleRoyaleBot.Update(f, ref filter, isTakingCircleDamage, botCtx);
		}

		public void OnRemoved(Frame f, EntityRef entity, BotCharacter* component)
		{
			f.FreeHashSet(component->InvalidMoveTargets);
			component->InvalidMoveTargets = default;
		}

		public void OnNavMeshWaypointReached(Frame f, EntityRef entity, FPVector3 waypoint,
											 Navigation.WaypointFlag waypointFlags, ref bool resetAgent)
		{
			BotLogger.LogAction(entity, $"Navmesh path ({waypointFlags.ToString()}) reached");

			if (!f.Unsafe.TryGetPointer<BotCharacter>(entity, out var bot)) return;

			// Every waypoint, if its not going towards consumable, bot re-evaluates his life
			if (bot->MoveTarget == entity || !bot->MoveTarget.IsValid || waypointFlags.HasFlag(Navigation.WaypointFlag.Target))
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
			if (ReviveSystem.IsKnockedOut(f, entity)) return;
			
			// Test change. Bots ALWAYS react on getting damaged
			//if (f.RNG->NextBool()) return; // 50% chance bots ignore

			BotLogger.LogAction(entity, $"Bot took damage from {attacker}");
			if (!f.Unsafe.TryGetPointer<BotCharacter>(entity, out var bot)) return;
			if (bot->IsStaticMovement()) return;
			if (attacker == bot->Target) return;
			if (!f.Unsafe.TryGetPointer<Transform3D>(attacker, out var attackerLocation)) return;
			if (!f.Unsafe.TryGetPointer<Transform3D>(entity, out var botLocation)) return;

			var distanceToAttacker =
				FPVector2.DistanceSquared(botLocation->Position.XZ, attackerLocation->Position.XZ);

			// If player attacks a bot that has no target, the bot will try to answer
			if (!bot->Target.IsValid)
			{
				if (bot->TryUseSpecials(f.Unsafe.GetPointer<PlayerInventory>(entity), entity, f)) return;
				var botMaxRange = bot->GetMaxWeaponRange(entity, f.Unsafe.GetPointer<PlayerCharacter>(entity), f);

				BotLogger.LogAction(entity, $"Going to kick {attacker} ass for shooting me from distance");

				// If enemy is not further than twice my range ill go for him
				botMaxRange *= botMaxRange;
				bot->Target = attacker;

				// when in range, ill just target back
				if (distanceToAttacker < botMaxRange)
				{
					bot->SetAttackTarget(entity, f, attacker);
					bot->SetSearchForEnemyDelay(f);
					bot->SetNextDecisionDelay(f, FP._3);
					BotLogger.LogAction(entity, "Fighting back");
				}
				else
				{
					// A bot goes to the attacker if a bot has a gun. Otherwise - run away
					if (!f.Unsafe.GetPointer<PlayerCharacter>(entity)->HasMeleeWeapon(f, entity))
					{
						bot->SetHasWaypoint(entity, f);
						bot->MoveTarget = attacker;
						bot->SetNextDecisionDelay(f, FP._3);
						bot->NextLookForTargetsToShootAtTime = f.Time;
						BotMovement.MoveToLocation(f, entity, attackerLocation->Position);
						BotLogger.LogAction(entity, $"Attacker too distant, coming closer");
					}
					else
					{
						var runawayPoint = (attackerLocation->Position - botLocation->Position).Normalized * FP._10;
						if (BotMovement.MoveToLocation(f, entity, runawayPoint))
						{
							bot->SetNextDecisionDelay(f, bot->DecisionInterval);
							BotLogger.LogAction(entity, $"Attacker too distant, but I have melee; running away");
						}
					}
				}
			}
			else // bot already has a valid target
			{
				if (!f.Unsafe.TryGetPointer<Transform3D>(bot->Target, out var targetLocation))
				{
					return;
				}

				// If the attacker is closer to the bot than the current bot target, 50% swap chance
				if (f.RNG->NextBool() &&
					distanceToAttacker <
					FPVector2.DistanceSquared(botLocation->Position.XZ, targetLocation->Position.XZ))
				{
					BotLogger.LogAction(entity, "Changing attacker to nearby attacker");
					bot->SetNextDecisionDelay(f, FP._3);
					bot->SetSearchForEnemyDelay(f);
					bot->SetAttackTarget(entity, f, attacker);
				}
			}
		}

		public void OnPlayerRevived(Frame f, EntityRef entity)
		{
			if (!f.Unsafe.TryGetPointer<BotCharacter>(entity, out var bot))
			{
				return;
			}

			bot->IsMoveSpeedReseted = true;
			bot->SetNextDecisionDelay(f, 0);

			if (f.Unsafe.TryGetPointer<AIBlackboardComponent>(entity, out var bb))
			{
				bb->Set(f, Constants.IsKnockedOut, false);
			}
		}

		public void OnPlayerKnockedOut(Frame f, EntityRef knockedOutEntity)
		{
			OnTeamMateKnockedOut(f, knockedOutEntity);

			if (!f.Unsafe.TryGetPointer<BotCharacter>(knockedOutEntity, out var bot))
			{
				return;
			}

			// Stop bot path because it will change it speed and behaviour
			if (f.Unsafe.TryGetPointer<NavMeshPathfinder>(knockedOutEntity, out var navMeshAgent))
			{
				navMeshAgent->Stop(f, knockedOutEntity, true);
				bot->ResetTargetWaypoint(f);
				bot->Target = EntityRef.None;
			}

			BotShooting.StopAiming(f, bot, knockedOutEntity);
			bot->SetNextDecisionDelay(f, 0);

			if (f.Unsafe.TryGetPointer<AIBlackboardComponent>(knockedOutEntity, out var bb))
			{
				bb->Set(f, Constants.IsKnockedOut, true);
			}

			if (f.Unsafe.TryGetPointer<HFSMAgent>(knockedOutEntity, out var agent))
			{
				HFSMManager.TriggerEvent(f, &agent->Data, knockedOutEntity, Constants.KnockedOutEvent);
			}
		}

		/// <summary>
		///  When a teammate gets knocked out stop everything and go help them
		/// </summary>
		private static void OnTeamMateKnockedOut(Frame f, EntityRef knockedOutEntity)
		{
			if (!f.Unsafe.TryGetPointer<TeamMember>(knockedOutEntity, out var teamMember)) return;

			foreach (var teamMemberEntity in f.ResolveHashSet(teamMember->TeamMates))
			{
				if (knockedOutEntity.IsValid && f.Unsafe.TryGetPointer<BotCharacter>(teamMemberEntity, out var teamMateBot) && !ReviveSystem.IsKnockedOut(f, teamMemberEntity))
				{
					teamMateBot->NextDecisionTime = f.Time;
					teamMateBot->ResetTargetWaypoint(f);
					f.Unsafe.GetPointer<NavMeshPathfinder>(teamMemberEntity)->Stop(f, teamMemberEntity);
				}
			}
		}
	}
}