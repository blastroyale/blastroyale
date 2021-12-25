namespace Quantum
{
	[System.Serializable]
	public unsafe partial class AIFunctionNOT : AIFunction<bool>
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public AIParamBool Value;

		// ========== AIFunction INTERFACE ============================================================================

		public override bool Execute(Frame frame, EntityRef entity)
		{
			frame.Unsafe.TryGetPointer<AIBlackboardComponent>(entity, out var blackboardComponent);
			return !Value.Resolve(frame, entity, blackboardComponent, null);
		}

		public override bool Execute(FrameThreadSafe frame, EntityRef entity)
		{
			frame.TryGetPointer<AIBlackboardComponent>(entity, out var blackboardComponent);
			return !Value.Resolve(frame, entity, blackboardComponent, null);
		}
	}
}
