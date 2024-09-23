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
											 ISignalOnPlayerKnockedOut,
											 ISignalOnPlayerStartReviving,
											 ISignalOnPlayerStopReviving,
											 ISignalPlayerDead
	{
#if BOT_DEBUG
		public static readonly bool Debug = true;
#else
		public static readonly bool Debug = false;
#endif
		private static int DistributeProcessingFrames = 20;

		private BotSetup _botSetup = new BotSetup();
		private BattleRoyaleBot _battleRoyaleBot = new BattleRoyaleBot();
		private WanderAndShootBot _wanderAndShootBot = new WanderAndShootBot();
		private StaticShootingBot _staticShootingBot = new StaticShootingBot();
		private BotUpdateGlobalContext _updateContext = new BotUpdateGlobalContext();

		public struct BotCharacterFilter
		{
			public EntityRef Entity;
			public Transform2D* Transform;
			public BotCharacter* BotCharacter;
			public PlayerCharacter* PlayerCharacter;
			public PlayerInventory* PlayerInventory;
			public AlivePlayerCharacter* AlivePlayerCharacter;
			public NavMeshPathfinder* NavMeshAgent;
			public TeamMember* TeamMember;

			public static BotCharacterFilter FromEntity(Frame f, EntityRef entity)
			{
				return new BotCharacterFilter
				{
					Entity = entity,
					Transform = f.Unsafe.GetPointer<Transform2D>(entity),
					BotCharacter = f.Unsafe.GetPointer<BotCharacter>(entity),
					PlayerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(entity),
					PlayerInventory = f.Unsafe.GetPointer<PlayerInventory>(entity),
					AlivePlayerCharacter = f.Unsafe.GetPointer<AlivePlayerCharacter>(entity),
					NavMeshAgent = f.Unsafe.GetPointer<NavMeshPathfinder>(entity),
					TeamMember = f.Unsafe.GetPointer<TeamMember>(entity)
				};
			}
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
			byte botIndex = 0;
			while (it.Next(&filter))
			{
				Update(f, botCtx, botIndex, ref filter);
				botIndex++;
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
		private void Update(Frame f, in BotUpdateGlobalContext botCtx, byte botIndex, ref BotCharacterFilter filter)
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
				return;
			}

			if (!filter.BotCharacter->SpeedResetAfterLanding)
			{
				filter.BotCharacter->SpeedResetAfterLanding = true;

				// We call stop aiming once here to set the movement speed to a proper stat value
				filter.StopAiming(f);
			}

			if (filter.BotCharacter->IsMoveSpeedReseted && f.Unsafe.GetPointer<Revivable>(filter.Entity)->RecoverMoveSpeedAfter < f.Time)
			{
				filter.StopAiming(f);
				filter.BotCharacter->IsMoveSpeedReseted = false;
			}

			bool canFight = filter.BotCharacter->WillFightInZone || BotState.IsPositionSafe(botCtx, filter, filter.Transform->Position);
			if (filter.BotCharacter->Target.IsValid && !canFight)
			{
				BotLogger.LogAction(f, filter.Entity, "stop fighthing taking damage from circle");
				filter.ClearTarget(f);
				filter.StopAiming(f);
			}
			else if (canFight)
			{
				// The bot will always try to keep aiming at his target
				// but if his target gets out of range, the target will get cleared
				filter.UpdateAimTarget(f);
			}

			// Distribute bot processing, this needs to be after aim, other wise it looks very cluncky
			if (botIndex % DistributeProcessingFrames != f.Number % DistributeProcessingFrames)
			{
				return;
			}

			// Bots look for others to shoot at not on every frame
			// It only does that when it does not have a target as thats costly on CPU
			// it can change targets if it gets damaged by another player closer to the bot
			if (!filter.BotCharacter->Target.IsValid && f.Time > filter.BotCharacter->NextLookForTargetsToShootAtTime && canFight)
			{
				filter.FindEnemiesToShootAt(f);
			}

			// Static bots don't move so no need to process anything else
			if (filter.BotCharacter->BehaviourType == BotBehaviourType.Static) return;
			if (filter.BotCharacter->BehaviourType == BotBehaviourType.StaticShooting)
			{
				_staticShootingBot.Update(f, ref filter);
				return;
			}

			if (filter.BotCharacter->BehaviourType == BotBehaviourType.WanderAndShoot)
			{
				_wanderAndShootBot.Update(f, ref filter);
				return;
			}

			_battleRoyaleBot.Update(f, ref filter, botCtx);
		}

		public void OnRemoved(Frame f, EntityRef entity, BotCharacter* component)
		{
			f.FreeHashSet(component->InvalidMoveTargets);
			component->InvalidMoveTargets = default;
		}

		public void OnNavMeshWaypointReached(Frame f, EntityRef entity, FPVector3 waypoint,
											 Navigation.WaypointFlag waypointFlags, ref bool resetAgent)
		{
			BotLogger.LogAction(f, entity, $"Navmesh path ({(waypointFlags.HasFlag(Navigation.WaypointFlag.Target) ? "target" : "")}) reached");

			if (!f.Unsafe.TryGetPointer<BotCharacter>(entity, out var bot)) return;

			// Every waypoint, if its not going towards consumable, bot re-evaluates his life
			if (!bot->MoveTarget.IsValid || waypointFlags.HasFlag(Navigation.WaypointFlag.Target))
			{
				BotLogger.LogAction(f, entity, $"arrived reset target");
				bot->ResetTargetWaypoint(entity, f);
			}
		}

		public void OnNavMeshSearchFailed(Frame f, EntityRef entity, ref bool resetAgent)
		{
			BotLogger.LogAction(f, entity, "pathfinding failed", TraceLevel.Error);
			if (f.Unsafe.TryGetPointer<BotCharacter>(entity, out var bot))
			{
				if (bot->MoveTarget != EntityRef.None && bot->MoveTarget != entity)
				{
					var invalid = f.ResolveHashSet(bot->InvalidMoveTargets);
					invalid.Add(bot->MoveTarget);
				}

				bot->ResetTargetWaypoint(entity, f);
				bot->MovementType = BotMovementType.None;
			}
		}

		/// <summary>
		/// If you attack a bot, he might get mad at you
		/// </summary>
		public void HealthChangedFromAttacker(Frame f, EntityRef entity, EntityRef attacker, int previousHealth)
		{
			if (!f.Unsafe.TryGetPointer<BotCharacter>(entity, out var bot)) return;
			if (ReviveSystem.IsKnockedOut(f, entity)) return;

			// Test change. Bots ALWAYS react on getting damaged
			//if (f.RNG->NextBool()) return; // 50% chance bots ignore

			// When bot has only melee and got attacked, something caused the bot sometime to freeze in place
			// So Nik put an easy solution here - don't react on damage if bot has only melee
			if (PlayerCharacter.HasMeleeWeapon(f, entity))
			{
				BotLogger.LogAction(f, entity, "got hit and ignored because don't have gun");
				return;
			}

			if (bot->IsStaticMovement()) return;
			if (attacker == bot->Target) return;
			if (!f.Unsafe.TryGetPointer<Transform2D>(attacker, out var attackerLocation)) return;
			if (!f.Unsafe.TryGetPointer<Transform2D>(entity, out var botLocation)) return;


			// If player attacks a bot that has no target, the bot will try to answer
			if (!bot->Target.IsValid)
			{
				if (bot->TryUseSpecials(f.Unsafe.GetPointer<PlayerInventory>(entity), entity, f)) return;
				var botMaxRange = bot->GetMaxWeaponRange(entity, f);


				// If enemy is not further than twice my range ill go for him
				botMaxRange *= botMaxRange;
				bot->Target = attacker;

				var distanceToAttacker = FPVector2.DistanceSquared(botLocation->Position, attackerLocation->Position);

				// when in range, ill just target back
				if (distanceToAttacker < botMaxRange)
				{
					bot->SetAttackTarget(entity, f, attacker);
					bot->SetSearchForEnemyDelay(f);
					bot->SetNextDecisionDelay(entity, f, FP._3);
					BotLogger.LogAction(f, entity, "fighting back");
				}
				else
				{
					bot->SetNextDecisionDelay(entity, f, FP._3);
					bot->MoveTarget = attacker;
					bot->NextLookForTargetsToShootAtTime = f.Time;
					BotMovement.MoveToLocation(f, entity, attackerLocation->Position, BotMovementType.Combat);
					BotLogger.LogAction(f, entity, $"getting closer to shooter");
				}
			}
			else // bot already has a valid target
			{
				if (!f.Unsafe.TryGetPointer<Transform2D>(bot->Target, out var targetLocation))
				{
					return;
				}

				var distanceToAttacker = FPVector2.DistanceSquared(botLocation->Position, attackerLocation->Position);

				// If the attacker is closer to the bot than the current bot target, 50% swap chance
				if (f.RNG->NextBool() &&
					distanceToAttacker <
					FPVector2.DistanceSquared(botLocation->Position, targetLocation->Position))
				{
					BotLogger.LogAction(f, entity, "swapping bot target to attacker");
					bot->SetNextDecisionDelay(entity, f, FP._3);
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
			bot->SetNextDecisionDelay(entity, f, 0);

			BotLogger.LogAction(f, entity, "revived force rethink");
			if (f.Unsafe.TryGetPointer<AIBlackboardComponent>(entity, out var bb))
			{
				bb->Set(f, Constants.IS_KNOCKED_OUT, false);
			}
		}

		public void OnPlayerKnockedOut(Frame f, EntityRef knockedOutEntity)
		{
			OnTeamMateKnockedOut(f, knockedOutEntity);

			if (!f.Unsafe.TryGetPointer<BotCharacter>(knockedOutEntity, out var bot))
			{
				return;
			}

			_battleRoyaleBot.CheckOnTeammates(f, f.Unsafe.GetPointer<Transform2D>(knockedOutEntity), f.Unsafe.GetPointer<TeamMember>(knockedOutEntity), bot, true);
			// Stop bot path because it will change it speed and behaviour
			BotShooting.StopAiming(f, bot, knockedOutEntity);
			BotLogger.LogAction(f, knockedOutEntity, "stop movement knocked out");
			bot->StopMovement(f, knockedOutEntity);

			bot->SetNextDecisionDelay(knockedOutEntity, f, 0);

			if (f.Unsafe.TryGetPointer<AIBlackboardComponent>(knockedOutEntity, out var bb))
			{
				bb->Set(f, Constants.IS_KNOCKED_OUT, true);
			}

			if (f.Unsafe.TryGetPointer<HFSMAgent>(knockedOutEntity, out var agent))
			{
				HFSMManager.TriggerEvent(f, &agent->Data, knockedOutEntity, Constants.KNOCKED_OUT_EVENT);
			}
		}

		/// <summary>
		///  When a teammate gets knocked out stop everything and go help them
		/// </summary>
		private void OnTeamMateKnockedOut(Frame f, EntityRef knockedOutEntity)
		{
			if (!f.Unsafe.TryGetPointer<TeamMember>(knockedOutEntity, out var teamMember)) return;

			foreach (var teamMemberEntity in f.ResolveHashSet(teamMember->TeamMates))
			{
				// If my team mate is a bot
				if (knockedOutEntity.IsValid && f.Unsafe.TryGetPointer<BotCharacter>(teamMemberEntity, out var teamMateBot))
				{
					if (!ReviveSystem.IsKnockedOut(f, teamMemberEntity))
					{
						BotLogger.LogAction(f, knockedOutEntity, "teammate got down, force thinking ");
						teamMateBot->NextDecisionTime = f.Time;
						teamMateBot->StopMovement(f, teamMemberEntity);
					}
					else
					{
						// Force looking for a teammate that is not down
						BotLogger.LogAction(f, knockedOutEntity, "teammate is down, he may not help me");
						_battleRoyaleBot.CheckOnTeammates(f,
							f.Unsafe.GetPointer<Transform2D>(teamMemberEntity),
							f.Unsafe.GetPointer<TeamMember>(teamMemberEntity),
							teamMateBot, true);
					}
				}
			}
		}

		public void OnPlayerStartReviving(Frame f, EntityRef entity)
		{
			if (!f.Unsafe.TryGetPointer<BotCharacter>(entity, out var bot)) return;
			BotLogger.LogAction(f, entity, "force rethink start reviving");
			bot->StopMovement(f, entity);
		}

		public void OnPlayerStopReviving(Frame f, EntityRef Entity)
		{
			if (!f.Unsafe.TryGetPointer<BotCharacter>(Entity, out var bot)) return;
			BotLogger.LogAction(f, Entity, "force rethink stop reviving");
			bot->StopMovement(f, Entity);
		}

		public void PlayerDead(Frame f, PlayerRef playerDead, EntityRef entityDead)
		{
			var shouldUseSmart = f.ComponentCount<BotCharacter>() < 15;
			if (!shouldUseSmart)
			{
				return;
			}

			var config = f.DynamicAssetDB.FindAsset<NavMeshAgentConfig>(BotSetup.FasterIntervalBotConfig);
			foreach (var (entity, component) in f.Unsafe.GetComponentBlockIterator<NavMeshPathfinder>())
			{
				if (component->ConfigId == config.Guid)
				{
					continue;
				}

				component->SetConfig(f, entity, config);
			}
		}
	}
}