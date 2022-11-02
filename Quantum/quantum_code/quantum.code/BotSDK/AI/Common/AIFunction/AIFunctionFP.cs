using Photon.Deterministic;

namespace Quantum
{
	[BotSDKHidden]
	[System.Serializable]
	public unsafe partial class DefaultAIFunctionFP : AIFunction<FP>
	{
		// ========== AIFunction INTERFACE ============================================================================

		public override FP Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			return FP._0;
		}
	}
}
