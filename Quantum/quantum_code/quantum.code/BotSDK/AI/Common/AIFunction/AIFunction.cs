namespace Quantum
{
	public unsafe class AIFunction<T> : AIFunction
	{
		public virtual T Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			return default;
		}

		public virtual T Execute(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
		{
			return Execute((Frame)frame, entity, ref aiContext);
		}
	}
}
