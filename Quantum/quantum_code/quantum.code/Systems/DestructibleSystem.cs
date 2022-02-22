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
			
			var power = (uint) filter.Stats->GetStatData(StatType.Power).StatValue.AsInt;
			var spell = Spell.CreateInstant(f, filter.Entity, filter.Entity, filter.Entity, power,
			                                filter.Transform->Position);

			QuantumHelpers.ProcessAreaHit(f, filter.Destructible->SplashRadius, spell);
			
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