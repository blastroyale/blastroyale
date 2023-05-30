using Photon.Deterministic;

namespace Quantum.Systems.Bot
{
	public unsafe class BotSafeAreaBehaviour : BotBehaviour
	{
		public override BotBehaviourType[] DisallowedBehaviourTypes => new[] {BotBehaviourType.Static};


		public override bool OnEveryUpdate(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			return TryGoToSafeArea(f, ref filter);
		}


		private bool TryGoToSafeArea(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			var (circleCenter, circleRadius, circleIsShrinking) = GetCircle(f);

			// Not all game modes have a Shrinking Circle
			if (circleRadius < FP.SmallestNonZero)
			{
				return false;
			}

			var range = FP._0;

			// If circle is shrinking then we try to stay closer to the center
			if (circleIsShrinking)
			{
				range = circleRadius * FP._0_33;
			}
			else
			{
				range = circleRadius * (FP._0_75 + FP._0_10);
			}

			var newPosition = circleCenter;
			var x = f.RNG->Next(-range, range);
			var y = f.RNG->Next(-range, range);
			newPosition.X += x;
			newPosition.Y += y;

			// We try to go into random position OR into circle center (it's good for a very small circle)
			if (QuantumHelpers.SetClosestTarget(f, filter.Entity, newPosition.XOY)
				|| QuantumHelpers.SetClosestTarget(f, filter.Entity, circleCenter.XOY))
			{
				filter.BotCharacter->MoveTarget = filter.Entity;
				return true;
			}

			return false;
		}
	}
}