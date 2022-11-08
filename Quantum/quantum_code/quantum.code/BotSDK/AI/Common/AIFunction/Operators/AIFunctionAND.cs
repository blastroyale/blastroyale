namespace Quantum
{

	[System.Serializable]
	public unsafe partial class AIFunctionAND : AIFunction<bool>
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public AIParamBool ValueA;
		public AIParamBool ValueB;

		// ========== AIFunction INTERFACE ============================================================================

		public override bool Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			frame.Unsafe.TryGetPointer<AIBlackboardComponent>(entity, out var blackboardComponent);
			return ValueA.Resolve(frame, entity, blackboardComponent, null, ref aiContext) 
				&& ValueB.Resolve(frame, entity, blackboardComponent, null, ref aiContext);
		}

		public override bool Execute(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
		{
			frame.TryGetPointer<AIBlackboardComponent>(entity, out var blackboardComponent);
			return ValueA.Resolve(frame, entity, blackboardComponent, null, ref aiContext) 
				&& ValueB.Resolve(frame, entity, blackboardComponent, null, ref aiContext);
		}
	}
}