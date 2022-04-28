namespace Quantum
{
	[BotSDKHidden]
	[System.Serializable]
	public unsafe partial class DefaultAIFunctionString : AIFunction<string>
	{
		// ========== AIFunction INTERFACE ============================================================================

		public override string Execute(Frame frame, EntityRef entity)
		{
			return null;
		}
	}
}
