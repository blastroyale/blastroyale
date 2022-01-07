namespace Quantum.Systems
{
	/// <summary>
	/// Handles Hazards
	/// </summary>
	public unsafe class HazardSystem : SystemMainThreadFilter<HazardSystem.HazardFilter>, ISignalOnTrigger3D
	{
		public struct HazardFilter
		{
			public EntityRef Entity;
			public Hazard* Hazard;
		}
		
		/// <inheritdoc />
		public override void Update(Frame f, ref HazardFilter filter)
		{
			var hazard = filter.Hazard;
			
			if (f.Time >= hazard->NextApplyTime)
			{
				hazard->NextApplyTime += hazard->Interval;
				hazard->IsActive = true;
			}
			else if (hazard->IsActive)
			{
				hazard->IsActive = false;
			}
			
			if (f.Time >= hazard->DestroyTime)
			{
				f.Add<EntityDestroyer>(filter.Entity);
			}
		}
		
		/// <inheritdoc />
		public void OnTrigger3D(Frame f, TriggerInfo3D info)
		{
			if (!f.TryGet<Hazard>(info.Entity, out var hazard) || !hazard.IsActive || info.IsStatic || info.Entity == info.Other)
			{
				return;
			}
			
			var position = f.Get<Transform3D>(info.Entity).Position;

			if (QuantumHelpers.ProcessHit(f, hazard.Attacker, info.Other, position, hazard.TeamSource, hazard.PowerAmount))
			{
				f.Events.OnHazardHit(info.Entity, info.Other, hazard, position);
			}
		}
	}
}