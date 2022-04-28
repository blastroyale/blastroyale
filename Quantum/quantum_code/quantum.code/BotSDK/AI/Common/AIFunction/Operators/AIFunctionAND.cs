namespace Quantum
{

	[System.Serializable]
	public unsafe partial class AIFunctionAND : AIFunction<bool>
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public AIParamBool ValueA;
		public AIParamBool ValueB;

		// ========== AIFunction INTERFACE ============================================================================

		public override bool Execute(Frame frame, EntityRef entity)
		{
			frame.Unsafe.TryGetPointer<AIBlackboardComponent>(entity, out var blackboardComponent);
			return ValueA.Resolve(frame, entity, blackboardComponent, null) && ValueB.Resolve(frame, entity, blackboardComponent, null);
		}

		public override bool Execute(FrameThreadSafe frame, EntityRef entity)
		{
			frame.TryGetPointer<AIBlackboardComponent>(entity, out var blackboardComponent);
			return ValueA.Resolve(frame, entity, blackboardComponent, null) && ValueB.Resolve(frame, entity, blackboardComponent, null);
		}
	}
}