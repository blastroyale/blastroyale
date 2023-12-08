namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the changes in a <see cref="Destructible"/> entity
	/// </summary>
	public unsafe class DestructibleSystem : SystemMainThreadFilter<DestructibleSystem.DestructibleFilter>, 
	                                         ISignalHealthIsZeroFromAttacker,
											 ISignalOnComponentAdded<Destructible>
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
			var spell = Spell.CreateInstant(f, filter.Entity, filter.Destructible->Destroyer, filter.Destructible->Destroyer, power, 0,
			                                filter.Transform->Position);
			
			var hitCount = QuantumHelpers.ProcessAreaHit(f, filter.Destructible->SplashRadius, &spell);
			f.Events.OnHazardLand(filter.Destructible->GameId, filter.Transform->Position, filter.Destructible->Destroyer, hitCount);
			f.Add<EntityDestroyer>(filter.Entity);
		}
		
		/// <inheritdoc />
		public void HealthIsZeroFromAttacker(Frame f, EntityRef entity, EntityRef attacker, QBoolean fromRoofDamage)
		{
			if (!f.Unsafe.TryGetPointer<Destructible>(entity, out var destructible) || destructible->IsDestructing)
			{
				return;
			}
			
			destructible->TimeToDestroy = f.Time + f.RNG->NextInclusive(destructible->DestructionLengthTime[0], destructible->DestructionLengthTime[1]);
			destructible->IsDestructing = true;
			destructible->Destroyer = attacker;
			f.Events.OnDestructibleScheduled(entity, *destructible);
		}

		public void OnAdded(Frame f, EntityRef entity, Destructible* component)
		{
			component->Init(f, entity);
		}
	}
}