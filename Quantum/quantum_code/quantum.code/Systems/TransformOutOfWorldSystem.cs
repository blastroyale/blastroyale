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
			public AlivePlayerCharacter* AlivePlayer;
			public Stats* Stats;
		}

		public override void Update(Frame f, ref TransformFilter filter)
		{
			if (filter.Transform->Position.Y < Constants.OUT_OF_WORLD_Y_THRESHOLD)
			{
				filter.Stats->ReduceHealth(f, filter.Entity, new Spell { Attacker = filter.Entity, PowerAmount = uint.MaxValue });
			}
		}
	}
}