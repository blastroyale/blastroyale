using Photon.Deterministic;

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
				if (hazard->GameId == GameId.AggroBeaconHazard)
				{
					f.Add<EntityDestroyer>(hazard->Attacker);
				}
				
				f.Add<EntityDestroyer>(filter.Entity);
			}
		}
		
		/// <inheritdoc />
		public void OnTrigger3D(Frame f, TriggerInfo3D info)
		{
			if (info.IsStatic || !f.TryGet<Hazard>(info.Entity, out var hazard) || !hazard.IsActive)
			{
				return;
			}
			
			if (f.TryGet<Targetable>(info.Other, out var targetable) &&
			    ((targetable.Team != hazard.TeamSource && !hazard.IsHealing) ||
			     (targetable.Team == hazard.TeamSource && hazard.IsHealing)))
			{
				var hazardHitData = new HazardHitData
				{
					TargetHit = info.Other,
					Hazard = info.Entity,
					HitPosition = f.Get<Transform3D>(info.Other).Position
				};
				
				var hitData = new ProjectileHitData
				{
					TargetHit = info.Other,
					Projectile = info.Entity,
					HitPosition = hazardHitData.HitPosition
				};
				
				var projectileProxyData = new ProjectileData
				{
					Attacker = hazard.Attacker,
					ProjectileAssetRef = 0,
					NormalizedDirection = FPVector3.Zero,
					SpawnPosition = hazardHitData.HitPosition,
					TeamSource = hazard.TeamSource,
					IsHealing = hazard.IsHealing,
					PowerAmount = hazard.PowerAmount,
					Speed = FP._0,
					Range = Constants.PROJECTILE_MAX_RANGE,
					SplashRadius = FP._0,
				};
				
				LocalPlayerHitEvents(f, &projectileProxyData, &hitData);
				
				f.Signals.HazardTargetHit(&hazardHitData);
			}
		}
	}
}