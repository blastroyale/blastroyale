using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// Handles Hazards
	/// </summary>
	public unsafe class HazardSystem : SystemMainThreadFilter<HazardSystem.HazardFilter>
	{
		public struct HazardFilter
		{
			public EntityRef Entity;
			public Hazard* Hazard;
			public Transform3D* Transform;
		}
		
		/// <inheritdoc />
		public override void Update(Frame f, ref HazardFilter filter)
		{
			var hazard = filter.Hazard;
			
			if (f.Time > hazard->EndTime)
			{
				f.Add<EntityDestroyer>(filter.Entity);
			}
			
			if (f.Time < hazard->NextTickTime)
			{
				return;
			}
			
			hazard->NextTickTime += hazard->NextTickTime == FP._0 ? f.Time + hazard->Interval : hazard->Interval;
			
			QuantumHelpers.ProcessAreaHit(f, hazard->Attacker, filter.Entity, hazard->Radius,
			                              filter.Transform->Position, hazard->PowerAmount, hazard->TeamSource, OnHit);
		}

		private void OnHit(Frame f, EntityRef attacker, EntityRef attackSource, EntityRef hitEntity, FPVector3 hitPoint)
		{
			var source = f.Get<Hazard>(attackSource);
			
			if (source.StunDuration > FP._0)
			{
				StatusModifiers.AddStatusModifierToEntity(f, hitEntity, StatusModifierType.Stun, source.StunDuration);
			}
			
			f.Events.OnHazardHit(attacker, hitEntity, source, hitPoint);
		}
	}
}