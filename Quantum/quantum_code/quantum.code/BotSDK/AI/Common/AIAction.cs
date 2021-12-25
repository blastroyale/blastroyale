using System;
using Photon.Deterministic;

namespace Quantum
{
	public abstract unsafe partial class AIAction
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public string Label;
		public const int NEXT_ACTION_DEFAULT = -1;

		// ========== AIAction INTERFACE ================================================================================

		public virtual void Update(Frame frame, EntityRef entity)
		{
		}

		public virtual void Update(FrameThreadSafe frame, EntityRef entity)
		{
			Update((Frame)frame, entity);
		}

		public virtual int NextAction(Frame frame, EntityRef entity) { return NEXT_ACTION_DEFAULT; }
		public virtual int NextActionThreadSafe(FrameThreadSafe frame, EntityRef entity) { return NextAction((Frame)frame, entity); }
	}
}
