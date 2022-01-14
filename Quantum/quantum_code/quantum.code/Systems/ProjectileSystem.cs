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
			if (!f.TryGet<Projectile>(info.Entity, out var projectile) || info.Other == info.Entity || 
			    projectile.Attacker == info.Entity || projectile.Attacker == info.Other)
			{
				return;
			}
			
			var position = f.Get<Transform3D>(info.Entity).Position;
			
			f.Add<EntityDestroyer>(info.Entity);
			
			if (QuantumHelpers.ProcessHit(f, projectile.Attacker, info.Other, position,
			                                               projectile.TeamSource, projectile.PowerAmount))
			{
				OnHit(f,  projectile.Attacker, info.Entity, info.Other, position);
				return;
			}
			
			if (projectile.SplashRadius == FP._0 || !info.IsStatic)
			{
				return;
			}
			
			QuantumHelpers.ProcessAreaHit(f, projectile.Attacker, info.Entity, projectile.SplashRadius, position,
			                              projectile.PowerAmount, projectile.TeamSource, OnHit);
		}

		private void OnHit(Frame f, EntityRef attacker, EntityRef attackSource, EntityRef hitEntity, FPVector3 hitPoint)
		{
			var source = f.Get<Projectile>(attackSource);
			
			if (source.StunDuration > FP._0)
			{
				StatusModifiers.AddStatusModifierToEntity(f, hitEntity, StatusModifierType.Stun, source.StunDuration);
			}
			
			f.Events.OnProjectileHit(attacker, hitEntity, source, hitPoint);
		}
	}
}