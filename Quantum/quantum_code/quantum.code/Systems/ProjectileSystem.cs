using System.Collections.Generic;
using System.IO;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="Projectile"/> collisions
	/// </summary>
	public unsafe class ProjectileSystem : SystemMainThreadFilter<ProjectileSystem.ProjectileFilter>, 
	                                       ISignalOnTriggerEnter3D
	{
		public struct ProjectileFilter
		{
			public EntityRef Entity;
			public Projectile* Projectile;
			public Transform3D* Transform;
		}

		/// <inheritdoc />
		public override void Update(Frame f, ref ProjectileFilter filter)
		{
			var distance = filter.Transform->Position - filter.Projectile->SpawnPosition;
			var range = filter.Projectile->Range;

			if (distance.SqrMagnitude > range * range)
			{
				f.Events.OnProjectileFailedHit(filter.Entity, *filter.Projectile);
				f.Add<EntityDestroyer>(filter.Entity);
				
				return;
			}
			
			// Projectile with Target is a Homing projectile. We update the direction based on Target's position
			if (QuantumHelpers.IsAttackable(f, filter.Projectile->Target, filter.Projectile->TeamSource))
			{
				var targetPosition = f.Get<Transform3D>(filter.Projectile->Target).Position;
				targetPosition.Y += Constants.ACTOR_AS_TARGET_Y_OFFSET;
				
				var directionNormalized = (targetPosition - filter.Transform->Position).Normalized;
				
				filter.Projectile->Direction = directionNormalized;
				filter.Transform->Rotation = FPQuaternion.LookRotation(directionNormalized);
			}

			filter.Transform->Position += f.DeltaTime * filter.Projectile->Speed * filter.Projectile->Direction;
		}
		
		/// <inheritdoc />
		public void OnTriggerEnter3D(Frame f, TriggerInfo3D info)
		{
			if (!f.TryGet<Projectile>(info.Entity, out var projectile) || info.StaticData.IsTrigger || info.Other == info.Entity)
			{
				return;
			}
			
			var position = f.Get<Transform3D>(info.Entity).Position;
			var sqrtRadius = projectile.SplashRadius * projectile.SplashRadius;

			if (!projectile.IsPiercing)
			{
				f.Add<EntityDestroyer>(info.Entity);
			}

			if (!info.IsStatic && QuantumHelpers.ProcessHit(f, projectile.Attacker, info.Other, position,
			                                               projectile.TeamSource, projectile.PowerAmount))
			{
				OnHit(f, info.Entity, info.Other, projectile, position);
			}
			
			if (projectile.SplashRadius == FP._0)
			{
				return;
			}
			
			var shape = Shape3D.CreateSphere(projectile.SplashRadius);
			var hits = f.Physics3D.ShapeCastAll(position, FPQuaternion.Identity, &shape, 
			                                    FPVector3.Zero, f.PlayerCastLayerMask, QueryOptions.HitDynamics);

			for (var j = 0; j < hits.Count; j++)
			{
				var hitPoint = hits[j].Point;
				var hitEntity = hits[j].Entity;
				var normalized = (hitPoint - position).SqrMagnitude / sqrtRadius;
				var amount = (uint) FPMath.RoundToInt(projectile.PowerAmount * normalized);

				if (hitEntity != info.Other && QuantumHelpers.ProcessHit(f, projectile.Attacker, hitEntity, 
				                                                         hitPoint, projectile.TeamSource, amount))
				{
					OnHit(f, info.Entity, hitEntity, projectile, hitPoint);
				}
			}
		}

		private void OnHit(Frame f, EntityRef attacker, EntityRef hitEntity, Projectile projectile, FPVector3 hitPoint)
		{
			if (projectile.StunDuration > FP._0)
			{
				StatusModifiers.AddStatusModifierToEntity(f, hitEntity, StatusModifierType.Stun, projectile.StunDuration);
			}
			
			f.Events.OnProjectileHit(attacker, hitEntity, projectile, hitPoint);
		}
	}
}