using Photon.Deterministic;

namespace Quantum
{
	public partial struct ShrinkingCircle
	{
		/// <summary>
		/// Calculates the current <paramref name="cener"/> and <paramref name="radius"/> of the circle
		/// </summary>
		public void GetMovingCircle(Frame f, out FPVector2 center, out FP radius)
		{
			if (ShrinkingDurationTime == 0)
			{
				center = CurrentCircleCenter;
				radius = CurrentRadius;
				return;
			}
			
			var lerp = FPMath.Max(0, (f.Time - ShrinkingStartTime) / ShrinkingDurationTime);

			radius = FPMath.Lerp(CurrentRadius, TargetRadius, lerp);
			center = FPVector2.Lerp(CurrentCircleCenter, TargetCircleCenter, lerp);
		}
	}
}