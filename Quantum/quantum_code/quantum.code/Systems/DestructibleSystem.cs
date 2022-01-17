using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the changes in a <see cref="Destructible"/> entity
	/// </summary>
	public unsafe class DestructibleSystem : SystemMainThreadFilter<DestructibleSystem.DestructibleFilter>,
	                                         ISignalHealthIsZero
	{
		public struct DestructibleFilter
		{
			public EntityRef Entity;
			public Destructible* Destructible;
			public Transform3D* Transform;
			public Targetable* Targetable;
			public Stats* Stats;
		}
		
		/// <inheritdoc />
		public override void Update(Frame f, ref DestructibleFilter filter)
		{
			if (!filter.Destructible->IsDestructing || f.Time < filter.Destructible->TimeToDestroy)
			{
				return;
			}
			
			var shape = Shape3D.CreateSphere(filter.Destructible->SplashRadius);
			var power = (uint) filter.Stats->GetStatData(StatType.Power).StatValue.AsInt;
			var hits = f.Physics3D.ShapeCastAll(filter.Transform->Position, FPQuaternion.Identity, shape, 
			                                    FPVector3.Zero, f.TargetAllLayerMask, QueryOptions.HitDynamics);

			for (var j = 0; j < hits.Count; j++)
			{
				if (hits[j].Entity == filter.Entity)
				{
					continue;
				}
				
				QuantumHelpers.ProcessHit(f, filter.Entity, hits[j].Entity, hits[j].Point,
				                          filter.Targetable->Team, power);
			}
			
			f.Add<EntityDestroyer>(filter.Entity);
		}
		
		/// <inheritdoc />
		public void HealthIsZero(Frame f, EntityRef entity, EntityRef attacker)
		{
			if (!f.Unsafe.TryGetPointer<Destructible>(entity, out var destructible) || destructible->IsDestructing)
			{
				return;
			}
			
			destructible->TimeToDestroy = f.Time + destructible->DestructionLengthTime;
			destructible->IsDestructing = true;
			
			f.Events.OnDestructibleScheduled(entity, *destructible);
		}
	}
}