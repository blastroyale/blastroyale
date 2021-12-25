namespace Quantum
{
	public unsafe abstract class AIFunction<T> : AIFunction
	{
		public virtual T Execute(Frame frame, EntityRef entity)
		{
			return default;
		}

		public virtual T Execute(FrameThreadSafe frame, EntityRef entity)
		{
			return Execute((Frame)frame, entity);
		}
	}
}
