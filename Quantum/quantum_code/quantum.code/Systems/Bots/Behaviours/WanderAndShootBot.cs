using System;
using Photon.Deterministic;
using static Quantum.Systems.Bots.BotCharacterSystem;

namespace Quantum.Systems.Bots
{
	public unsafe class WanderAndShootBot
	{
		private const int RETRIES = 10;
		private FP rad360 = FP.Rad_180 * 2;

		internal void Update(Frame f, ref BotCharacterFilter filter)
		{
			// Do not do any decision making if the time has not come, unless a bot have no target to move to
			if (filter.BotCharacter->MoveTarget != EntityRef.None
				&& f.Time < filter.BotCharacter->NextDecisionTime)
			{
				return;
			}

			var angle = f.RNG->NextInclusive(FP._0, rad360);
			// It will try to go from 0 to 338, it doesn't make sence to go back to 360
			var variationPerRetry = (rad360 - FP.Rad_22_50) / RETRIES;
			for (var i = 0; i < RETRIES; i++)
			{
				if (TryToWanderToAngle(f, ref filter, filter.Transform->Position, FP._5, angle))
				{
					break;
				}

				angle += variationPerRetry;
			}
		}

		private bool TryToWanderToAngle(Frame f, ref BotCharacterFilter filter, FPVector2 circleCenter, FP circleRadius, FP angleRad)
		{
			var x = circleCenter.X + circleRadius * FPMath.Cos(angleRad);
			var y = circleCenter.Y + circleRadius * FPMath.Sin(angleRad);
			if (BotMovement.MoveToLocation(f, filter.Entity, new FPVector2(x, y), BotMovementType.Wander))
			{
				BotLogger.LogAction(f, ref filter, $"Wandering to ({x},{y})");
				filter.BotCharacter->SetNextDecisionDelay(filter.Entity, f, FP._1);
				return true;
			}

			return false;
		}
	}
}