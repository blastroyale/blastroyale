namespace Quantum
{
	[System.Serializable]
	public unsafe partial class AIFunctionNOT : AIFunction<bool>
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public AIParamBool Value;

		// ========== AIFunction INTERFACE ============================================================================

		public override bool Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			frame.Unsafe.TryGetPointer<AIBlackboardComponent>(entity, out var blackboardComponent);
			return !Value.Resolve(frame, entity, blackboardComponent, null, ref aiContext);
		}

		public override bool Execute(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
		{
			frame.TryGetPointer<AIBlackboardComponent>(entity, out var blackboardComponent);
			return !Value.Resolve(frame, entity, blackboardComponent, null, ref aiContext);
		}
	}
}
