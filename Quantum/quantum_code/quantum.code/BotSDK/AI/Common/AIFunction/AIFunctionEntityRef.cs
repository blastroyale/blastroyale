namespace Quantum
{
	[BotSDKHidden]
	[System.Serializable]
	public unsafe partial class DefaultAIFunctionEntityRef : AIFunction<EntityRef>
	{
		// ========== AIFunction INTERFACE ============================================================================

		public override EntityRef Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			return default(EntityRef);
		}
	}
}
