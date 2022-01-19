namespace Quantum
{
	[BotSDKHidden]
	[System.Serializable]
	public unsafe partial class DefaultAIFunctionBool : AIFunction<bool>
	{
		// ========== AIFunction INTERFACE ============================================================================

		public override bool Execute(Frame frame, EntityRef entity)
		{
			return false;
		}
	}
}
