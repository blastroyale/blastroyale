using System.Linq;
using Photon.Deterministic;
using Quantum.Profiling;
using static Quantum.Systems.Bots.BotCharacterSystem;

namespace Quantum.Systems.Bots
{
	public unsafe class BattleRoyaleBot
	{
		public static FP MaxDistanceToTryToRevive = FP.FromString("25") * FP.FromString("25");

		internal void Update(Frame f, ref BotCharacterFilter filter, in BotUpdateGlobalContext botCtx)
		{
			filter.CleanDestroyedWaypointTarget(f);

			if (filter.BotCharacter->TeamSize > 1)
			{
				CheckOnTeammates(f, filter.Transform, filter.TeamMember, filter.BotCharacter);
			}

			if (filter.AlivePlayerCharacter->TakingCircleDamage)
			{
				// The safety of combat in the dead zone is handled at the combat stage, here we just make sure bots keep fighting
				if (filter.BotCharacter->MovementType != BotMovementType.GoToSafeArea && !filter.BotCharacter->Target.IsValid)
				{
					filter.StopAiming(f);
					TryGoToSafeArea(f, ref filter, botCtx.circleCenter, botCtx.circleRadius);
				}

				return;
			}


			// Do not do any decision making if the time has not come, unless a bot have no target to move to
			if (!filter.BotCharacter->GetCanTakeDecision(f))
			{
				return;
			}

			///////////////////////////////////////////////////////////
			// The following code works in Decision time intervals
			///////////////////////////////////////////////////////////
			BotLogger.LogAction(f, ref filter, "Taking decision");

			if (ReviveSystem.IsKnockedOut(f, filter.Entity))
			{
				// Someone is already reviving the bot so don't move idiot
				if (ReviveSystem.IsBeingRevived(f, filter.Entity))
				{
					BotLogger.LogAction(f, filter.Entity, "wait being revived");
					return;
				}

				// hard coded values so the bot will always go to teammate
				if (TryStayCloseToTeammate(f, ref filter, botCtx.circleCenter, FP._0, false, realClose: true))
				{
					BotLogger.LogAction(f, ref filter, "go to teammate for help");
					filter.BotCharacter->SetNextDecisionDelay(filter.Entity, f, filter.BotCharacter->DecisionInterval);
					return;
				}

				return;
			}

			// If there is a knockedout member near him go help 
			if (TryGoReviveTeamMate(f, ref filter, botCtx)) return;


			// If bot is collecting something at the moment then let this bot finish collection before doing anything else
			if (filter.BotCharacter->MoveTarget != EntityRef.None &&
				f.TryGet<Collectable>(filter.BotCharacter->MoveTarget, out var collectable) &&
				collectable.TryGetCollectingEndTime(f, filter.BotCharacter->MoveTarget, filter.Entity, out var collectionTime))
			{
				BotLogger.LogAction(f, ref filter, "skip: collecting " + collectable.GameId.ToString());
				filter.BotCharacter->NextDecisionTime = collectionTime + FP._0_10;
				return;
			}

			filter.BotCharacter->SetNextDecisionDelay(filter.Entity, f, filter.BotCharacter->DecisionInterval);

			// We stop aiming after the use of special because real players can't shoot and use specials at the same time
			// So we don't allow bots to do it as well
			if (filter.BotCharacter->TryUseSpecials(filter.PlayerInventory, filter.Entity, f))
			{
				filter.StopAiming(f);
			}

			// In case a bot has a gun and no ammo we switch back to hammer
			if (!PlayerCharacter.HasMeleeWeapon(f, filter.Entity) &&
				f.TryGet<Stats>(filter.Entity, out var ammoStats) &&
				ammoStats.CurrentAmmoPercent == FP._0)
			{
				filter.TrySwitchToHammer(f);
			}

			// In case a bot has a gun and ammo but switched to a hammer - we switch back to a gun
			if (PlayerCharacter.HasMeleeWeapon(f, filter.Entity) &&
				f.TryGet<Stats>(filter.Entity, out var stats) && stats.CurrentAmmoPercent > FP._0)
			{
				for (var slotIndex = 1; slotIndex < filter.PlayerCharacter->WeaponSlots.Length; slotIndex++)
				{
					if (filter.PlayerCharacter->WeaponSlots[slotIndex].Weapon.IsValid())
					{
						BotLogger.LogAction(f, ref filter, "switch to weapon");
						filter.PlayerCharacter->EquipSlotWeapon(f, filter.Entity, slotIndex);
						break;
					}
				}
			}

			HostProfiler.Start("TryGoToSafeArea");
			if (!BotState.IsPositionSafe(botCtx, filter, filter.Transform->Position))
			{
				filter.StopAiming(f);
				if (TryGoToSafeArea(f, ref filter, botCtx.circleTargetCenter, botCtx.circleTargetRadius))
				{
					HostProfiler.End();
					return;
				}
			}

			HostProfiler.End();

			// Let it finish the path
			if (filter.BotCharacter->MovementType != BotMovementType.None && filter.NavMeshAgent->IsActive)
			{
				BotLogger.LogAction(f, ref filter, "wait for waypoint " + filter.BotCharacter->MoveTarget);
				return;
			}

			HostProfiler.Start("TryGoForClosestCollectable");
			if (filter.TryGoForClosestCollectable(f, botCtx))
			{
				filter.StopAiming(f);
				HostProfiler.End();
				return;
			}

			HostProfiler.End();

			HostProfiler.Start("TryStayCloseToTeammate");
			if (TryStayCloseToTeammate(f, ref filter, botCtx.circleCenter, botCtx.circleRadius, botCtx.circleIsShrinking))
			{
				HostProfiler.End();
				return;
			}

			HostProfiler.End();

			HostProfiler.Start("WanderInsideCircle");
			if (filter.BotCharacter->WanderInsideCircle(filter.Entity, f, botCtx.circleCenter, botCtx.circleRadius, BotMovementType.Wander))
			{
				BotLogger.LogAction(f, ref filter, "wander inside circle");
				filter.ClearTarget(f);
				HostProfiler.End();
				return;
			}

			HostProfiler.End();

			if (filter.BotCharacter->IsDoingJackShit())
			{
				filter.BotCharacter->SetNextDecisionDelay(filter.Entity, f, FP._0_05);
				BotLogger.LogAction(f, ref filter, "jackshit");
			}

			BotLogger.LogAction(f, ref filter, "no action");
		}

