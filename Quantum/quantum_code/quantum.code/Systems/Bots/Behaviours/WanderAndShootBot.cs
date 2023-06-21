using System;
using Photon.Deterministic;
using static Quantum.Systems.Bots.BotCharacterSystem;

namespace Quantum.Systems.Bots
{
	public unsafe class WanderAndShootBot
	{
		internal void Update(Frame f, ref BotCharacterFilter filter, BotUpdateGlobalContext botCtx)
		{


			// Do not do any decision making if the time has not come, unless a bot have no target to move to
			if (filter.BotCharacter->MoveTarget != EntityRef.None
				&& f.Time < filter.BotCharacter->NextDecisionTime)
			{
				BotLogger.LogAction(ref filter, "waiting for decision " + (filter.BotCharacter->NextDecisionTime - f.Time));

				return;
			}
			
			const int RETRIES = 10;
			var rad360 = FP.Rad_180 * 2;

			var angle = f.RNG->NextInclusive(FP._0, rad360);
			// It will try to go from 0 to 338, it doesn't make sence to go back to 360
			var variationPerRetry = (rad360 - FP.Rad_22_50) / RETRIES;
			for (var i = 0; i < RETRIES; i++)
			{
				if (TryToWanderToAngle(f, ref filter, filter.Transform->Position.XZ, FP._10, angle))
				{
					filter.BotCharacter->MoveTarget = filter.Entity;
					filter.BotCharacter->NextDecisionTime = f.Time + FP._5;
					break;
				}

				angle += variationPerRetry;
			}
		}


		private bool TryToWanderToAngle(Frame f, ref BotCharacterFilter filter, FPVector2 circleCenter, FP circleRadius, FP angleRad)
		{
			var x = circleCenter.X + circleRadius * FPMath.Cos(angleRad);
			var y = circleCenter.Y + circleRadius * FPMath.Sin(angleRad);
			BotLogger.LogAction(ref filter,$"Going to ({x},{y})");
			return QuantumHelpers.SetClosestTarget(f, filter.Entity, new FPVector3(x, FP._0, y), FP._2);
		}
	}
}