using System.Linq;
using Photon.Deterministic;
using Quantum.Profiling;
using static Quantum.Systems.Bots.BotCharacterSystem;

namespace Quantum.Systems.Bots
{
	public unsafe class BattleRoyaleBot
	{
		public static FP MaxDistanceToTryToRevive = FP.FromString("25") * FP.FromString("25");

		internal void Update(Frame f, ref BotCharacterFilter filter, in bool isTakingCircleDamage, in BotUpdateGlobalContext botCtx)
		{
			filter.CleanDestroyedWaypointTarget(f);

			if (filter.BotCharacter->TeamSize > 1)
			{
				CheckOnTeammates(f, ref filter);
			}

			if (isTakingCircleDamage)
			{
				GoToCenterOfCircle(f, ref filter, botCtx.circleCenter);
				BotLogger.LogAction(ref filter, "go center of circle because of damage");
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
			BotLogger.LogAction(ref filter, "Taking decision");

			if (ReviveSystem.IsKnockedOut(f, filter.Entity))
			{
				// hard coded values so the bot will always go to teammate
				if (TryStayCloseToTeammate(f, ref filter, botCtx.circleCenter, FP._0, false))
				{
					BotLogger.LogAction(ref filter, "stay close to team mate");
					filter.BotCharacter->SetNextDecisionDelay(f, filter.BotCharacter->DecisionInterval);
					return;
				}
				
				return;
			}

			// If there is a knockedout member near him go help 
			if (TryGoReviveTeamMate(f, ref filter, botCtx)) return;


			// If bot is collecting something at the moment then let this bot finish collection before doing anything else
			if (filter.BotCharacter->MoveTarget != EntityRef.None &&
				f.TryGet<Collectable>(filter.BotCharacter->MoveTarget, out var collectable) &&
				collectable.TryGetCollectingEndTime(f, filter.Entity, out var collectionTime))
			{
				BotLogger.LogAction(ref filter, "is collecting item");
				filter.BotCharacter->NextDecisionTime = collectionTime + FP._0_10;
				return;
			}

			filter.BotCharacter->SetNextDecisionDelay(f, filter.BotCharacter->DecisionInterval);

			// We stop aiming after the use of special because real players can't shoot and use specials at the same time
			// So we don't allow bots to do it as well
			if (filter.BotCharacter->TryUseSpecials(filter.PlayerInventory, filter.Entity, f))
			{
				filter.StopAiming(f);
			}

			// In case a bot has a gun and no ammo we switch back to hammer
			if (!filter.PlayerCharacter->HasMeleeWeapon(f, filter.Entity) &&
				f.TryGet<Stats>(filter.Entity, out var ammoStats) &&
				ammoStats.CurrentAmmoPercent == FP._0)
			{
				filter.TrySwitchToHammer(f);
			}

			// In case a bot has a gun and ammo but switched to a hammer - we switch back to a gun
			if (filter.PlayerCharacter->HasMeleeWeapon(f, filter.Entity) &&
				f.TryGet<Stats>(filter.Entity, out var stats) && stats.CurrentAmmoPercent > FP._0)
			{
				for (var slotIndex = 1; slotIndex < filter.PlayerCharacter->WeaponSlots.Length; slotIndex++)
				{
					if (filter.PlayerCharacter->WeaponSlots[slotIndex].Weapon.IsValid())
					{
						filter.PlayerCharacter->EquipSlotWeapon(f, filter.Entity, slotIndex);
						break;
					}
				}
			}

			HostProfiler.Start("TryGoToSafeArea");
			if (!FPMathHelpers.IsPositionInsideCircle(botCtx.circleTargetCenter, botCtx.circleTargetRadius, filter.Transform->Position.XZ) && botCtx.circleTimeToShrink < filter.BotCharacter->TimeStartRunningFromCircle)
			{
				if (TryGoToSafeArea(f, ref filter, botCtx.circleTargetCenter, botCtx.circleTargetRadius))
				{
					BotLogger.LogAction(ref filter, "go to safe area");
					HostProfiler.End();
					return;
				}
			}
			HostProfiler.End();


			// Let it finish the path
			if (filter.BotCharacter->HasWaypoint(filter.Entity, f) && filter.Controller->Velocity != FPVector3.Zero && filter.NavMeshAgent->IsActive)
			{
				BotLogger.LogAction(ref filter, "wait for path to finish waypoint. MoveTarget now is: " + filter.BotCharacter->MoveTarget);
				return;
			}

			HostProfiler.Start("TryGoForClosestCollectable");
			if (filter.TryGoForClosestCollectable(f, botCtx.circleCenter, botCtx.circleRadius, botCtx.circleIsShrinking))
			{
				BotLogger.LogAction(ref filter, "go to collectable. MoveTarget now is: " + filter.BotCharacter->MoveTarget);
				HostProfiler.End();
				return;
			}
			HostProfiler.End();

			HostProfiler.Start("TryStayCloseToTeammate");
			if (TryStayCloseToTeammate(f, ref filter, botCtx.circleCenter, botCtx.circleRadius, botCtx.circleIsShrinking))
			{
				BotLogger.LogAction(ref filter, "stay close to team mate");
				HostProfiler.End();
				return;
			}
			HostProfiler.End();

			HostProfiler.Start("WanderInsideCircle");
			if (filter.BotCharacter->WanderInsideCircle(filter.Entity, f, botCtx.circleCenter, botCtx.circleRadius))
			{
				filter.SetHasWaypoint(f);
				BotLogger.LogAction(ref filter, "wander");
				HostProfiler.End();
				return;
			}
			HostProfiler.End();

			if (filter.BotCharacter->IsDoingJackShit())
			{
				filter.BotCharacter->SetNextDecisionDelay(f, FP._0_05);
			}

			BotLogger.LogAction(ref filter, "no action");
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
						filter.BotCharacter->SetNextDecisionDelay(f, filter.BotCharacter->DecisionInterval);
						return true;
					}

					// Someone else is reviving him so lets shoot other players
					if (reviving.Count > 0)
					{
						continue;
					}

					var teamMatePosition = f.Unsafe.GetPointer<Transform3D>(entityRef)->Position;
					// if the player is outside the safe zone that's his problem :D
					if (!BotState.IsInCircle(botCtx.circleCenter, botCtx.circleRadius, teamMatePosition))
					{
						continue;
					}

					var vectorToTeammate = teamMatePosition - filter.Transform->Position;

					if (vectorToTeammate.SqrMagnitude > MaxDistanceToTryToRevive) continue;

					var destination = teamMatePosition + (vectorToTeammate.Normalized * FP._0_50);

					filter.BotCharacter->SetNextDecisionDelay(f, filter.BotCharacter->DecisionInterval);

					if (BotMovement.MoveToLocation(f, filter.Entity, destination))
					{
						filter.BotCharacter->SetHasWaypoint(entityRef, f);
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
			if (BotMovement.MoveToLocation(f, filter.Entity, newPosition.XOY))
			{
				filter.SetHasWaypoint(f);
			}
			HostProfiler.End();
		}

		// We loop through players to find a reference for alive teammate in case current is dead
		private void CheckOnTeammates(Frame f, ref BotCharacterFilter filter)
		{
			if (!QuantumHelpers.IsDestroyed(f, filter.BotCharacter->RandomTeammate))
			{
				return;
			}

			HostProfiler.Start("CheckOnTeammates");

			var randomTeammate = EntityRef.None;

			foreach (var candidate in f.ResolveHashSet(filter.TeamMember->TeamMates))
			{
				if (candidate.IsValid && f.Has<AlivePlayerCharacter>(candidate))
				{
					randomTeammate = candidate;
					break;
				}
			}

			// If no teammates are alive then we let a bot think that they are alone in their team to not look for teammates anymore
			if (randomTeammate == EntityRef.None)
			{
				filter.BotCharacter->TeamSize = 1;
			}

			filter.BotCharacter->RandomTeammate = randomTeammate;
			HostProfiler.End();
		}


		private bool TryGoToSafeArea(Frame f, ref BotCharacterFilter filter, FPVector2 circleCenter, FP circleRadius)
		{
			// Not all game modes have a Shrinking Circle
			if (circleRadius < FP.SmallestNonZero)
			{
				return false;
			}

			var botPosition = filter.Transform->Position.XZ;

			var circleToVector = FPVector2.Normalize(botPosition - circleCenter);
			var range = circleRadius * (FP._0_75 + f.RNG->NextInclusive(FP._0_10 * FP.Minus_1, FP._0_10));
			var intersectionPoint = circleCenter + (circleToVector * range);
			var newPosition = intersectionPoint;

			if (BotMovement.MoveToLocation(f, filter.Entity, newPosition.XOY))
			{
				filter.SetHasWaypoint(f);
				BotLogger.LogAction(ref filter, $"Going towards direction random radius center:{circleCenter} Random:{newPosition} bot:{filter.Transform->Position}");
				return true;
			}

			// We try to go into random position OR into circle center (it's good for a very small circle)
			if (BotMovement.MoveToLocation(f, filter.Entity, circleCenter.XOY))
			{
				BotLogger.LogAction(ref filter, $"Going towards center: Center:{circleCenter} Random:{newPosition} bot:{filter.Transform->Position}");
				filter.SetHasWaypoint(f);
				return true;
			}

			return false;
		}

		private bool TryStayCloseToTeammate(Frame f, ref BotCharacterFilter filter, FPVector2 circleCenter, FP circleRadius, bool circleIsShrinking)
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

			var distance = vectorToTeammate.SqrMagnitude;
			if (distance < maxDistanceSquared)
			{
				HostProfiler.End();
				return false;
			}

			var destination = filter.Transform->Position + vectorToTeammate.Normalized * (vectorToTeammate.Magnitude / FP._2);
			var isGoing = BotState.IsInCircleWithSpareSpace(circleCenter, circleRadius, circleIsShrinking, destination)
				&& BotMovement.MoveToLocation(f, filter.Entity, destination);

			if (isGoing)
			{
				BotLogger.LogAction(ref filter, $"going to teammate {filter.BotCharacter->RandomTeammate}distance {distance}");
				filter.SetHasWaypoint(f);
			}

			HostProfiler.End();
			return isGoing;
		}
	}
}