		private static bool TryGoReviveTeamMate(Frame f, ref BotCharacterFilter filter, in BotUpdateGlobalContext botCtx)
		{
			HostProfiler.Start("TryGoReviveTeamMate");
			foreach (var entityRef in f.ResolveHashSet(filter.TeamMember->TeamMates))
			{
				if (entityRef.IsValid && f.Unsafe.TryGetPointer<KnockedOut>(entityRef, out var knockedOut))
				{
					var reviving = f.ResolveHashSet(knockedOut->PlayersReviving);
					// Already reviving wait for finish
					if (reviving.Contains(filter.Entity))
					{
						BotLogger.LogAction(f, filter.Entity, "wait finish reviving");
						filter.BotCharacter->SetNextDecisionDelay(filter.Entity, f, filter.BotCharacter->DecisionInterval);
						return true;
					}

					// Someone else is reviving him so lets shoot other players
					if (reviving.Count > 0)
					{
						continue;
					}

					var teamMatePosition = f.Unsafe.GetPointer<Transform2D>(entityRef)->Position;
					// if the player is outside the safe zone that's his problem :D
					if (!BotState.IsInCircle(botCtx.circleCenter, botCtx.circleRadius, teamMatePosition))
					{
						continue;
					}

					var vectorToTeammate = teamMatePosition - filter.Transform->Position;

					if (vectorToTeammate.SqrMagnitude > MaxDistanceToTryToRevive) continue;

					var destination = teamMatePosition + (vectorToTeammate.Normalized * FP._0_50);

					filter.BotCharacter->SetNextDecisionDelay(filter.Entity, f, filter.BotCharacter->DecisionInterval);

					if (BotMovement.MoveToLocation(f, filter.Entity, destination, BotMovementType.GoCloserToTeamMate))
					{
						BotLogger.LogAction(f, filter.Entity, "go revive teammate");
						HostProfiler.End();
						return true;
					}
				}
			}

			HostProfiler.End();
			return false;
		}


		public void GoToCenterOfCircle(Frame f, ref BotCharacterFilter filter, FPVector2 circleCenter)
		{
			HostProfiler.Start("GoToCenterOfCircle");

			var newPosition = circleCenter;
			// We try to go into random position OR into circle center (it's good for a very small circle)
			BotMovement.MoveToLocation(f, filter.Entity, newPosition, BotMovementType.GoToSafeArea);


			HostProfiler.End();
		}

