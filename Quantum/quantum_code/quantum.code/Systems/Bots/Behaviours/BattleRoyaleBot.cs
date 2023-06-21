using System.Linq;
using Photon.Deterministic;
using static Quantum.Systems.Bots.BotCharacterSystem;

namespace Quantum.Systems.Bots
{
	public unsafe class BattleRoyaleBot
	{
		internal void Update(Frame f, ref BotCharacterFilter filter, bool isTakingCircleDamage, BotUpdateGlobalContext botCtx)
		{
			// Check move target in case it disappeared or a bot collected it and needs to move on
			// We set 0.5 second delay before letting a bot making another decision
			if (filter.BotCharacter->MoveTarget != EntityRef.None &&
				QuantumHelpers.IsDestroyed(f, filter.BotCharacter->MoveTarget))
			{
				filter.BotCharacter->MoveTarget = EntityRef.None;
				filter.NavMeshAgent->Stop(f, filter.Entity, true);
				filter.BotCharacter->NextDecisionTime = f.Time + FP._0_05;
			}

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
			if (filter.BotCharacter->MoveTarget != EntityRef.None
				&& f.Time < filter.BotCharacter->NextDecisionTime)
			{
				BotLogger.LogAction(ref filter, "waiting for decision " + (filter.BotCharacter->NextDecisionTime - f.Time));

				return;
			}

			///////////////////////////////////////////////////////////
			// The following code works in Decision time intervals
			///////////////////////////////////////////////////////////


			// If bot is collecting something at the moment then let this bot finish collection before doing anything else
			// and also clear StuckPosition so bot doesn't think that they stuck
			if (filter.BotCharacter->MoveTarget != EntityRef.None &&
				f.TryGet<Collectable>(filter.BotCharacter->MoveTarget, out var collectable) &&
				collectable.IsCollecting(filter.PlayerCharacter->Player))
			{
				BotLogger.LogAction(ref filter, "is collecting item");
				filter.BotCharacter->NextDecisionTime = collectable.CollectorsEndTime[filter.PlayerCharacter->Player] + FP._0_50;
				return;
			}

			filter.BotCharacter->NextDecisionTime = f.Time + filter.BotCharacter->DecisionInterval;

			// We stop aiming after the use of special because real players can't shoot and use specials at the same time
			// So we don't allow bots to do it as well
			if (TryUseSpecials(f, ref filter))
			{
				filter.StopAiming(f);
			}

			// In case a bot has a gun and ammo but switched to a hammer - we switch back to a gun
			if (filter.PlayerCharacter->HasMeleeWeapon(f, filter.Entity) &&
				f.TryGet<Stats>(filter.BotCharacter->MoveTarget, out var stats) && stats.CurrentAmmoPercent > FP._0)
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


			if (!FPMathHelpers.IsPositionInsideCircle(botCtx.circleTargetCenter, botCtx.circleTargetRadius, filter.Transform->Position.XZ) && botCtx.circleTimeToShrink < filter.BotCharacter->TimeStartRunningFromCircle)
			{
				TryGoToSafeArea(f, ref filter, botCtx.circleTargetCenter, botCtx.circleTargetRadius);
				BotLogger.LogAction(ref filter, "go to safe area");

				return;
			}

			// Let it finish the path
			if (filter.NavMeshAgent->IsActive)
			{
				BotLogger.LogAction(ref filter, "wait for path to finish waipoint_count:" + filter.NavMeshAgent->WaypointCount);
				return;
			}

			if (TryGoForClosestCollectable(f, ref filter, botCtx.circleCenter, botCtx.circleRadius, botCtx.circleIsShrinking))
			{
				BotLogger.LogAction(ref filter, "go to collectable");
				return;
			}

			if (TryStayCloseToTeammate(f, ref filter, botCtx.circleCenter, botCtx.circleRadius, botCtx.circleIsShrinking))
			{
				BotLogger.LogAction(ref filter, "stay close to team mate");
				return;
			}

			if (WanderInsideCircle(f, ref filter, botCtx.circleCenter, botCtx.circleRadius))
			{
				BotLogger.LogAction(ref filter, "wander");
				return;
			}

			BotLogger.LogAction(ref filter, "no action");
		}


