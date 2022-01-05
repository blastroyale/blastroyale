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
			if (QuantumHelpers.IsAttackable(f, filter.Projectile->Target))
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
			if (f.Has<EntityDestroyer>(info.Entity) || !f.TryGet<Projectile>(info.Entity, out var projectile) ||
			    !IsValidCollision(f, projectile, info))
			{
				return;
			}
			
			var position = f.Get<Transform3D>(info.Entity).Position;

			if (!projectile.IsPiercing)
			{
				f.Add<EntityDestroyer>(info.Entity);
			}
			
			if (projectile.SplashRadius == FP._0)
			{
				QuantumHelpers.ProcessHit(f, projectile.Attacker, info.Other, position,
				                          projectile.TeamSource, projectile.PowerAmount);

				f.Events.OnProjectileHit(info.Entity, info.Other, projectile, position);

				return;
			}
			
			var sqrtRadius = projectile.SplashRadius * projectile.SplashRadius;
			var shape = Shape3D.CreateSphere(projectile.SplashRadius);
			var hits = f.Physics3D.ShapeCastAll(position, FPQuaternion.Identity, &shape, 
			                                    FPVector3.Zero, f.PlayerCastLayerMask, QueryOptions.HitDynamics);

			for (var j = 0; j < hits.Count; j++)
			{
				var hitPoint = hits[j].Point;
				var hitEntity = hits[j].Entity;
				var normalized = (hitPoint - position).SqrMagnitude / sqrtRadius;
					
				QuantumHelpers.ProcessHit(f, projectile.Attacker, hitEntity, hitPoint, projectile.TeamSource,
				                          (uint) FPMath.RoundToInt(projectile.PowerAmount * normalized));
					
				f.Events.OnProjectileHit(info.Entity, hitEntity, projectile, hitPoint);
			}
			
			
		}

		private bool IsValidCollision(Frame f, Projectile projectile, TriggerInfo3D info)
		{
			var neutral = (int)TeamType.Neutral;
			
			return info.IsStatic || f.TryGet<Targetable>(info.Other, out var targetable) && 
			       (targetable.Team != projectile.TeamSource || targetable.Team == neutral || projectile.TeamSource == neutral); 
		}
	}
}