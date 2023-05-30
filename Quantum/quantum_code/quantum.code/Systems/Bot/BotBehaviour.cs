using Photon.Deterministic;

namespace Quantum.Systems.Bot
{
	public abstract unsafe class BotBehaviour
	{
		public virtual BotBehaviourType[] DisallowedBehaviourTypes { get; }

		/// <summary>
		/// Returns true to early return update
		/// </summary>
		/// <param name="f"></param>
		/// <param name="filter"></param>
		/// <returns></returns>
		public virtual bool OnEveryUpdate(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			return false;
		}

		public virtual bool OnDecisionUpdate(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			return false;
		}


		protected bool IsDecisionTime(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			return filter.BotCharacter->MoveTarget == EntityRef.None
				|| f.Time > filter.BotCharacter->NextDecisionTime;
		}

		protected bool IsInVisionRange(FP distanceSqr, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			var visionRangeSqr = filter.BotCharacter->VisionRangeSqr;

			return visionRangeSqr < FP._0 || distanceSqr <= visionRangeSqr;
		}

		protected (FPVector2 circleCenter, FP circleRadius, bool shrinking) GetCircle(Frame f)
		{
			var circleCenter = FPVector2.Zero;
			var circleRadius = FP._0;
			var circleIsShrinking = false;
			if (f.TryGetSingleton<ShrinkingCircle>(out var circle))
			{
				circle.GetMovingCircle(f, out circleCenter, out circleRadius);
				circleIsShrinking = circle.ShrinkingStartTime <= f.Time;
			}

			return (circleCenter, circleRadius, circleIsShrinking);
		}


		protected bool IsInCircle(Frame f, ref BotCharacterSystem.BotCharacterFilter filter, FPVector3 positionToCheck)
		{
			var (circleCenter, circleRadius, circleIsShrinking) = GetCircle(f);


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
	}
}