		public void GoToCenterOfCircle(Frame f, ref BotCharacterFilter filter, FPVector2 circleCenter)
		{
			var newPosition = circleCenter;
			// We try to go into random position OR into circle center (it's good for a very small circle)
			if (QuantumHelpers.SetClosestTarget(f, filter.Entity, newPosition.XOY))
			{
				filter.BotCharacter->MoveTarget = filter.Entity;
				filter.BotCharacter->NextDecisionTime = f.Time + FP._5;
			}
		}

		// We loop through players to find a reference for alive teammate in case current is dead
		private void CheckOnTeammates(Frame f, ref BotCharacterFilter filter)
		{
			if (!QuantumHelpers.IsDestroyed(f, filter.BotCharacter->RandomTeammate))
			{
				return;
			}

			var randomTeammate = EntityRef.None;
			var team = f.Get<Targetable>(filter.Entity).Team;

			foreach (var candidate in f.Unsafe.GetComponentBlockIterator<Targetable>())
			{
				if (candidate.Component->Team == team)
				{
					randomTeammate = candidate.Entity;
					break;
				}
			}

			// If no teammates are alive then we let a bot think that they are alone in their team to not look for teammates anymore
			if (randomTeammate == EntityRef.None)
			{
				filter.BotCharacter->TeamSize = 1;
			}

			filter.BotCharacter->RandomTeammate = randomTeammate;
		}


		// We check specials and try to use them depending on their type if possible
		private bool TryUseSpecials(Frame f, ref BotCharacterFilter filter)
		{
			if (f.Time < filter.BotCharacter->NextAllowedSpecialUseTime)
			{
				return false;
			}

			if (!(f.RNG->Next() < filter.BotCharacter->ChanceToUseSpecial))
			{
				return false;
			}

			for (var i = 0; i <= 1; i++)
			{
				if (TryUseSpecial(f, filter.PlayerCharacter, i, filter.Entity, filter.BotCharacter->Target))
				{
					filter.BotCharacter->NextAllowedSpecialUseTime = f.Time + f.RNG->NextInclusive(filter.BotCharacter->SpecialCooldown);
					return true;
				}
			}

			return false;
		}

		private bool TryUseSpecial(Frame f, PlayerCharacter* playerCharacter, int specialIndex, EntityRef entity,
								   EntityRef target)
		{
			var special = playerCharacter->WeaponSlot->Specials[specialIndex];

			if ((target != EntityRef.None || special.SpecialType == SpecialType.ShieldSelfStatus) &&
				special.TryActivate(f, PlayerRef.None, entity, FPVector2.Zero, specialIndex))
			{
				playerCharacter->WeaponSlot->Specials[specialIndex] = special;
				return true;
			}

			return false;
		}

