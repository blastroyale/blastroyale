using Photon.Deterministic;

namespace Quantum
{
	public partial struct EntityDestroyer
	{
		/// <summary>
		/// Adds am <see cref="EntityDestroyer"/> to the entity, with a destroy-at time.
		/// </summary>
		public static void Create(Frame f, EntityRef entity, FP destroyTime)
		{
			f.Add(entity, new EntityDestroyer
			{
				time = destroyTime
			});
		}

		/// <summary>
		/// Adds am <see cref="EntityDestroyer"/> to the entity, which is evaluated immediately (no delay).
		/// </summary>
		public static void Create(Frame f, EntityRef entity)
		{
			f.Add(entity, new EntityDestroyer
			{
				time = FP._0
			});
		}
	}
}