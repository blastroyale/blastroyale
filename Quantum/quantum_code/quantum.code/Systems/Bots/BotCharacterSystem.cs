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
											 ISignalAllPlayersSpawned, ISignalOnNavMeshWaypointReached, ISignalOnNavMeshSearchFailed, ISignalOnComponentRemoved<BotCharacter>
	{
		private BotSetup _botSetup = new BotSetup();
		private BattleRoyaleBot _battleRoyaleBot = new BattleRoyaleBot();
		private WanderAndShootBot _wanderAndShootBot = new WanderAndShootBot();

		public struct BotCharacterFilter
		{
			public EntityRef Entity;
			public Transform3D* Transform;
			public BotCharacter* BotCharacter;
			public PlayerCharacter* PlayerCharacter;
			public AlivePlayerCharacter* AlivePlayerCharacter;
			public NavMeshPathfinder* NavMeshAgent;
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
			// grab iterator
			var it = f.Unsafe.FilterStruct<BotCharacterFilter>();
			// set culling flag
			it.UseCulling = true;
			// execute filter loop
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
			if (f.TryGetSingleton<ShrinkingCircle>(out var circle))
			{
				circle.GetMovingCircle(f, out circleCenter, out circleRadius);
				circleIsShrinking = circle.ShrinkingStartTime <= f.Time;
				circleTargetCenter = circle.TargetCircleCenter;
				circleTargetRadius = circle.TargetRadius;
				circleTimeToShrink = circle.ShrinkingStartTime - f.Time;
			}

			return new BotUpdateGlobalContext()
			{
				circleCenter = circleCenter,
				circleRadius = circleRadius,
				circleIsShrinking = circleIsShrinking,
				circleTargetCenter = circleTargetCenter,
				circleTargetRadius = circleTargetRadius,
				circleTimeToShrink = circleTimeToShrink,
			};
		}

		/// <inheritdoc />
		private void Update(Frame f, BotUpdateGlobalContext botCtx, ref BotCharacterFilter filter)
		{
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

			var kcc = f.Unsafe.GetPointer<CharacterController3D>(filter.Entity);

			// Don't do anything until grounded
			if (!kcc->Grounded)
			{
				// Grounding is handled by skydiving if it exists; otherwise we need to call "Move" so gravity does its job
				if (!f.Context.GameModeConfig.SkydiveSpawn)
				{
					kcc->Move(f, filter.Entity, FPVector3.Zero);
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
				filter.UpdateAimTarget(f);
			}

			var weaponConfig = f.WeaponConfigs.GetConfig(filter.PlayerCharacter->CurrentWeapon.GameId);

			// Bots look for others to shoot at not on every frame
			if (f.Time > filter.BotCharacter->NextLookForTargetsToShootAtTime)
			{
				filter.CheckEnemiesToShooAt(f, ref weaponConfig);

				filter.BotCharacter->NextLookForTargetsToShootAtTime =
					f.Time + filter.BotCharacter->LookForTargetsToShootAtInterval;
			}

			// Static bots don't move so no need to process anything else
			if (filter.BotCharacter->BehaviourType == BotBehaviourType.Static)
			{
				return;
			}

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

			// if the navigation finished
			if ((waypointFlags & Navigation.WaypointFlag.LinkEnd) == 0) return;

			if (!f.Unsafe.TryGetPointer<BotCharacter>(entity, out var bot)) return;

			// If the target is the bot it means is moving to another position in the map
			if (bot->MoveTarget == entity)
			{
				bot->MoveTarget = EntityRef.None;
				// let the bot do a quick decision after this
				bot->NextDecisionTime = f.Time + FP._0_01;
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

				bot->MoveTarget = EntityRef.None;
				// If the target is the bot it means is moving to another position in the map
				// let the bot do a quick decision after this
				bot->NextDecisionTime = f.Time + FP._0_01;
			}
		}
	}
}