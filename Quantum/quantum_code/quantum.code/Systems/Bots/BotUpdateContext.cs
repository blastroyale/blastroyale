using Photon.Deterministic;

namespace Quantum.Systems.Bots
{
	public class BotUpdateGlobalContext
	{
		public FPVector2 circleCenter;
		public FP circleRadius;
		public bool circleIsShrinking;
		public FPVector2 circleTargetCenter ;
		public FP circleTargetRadius;
		public FP circleTimeToShrink;
	}
}