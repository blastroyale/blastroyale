using Photon.Deterministic;

namespace Quantum.Systems.Bots
{
	public class BotUpdateGlobalContext
	{
		public FPVector2 circleCenter = FPVector2.Zero;
		public FP circleRadius = FP._0;
		public bool circleIsShrinking = false;
		public FPVector2 circleTargetCenter = FPVector2.Zero;
		public FP circleTargetRadius = FP._0;
		public FP circleTimeToShrink = FP._0;
	}
}