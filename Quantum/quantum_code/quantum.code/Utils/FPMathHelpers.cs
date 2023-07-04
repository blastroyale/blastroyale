using Photon.Deterministic;

namespace Quantum
{
	public static class FPMathHelpers
	{
		public static bool IsPositionInsideCircle(FPVector2 circleCenter, FP circleRadius, FPVector2 position)
		{
			var distanceSquared = (position - circleCenter).SqrMagnitude;
			return distanceSquared <= circleRadius * circleRadius;
		}
	}
}