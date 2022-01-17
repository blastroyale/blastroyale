namespace Quantum
{
	[BotSDKHidden]
	[System.Serializable]
	public unsafe partial class DefaultAIFunctionByte : AIFunction<byte>
	{
		// ========== AIFunction INTERFACE ============================================================================

		public override byte Execute(Frame frame, EntityRef entity)
		{
			return 0;
		}
	}
}
