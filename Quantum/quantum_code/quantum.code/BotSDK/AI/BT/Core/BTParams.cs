using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Quantum
{
	[StructLayout(LayoutKind.Auto)]
	public unsafe partial struct BTParams
	{
		// ========== PUBLIC MEMBERS ==================================================================================
		public Frame Frame { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => (Frame)_frame; }
		public FrameThreadSafe FrameThreadSafe { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _frame; }
		public BTAgent* Agent { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _agent; }
		public EntityRef Entity { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _entity; }
		public AIBlackboardComponent* Blackboard { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _blackboard; }

		public BTParamsUser UserParams { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _userParams; set => _userParams = value; }

		// ========== PRIVATE MEMBERS =================================================================================

		private FrameThreadSafe _frame;
		private BTAgent* _agent;
		private EntityRef _entity;
		private AIBlackboardComponent* _blackboard;

		private BTParamsUser _userParams;

		// ========== PUBLIC METHODS ==================================================================================

		public void SetDefaultParams(FrameThreadSafe frame, BTAgent* agent, EntityRef entity, AIBlackboardComponent* blackboard = null)
		{
			_frame = frame;
			_agent = agent;
			_entity = entity;
			_blackboard = blackboard;
		}

		public void Reset()
		{
			_frame = default;
			_agent = default;
			_entity = default;
			_blackboard = default;

			_userParams = default;
		}
	}

	// ==============================================================================================================

	public partial struct BTParamsUser
	{
	}
}