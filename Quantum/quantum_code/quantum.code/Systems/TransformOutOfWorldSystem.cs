using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles Transform3D objects that fell out of world
	/// </summary>
	public unsafe class TransformOutOfWorldSystem : SystemMainThreadFilter<TransformOutOfWorldSystem.TransformFilter>
	{
		public struct TransformFilter
		{
			public EntityRef Entity;
			public Transform3D* Transform;
		}

		public override void Update(Frame f, ref TransformFilter filter)
		{
			if (filter.Transform->Position.Y < Constants.OUT_OF_WORLD_Y_THRESHOLD && f.Has<AlivePlayerCharacter>(filter.Entity))
			{
				var stats = f.Get<Stats>(filter.Entity);
				var currentHealth = stats.CurrentHealth;
				
				f.Signals.HealthIsZero(filter.Entity, filter.Entity);
				f.Events.OnHealthIsZero(filter.Entity, filter.Entity, currentHealth, stats.Values[(int)StatType.Health].StatValue.AsInt, false);
			}
		}
	}
}