using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum.Systems
{	
	/// <summary>
	/// This system handles all the behaviour for the <see cref="RaycastShot"/> instantiation
	/// </summary>
	public unsafe class RaycastShotSystem : SystemMainThreadFilter<RaycastShotSystem.RaycastShotFilter>
	{ 
		public struct RaycastShotFilter
		{
			public EntityRef Entity;
			public RaycastShot* RaycastShot;
		}

		public override void Update(Frame f, ref RaycastShotFilter filter)
		{
			if ((f.Time - filter.RaycastShot->StartTime) > filter.RaycastShot->AttackHitTime)
			{
				f.Add<EntityDestroyer>(filter.Entity);
				return;
			}

			var weaponConfig = f.WeaponConfigs.GetConfig(filter.RaycastShot->WeaponConfigId);
			var position = filter.RaycastShot->SpawnPosition;
			var hitQuery = QueryOptions.HitDynamics | QueryOptions.HitKinematics | QueryOptions.HitStatics;
			var powerAmount = filter.RaycastShot->PowerAmount;
			var targetsHit = new List<EntityRef>();

			ShootRaycast(f, filter, filter.RaycastShot->Direction, position, weaponConfig, hitQuery, targetsHit, powerAmount);
		}

		private static void ShootRaycast(Frame f, RaycastShotFilter filter, FPVector2 direction,
		                                 FPVector3 position, QuantumWeaponConfig weaponConfig, QueryOptions hitQuery,
		                                 List<EntityRef> targetsHit, uint powerAmount)
		{
			var normalizedCurrentTime = filter.RaycastShot->AttackHitTime > 0? (f.Time - filter.RaycastShot->StartTime) / filter.RaycastShot->AttackHitTime : 1;
			var bulletEndPosition = position + direction.XOY *
			                          ((filter.RaycastShot->Range * normalizedCurrentTime));
			var bulletLength = (bulletEndPosition - filter.RaycastShot->LastBulletPosition).Magnitude;
			
			var hit = f.Physics3D.Raycast(filter.RaycastShot->LastBulletPosition, direction.XOY, bulletLength, f.TargetAllLayerMask, hitQuery);

			filter.RaycastShot->PreviousToLastBulletPosition = filter.RaycastShot->LastBulletPosition;
			filter.RaycastShot->LastBulletPosition = bulletEndPosition;
			
			if (!hit.HasValue || hit.Value.Entity == filter.RaycastShot->Attacker ||
			    (!weaponConfig.CanHitSameTarget && targetsHit.Contains(hit.Value.Entity)))
			{
				return;
			}

			targetsHit.Add(hit.Value.Entity);
			
			var spell = Spell.CreateInstant(f, hit.Value.Entity, filter.RaycastShot->Attacker, filter.RaycastShot->Attacker,
			                                powerAmount, hit.Value.Point, filter.RaycastShot->TeamSource);

			if (weaponConfig.SplashRadius > FP._0)
			{
				QuantumHelpers.ProcessAreaHit(f, weaponConfig.SplashRadius, spell);
			}
			else
			{
				QuantumHelpers.ProcessHit(f, spell);
			}
			
			f.Add<EntityDestroyer>(filter.Entity);
		}
	}
}