using Photon.Deterministic;
using Quantum.Profiling;

namespace Quantum.Systems.Bots
{
	public unsafe static class BotMovement
	{
		/// <summary>
		/// It goes around a invisible circle to wonder in the borders
		/// </summary>
		public static bool WanderInsideCircle(this ref BotCharacter bot, in EntityRef botEntity, Frame f, in FPVector2 circleCenter, in FP circleRadius, BotMovementType botMovementType)
		{
			if (!bot.TryWanderInsideCircle(botEntity, f, circleCenter, circleRadius, bot.WanderDirection, botMovementType))
			{
				// If fails to move change direction and try again
				bot.WanderDirection = !bot.WanderDirection;
				return bot.TryWanderInsideCircle(botEntity, f, circleCenter, circleRadius, bot.WanderDirection, botMovementType);
			}

			return true;
		}

		/// <summary>
		/// Checks if the bot waypoint is destroyed, and if so, cleans current waypoint
		/// </summary>
		public static void CleanDestroyedWaypointTarget(this ref BotCharacterSystem.BotCharacterFilter filter, Frame f)
		{
			HostProfiler.Start("CleanDestoryedWaypoint");
			if (filter.BotCharacter->MoveTarget != EntityRef.None &&
				QuantumHelpers.IsDestroyed(f, filter.BotCharacter->MoveTarget))
			{
				BotLogger.LogAction(f, filter.Entity, "cleaning destroyed movetarget");
				filter.BotCharacter->StopMovement(f, filter.Entity, filter.NavMeshAgent);
				filter.BotCharacter->MoveTarget = EntityRef.None;
			}

			HostProfiler.End();
		}

		public static bool IsStaticMovement(this ref BotCharacter botCharacter)
		{
			return botCharacter.BehaviourType == BotBehaviourType.Static || botCharacter.BehaviourType == BotBehaviourType.StaticShooting;
		}

		/// <summary>
		/// Randomizes a position inside a circle for the bot to move to
		/// </summary>
		public static bool TryWanderInsideCircle(this ref BotCharacter bot, in EntityRef botEntity, Frame f, in FPVector2 circleCenter, in FP circleRadius, in bool clockwise, BotMovementType movementType)
		{
			var position = botEntity.GetPosition(f);

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
			BotLogger.LogAction(f, botEntity, @$"From angle {FP.Rad2Deg * currentAngle} target angle: {FP.Rad2Deg * randomizedAngle}
From radius {distanceToCenter} to radius {randomizedRadius}
From position {position} to position ({x},{y})
");
			return MoveToLocation(f, botEntity, new FPVector2(x, y), movementType);
		}

		/// <summary>
		/// Set's the navmesh agent of the given entity's target position to as closest as possible
		/// </summary>
		public static bool MoveToLocation(Frame f, in EntityRef e, in FPVector2 destination, BotMovementType type)
		{
			var agent = f.Unsafe.GetPointer<NavMeshPathfinder>(e);
			f.Unsafe.GetPointer<BotCharacter>(e)->MovementType = type;
			var navMesh = f.NavMesh;
			agent->SetTarget(f, destination.XOY, navMesh);
			return true;
		}
	}
}