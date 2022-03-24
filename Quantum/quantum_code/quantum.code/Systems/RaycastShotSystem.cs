using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum.Systems
{	
	/// <summary>
	/// This system handles all the behaviour for the <see cref="RaycastShot"/> instantiation
	/// </summary>
	public unsafe class RaycastShotSystem : SystemMainThreadFilter<RaycastShotSystem.RaycastShotFilter>
	{
		private static readonly FP _bulletLength = FP._0_20;
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

			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(filter.RaycastShot->Attacker);
			var weaponConfig = f.WeaponConfigs.GetConfig(playerCharacter->CurrentWeapon.GameId);
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
			var normalizedCurrentTime = (f.Time - filter.RaycastShot->StartTime) / filter.RaycastShot->AttackHitTime;
			var bulletStartPosition = position + direction.XOY.Normalized *
			                          ((filter.RaycastShot->Range * normalizedCurrentTime) - _bulletLength / 2);
			
			var hit = f.Physics3D.Raycast(bulletStartPosition, direction.XOY, _bulletLength, f.TargetAllLayerMask, hitQuery);

			if (!hit.HasValue || hit.Value.Entity == filter.RaycastShot->Attacker ||
			    (!weaponConfig.CanHitSameTarget && targetsHit.Contains(hit.Value.Entity)))
			{
				return;
			}

			targetsHit.Add(hit.Value.Entity);

			var spell = Spell.CreateInstant(f, hit.Value.Entity, filter.RaycastShot->Attacker, filter.RaycastShot->Attacker,
			                                powerAmount, hit.Value.Point);

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