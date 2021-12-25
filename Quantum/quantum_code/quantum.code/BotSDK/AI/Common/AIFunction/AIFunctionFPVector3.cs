using Photon.Deterministic;

namespace Quantum
{
	[BotSDKHidden]
	[System.Serializable]
	public unsafe partial class DefaultAIFunctionFPVector3 : AIFunction<FPVector3>
	{
		// ========== AIFunction INTERFACE ============================================================================

		public override FPVector3 Execute(Frame frame, EntityRef entity)
		{
			return FPVector3.Zero;
		}
	}
}