		private bool TryGoForClosestCollectable(Frame f, ref BotCharacterFilter filter, FPVector2 circleCenter, FP circleRadius, bool circleIsShrinking)
		{
			// Strategy is to pick up everything you can possible pick up
			// So we will look at closest pickups and discard things that we can't / don't need to pickup

			var sqrDistance = FP.MaxValue;
			var collectablePosition = FPVector3.Zero;
			var collectableEntity = EntityRef.None;
			var iterator = f.Unsafe.GetComponentBlockIterator<Collectable>();

			var botPosition = filter.Transform->Position;
			var stats = f.Get<Stats>(filter.Entity);
			var maxShields = stats.Values[(int)StatType.Shield].StatValue;
			var currentAmmo = stats.CurrentAmmoPercent;
			var maxHealth = stats.Values[(int)StatType.Health].StatValue;

			var needWeapon = filter.PlayerCharacter->HasMeleeWeapon(f, filter.Entity) || currentAmmo < FP.SmallestNonZero;
			var needAmmo = currentAmmo < FP._0_99;
			var needShields = stats.CurrentShield < maxShields;
			var needHealth = stats.CurrentHealth < maxHealth;

			var teamMembers = TeamHelpers.GetTeamMembers(f, filter.Entity);
			var invalidTargets = f.ResolveHashSet(filter.BotCharacter->InvalidMoveTargets);
			foreach (var collectableCandidate in iterator)
			{
				if (invalidTargets.Contains(collectableCandidate.Entity))
				{
					continue;
				}

				if (collectableCandidate.Component->GameId.IsInGroup(GameIdGroup.Weapon) && !needWeapon)
				{
					continue;
				}

				// If team mate is collecting ignore it!
				var teamMemberCollecting =
					teamMembers.Any(member =>
					{
						if (collectableCandidate.Component->IsCollecting(member.Component->Player))
						{
							return true;
						}

						if (f.TryGet<BotCharacter>(member.Entity, out var otherBot))
						{
							if (otherBot.MoveTarget == collectableCandidate.Entity)
							{
								return true;
							}
						}

						return false;
					});

				if (teamMemberCollecting)
				{
					continue;
				}


				if (f.TryGet<Consumable>(collectableCandidate.Entity, out var consumable))
				{
					var usefulConsumable = consumable.ConsumableType switch
					{
						ConsumableType.Ammo   => needAmmo,
						ConsumableType.Shield => needShields,
						ConsumableType.Health => needHealth,
						_                     => true
					};

					if (!usefulConsumable)
					{
						continue;
					}
				}

				var positionCandidate = f.Get<Transform3D>(collectableCandidate.Entity).Position;
				var newSqrDistance = (positionCandidate - botPosition).SqrMagnitude;

				if (IsInVisionRange(newSqrDistance, ref filter)
					&& newSqrDistance < sqrDistance
					&& IsInCircle(f, ref filter, circleCenter, circleRadius, circleIsShrinking, positionCandidate))
				{
					sqrDistance = newSqrDistance;
					collectablePosition = positionCandidate;
					collectableEntity = collectableCandidate.Entity;
				}
			}

			if (collectableEntity == EntityRef.None)
			{
				return false;
			}

			if (filter.NavMeshAgent->IsActive
				&& filter.BotCharacter->MoveTarget == collectableEntity)
			{
				return true;
			}

			if (QuantumHelpers.SetClosestTarget(f, filter.Entity, collectablePosition))
			{
				//Log.Warn("Bot: " + filter.Entity.Index + "; moves to Collectable: " + collectableEntity + " at " + collectablePosition);

				filter.BotCharacter->MoveTarget = collectableEntity;

				return true;
			}

			return false;
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

			if (QuantumHelpers.SetClosestTarget(f, filter.Entity, newPosition.XOY))
			{
				filter.BotCharacter->NextDecisionTime = f.Time + FP._10;
				filter.BotCharacter->MoveTarget = filter.Entity;
				BotLogger.LogAction(ref filter, $"Going towards direction random radius center:{circleCenter} Random:{newPosition} bot:{filter.Transform->Position}");
				return true;
			}

			// We try to go into random position OR into circle center (it's good for a very small circle)
			if (QuantumHelpers.SetClosestTarget(f, filter.Entity, circleCenter.XOY))
			{
				BotLogger.LogAction(ref filter, $"Going towards center: Center:{circleCenter} Random:{newPosition} bot:{filter.Transform->Position}");

				filter.BotCharacter->NextDecisionTime = f.Time + FP._10;
				filter.BotCharacter->MoveTarget = filter.Entity;
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

			var teammatePosition = filter.BotCharacter->RandomTeammate.GetPosition(f);
			var botPosition = filter.Transform->Position;
			var vectorToTeammate = teammatePosition - botPosition;
			var maxDistanceSquared = filter.BotCharacter->MaxDistanceToTeammateSquared;

			if (vectorToTeammate.SqrMagnitude < maxDistanceSquared)
			{
				return false;
			}

			var destination = filter.Transform->Position + vectorToTeammate.Normalized * (vectorToTeammate.Magnitude / FP._2);
			var isGoing = IsInCircle(f, ref filter, circleCenter, circleRadius, circleIsShrinking, destination)
				&& QuantumHelpers.SetClosestTarget(f, filter.Entity, destination);

			if (isGoing)
			{
				filter.BotCharacter->MoveTarget = filter.Entity;
			}

			return isGoing;
		}

		private bool IsInVisionRange(FP distanceSqr, ref BotCharacterFilter filter)
		{
			var visionRangeSqr = filter.BotCharacter->VisionRangeSqr;

			return visionRangeSqr < FP._0 || distanceSqr <= visionRangeSqr;
		}

		private bool IsInCircle(Frame f, ref BotCharacterFilter filter, FPVector2 circleCenter, FP circleRadius, bool circleIsShrinking, FPVector3 positionToCheck)
		{
			// If circle doesn't exist then we always return true
			if (circleRadius < FP.SmallestNonZero)
			{
				return true;
			}

			var distanceSqr = (positionToCheck.XZ - circleCenter).SqrMagnitude;

			// If circle is shrinking then it's risky to get to consumables on the edge so we don't do it
			if (circleIsShrinking)
			{
				return distanceSqr <= (circleRadius * circleRadius) * (FP._0_20 + FP._0_10);
			}

			return distanceSqr <= (circleRadius * circleRadius) * (FP._0_75);
		}


		/// <summary>
		/// It goes around a invisible circle to wonder in the borders
		/// </summary>
		public bool WanderInsideCircle(Frame f, ref BotCharacterFilter filter, FPVector2 circleCenter, FP circleRadius)
		{
			if (!TryToWanderToDirection(f, ref filter, circleCenter, circleRadius, filter.BotCharacter->WanderDirection))
			{
				// If fails to move change direction and try again
				filter.BotCharacter->WanderDirection = !filter.BotCharacter->WanderDirection;
				return TryToWanderToDirection(f, ref filter, circleCenter, circleRadius, filter.BotCharacter->WanderDirection);
			}

			return true;
		}


		private bool TryToWanderToDirection(Frame f, ref BotCharacterFilter filter, FPVector2 circleCenter, FP circleRadius, bool clockwise)
		{
			var position = filter.Transform->Position.XZ;

			// Player angle in relation to the center of the circle
			var distanceToCenter = FPVector2.Distance(position, circleCenter);
			var direction = position - circleCenter;
			var currentAngle = FPMath.Atan2(direction.Y, direction.X);


			// Let the player wander between 0 and 45º around the circle 
			// a bot has a fixed angle increase so doesn't keep going back to the same position, always going around the circle
			var randomizedAngle = currentAngle + f.RNG->NextInclusive(FP._0_20, FP._0_75) * (clockwise ? FP._1 : FP.Minus_1);

			// Also varying radius to be more natural, but a little bit per course adjust
			var radiusVariation = (circleRadius * FP._0_10) * f.RNG->NextInclusive(FP.Minus_1 * FP._1_50, FP._1_50);
			var playerTargetRadius = FPMath.Abs(distanceToCenter + radiusVariation);
			// and to be safe do not let the player go too close to the border
			var randomizedRadius = FPMath.Min(circleRadius * (FP._0_75), playerTargetRadius);


			var x = circleCenter.X + randomizedRadius * FPMath.Cos(randomizedAngle);
			var y = circleCenter.Y + randomizedRadius * FPMath.Sin(randomizedAngle);
			BotLogger.LogAction(ref filter, @$"From angle {FP.Rad2Deg * currentAngle} target angle: {FP.Rad2Deg * randomizedAngle}
From radius {distanceToCenter} to radius {randomizedRadius}
From position {position} to position ({x},{y})
");

			return QuantumHelpers.SetClosestTarget(f, filter.Entity, new FPVector3(x, FP._0, y));
		}
	}
}