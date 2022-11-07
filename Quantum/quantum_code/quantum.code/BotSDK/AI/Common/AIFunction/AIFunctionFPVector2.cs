using Photon.Deterministic;

namespace Quantum
{
	[BotSDKHidden]
	[System.Serializable]
	public unsafe partial class DefaultAIFunctionFPVector2 : AIFunction<FPVector2>
	{
		// ========== AIFunction INTERFACE ============================================================================

		public override FPVector2 Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			return FPVector2.Zero;
		}
	}
}
