using Photon.Deterministic;
using System;

namespace Quantum
{
	public abstract unsafe partial class HFSMDecision
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public string Label;

		// ========== HFSMDecision INTERFACE ==========================================================================

		public virtual Boolean Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			return false;
		}

		public virtual Boolean DecideThreadSafe(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
		{
			return Decide((Frame)frame, entity, ref aiContext);
		}
	}
}