		// We loop through players to find a reference for alive teammate in case current is dead
		public void CheckOnTeammates(Frame f, Transform2D* botTransform, TeamMember* botTeamMember, BotCharacter* botCharacter, bool force = false)
		{
			if (!QuantumHelpers.IsDestroyed(f, botCharacter->RandomTeammate) && !force)
			{
				return;
			}

			HostProfiler.Start("CheckOnTeammates");

			var randomTeammate = EntityRef.None;
			var distance = FP.MaxValue;
			foreach (var candidate in f.ResolveHashSet(botTeamMember->TeamMates))
			{
				if (candidate.IsValid && f.Has<AlivePlayerCharacter>(candidate) && f.TryGet<Transform2D>(candidate, out var transform))
				{
					var candidateDistance = FPVector2.DistanceSquared(transform.Position, botTransform->Position);
					if (candidateDistance < distance)
					{
						// We have preference for non knocked out players
						if (randomTeammate != EntityRef.None && ReviveSystem.IsKnockedOut(f, candidate) && !ReviveSystem.IsKnockedOut(f, randomTeammate))
						{
							continue;
						}

						randomTeammate = candidate;
					}
				}
			}

			// If no teammates are alive then we let a bot think that they are alone in their team to not look for teammates anymore
			if (randomTeammate == EntityRef.None)
			{
				botCharacter->TeamSize = 1;
			}

			botCharacter->RandomTeammate = randomTeammate;
			HostProfiler.End();
		}


		private bool TryGoToSafeArea(Frame f, ref BotCharacterFilter filter, FPVector2 circleCenter, FP circleRadius)
		{
			// Not all game modes have a Shrinking Circle
			if (circleRadius < FP.SmallestNonZero)
			{
				return false;
			}

			if (filter.BotCharacter->MovementType == BotMovementType.GoToSafeArea)
			{
				BotLogger.LogAction(f, ref filter, "keep going safe area");
				return true;
			}

			var botPosition = filter.Transform->Position;

			var circleToVector = FPVector2.Normalize(botPosition - circleCenter);
			var range = circleRadius * (FP._0_50 + f.RNG->NextInclusive(FP._0_33 * FP.Minus_1, FP._0_33));
			var intersectionPoint = circleCenter + (circleToVector * range);
			var newPosition = intersectionPoint;


			if (BotMovement.MoveToLocation(f, filter.Entity, newPosition, BotMovementType.GoToSafeArea))
			{
				BotLogger.LogAction(f, ref filter, $"going towards circle center + random");
				return true;
			}

			// We try to go into random position OR into circle center (it's good for a very small circle)
			if (BotMovement.MoveToLocation(f, filter.Entity, circleCenter, BotMovementType.GoToSafeArea))
			{
				BotLogger.LogAction(f, ref filter, $"going towards circle center");
				return true;
			}

			return false;
		}

		private bool TryStayCloseToTeammate(Frame f, ref BotCharacterFilter filter, FPVector2 circleCenter, FP circleRadius, bool circleIsShrinking, bool realClose = false)
		{
			if (filter.BotCharacter->TeamSize <= 1 || QuantumHelpers.IsDestroyed(f, filter.BotCharacter->RandomTeammate))
			{
				return false;
			}

			HostProfiler.Start("TryStayCloseToTeammate");

			var teammatePosition = filter.BotCharacter->RandomTeammate.GetPosition(f);
			var botPosition = filter.Transform->Position;
			var vectorToTeammate = teammatePosition - botPosition;
			var maxDistanceSquared = filter.BotCharacter->MaxDistanceToTeammateSquared;
			if (realClose)
			{
				maxDistanceSquared = FP._3;
			}

			var distance = vectorToTeammate.SqrMagnitude;
			if (distance < maxDistanceSquared)
			{
				HostProfiler.End();
				return false;
			}

			var destination = teammatePosition - vectorToTeammate.Normalized * (realClose ? FP._1_50 : FP._5);
			var isGoing = BotState.IsInCircleWithSpareSpace(circleCenter, circleRadius, circleIsShrinking, destination)
				&& BotMovement.MoveToLocation(f, filter.Entity, destination, BotMovementType.GoCloserToTeamMate);

			if (isGoing)
			{
				BotLogger.LogAction(f, ref filter, $"going to teammate {filter.BotCharacter->RandomTeammate}distance {distance}");
			}

			HostProfiler.End();
			return isGoing;
		}

		public int RuntimeIndex { get; }
	}
}