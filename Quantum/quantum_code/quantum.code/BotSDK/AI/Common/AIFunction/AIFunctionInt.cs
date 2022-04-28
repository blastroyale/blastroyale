namespace Quantum
{
	[BotSDKHidden]
	[System.Serializable]
	public unsafe partial class DefaultAIFunctionInt : AIFunction<int>
	{
		// ========== AIFunction INTERFACE ============================================================================

		public override int Execute(Frame frame, EntityRef entity)
		{
			return 0;
		}
	}
}
