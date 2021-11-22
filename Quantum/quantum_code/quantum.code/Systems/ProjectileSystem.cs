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
			// Current time should be higher than LaunchTime which is 0 by default
			if (f.Time < filter.Projectile->Data.LaunchTime)
			{
				return;
			}
			
			var distance = filter.Transform->Position - filter.Projectile->Data.SpawnPosition;
			var range = filter.Projectile->Data.Range;

			if (distance.SqrMagnitude > range * range)
			{
				if (filter.Projectile->Data.IsHitOnRangeLimit)
				{
					var hitData = new ProjectileHitData
					{
						TargetHit = EntityRef.None,
						Projectile = filter.Entity,
						// If the hit happens on range limit then we use the exact position of a range limit instead of a hit position
						HitPosition =  filter.Projectile->Data.SpawnPosition + filter.Projectile->Data.NormalizedDirection * range,
						// We consider this a static hit, because if it's not then the collision would be handled in OnTriggerEnter3D
						IsStaticHit = true
					};
					
					ProjectileHit(f, filter.Projectile, &hitData);
				}
				else
				{
					f.Events.OnProjectileFailedHitDestroy(filter.Entity);
					f.Add<EntityDestroyer>(filter.Entity);
				}
				return;
			}
			
			// Projectile with Target is a Homing projectile. We update the direction based on Target's position
			if (QuantumHelpers.IsAttackable(f, filter.Projectile->Data.Target))
			{
				var targetPosition = f.Get<Transform3D>(filter.Projectile->Data.Target).Position;
				targetPosition.Y += Constants.ACTOR_AS_TARGET_Y_OFFSET;
				
				var directionNormalized = (targetPosition - filter.Transform->Position).Normalized;
				
				filter.Projectile->Data.NormalizedDirection = directionNormalized;
				filter.Transform->Rotation = FPQuaternion.LookRotation(directionNormalized);
			}

			filter.Transform->Position += f.DeltaTime * filter.Projectile->Data.Speed * filter.Projectile->Data.NormalizedDirection;
		}
		
		/// <inheritdoc />
		public void OnTriggerEnter3D(Frame f, TriggerInfo3D info)
		{
			if (!f.Unsafe.TryGetPointer<Projectile>(info.Entity, out var projectile)
			    || projectile->Data.IsHitOnlyOnRangeLimit
			    // If it's not splash damage entity (speed!=0) and projectile already hit something then we don't process another hit
			    || (f.Has<EntityDestroyer>(info.Entity) && projectile->Data.Speed != FP._0))
			{
				return;
			}
			
			var hitData = new ProjectileHitData
			{
				TargetHit = info.Other,
				Projectile = info.Entity,
				HitPosition =  f.Get<Transform3D>(info.Entity).Position,
				IsStaticHit = info.IsStatic
			};
			
			if (info.IsStatic)
			{
				ProjectileHit(f, projectile, &hitData);
				
				return;
			}
			
			if (f.TryGet<Targetable>(info.Other, out var targetable) &&
			    ((targetable.Team != projectile->Data.TeamSource && !projectile->Data.IsHealing) ||
			     (targetable.Team == projectile->Data.TeamSource && projectile->Data.IsHealing) ||
			     (targetable.Team == (int) TeamType.Neutral && !projectile->Data.IsHealing)))
			{
				LocalPlayerHitEvents(f, projectile, &hitData);
				ProjectileHit(f, projectile, &hitData);
			}
		}

		private void LocalPlayerHitEvents(Frame f, Projectile* projectile, ProjectileHitData* hitData)
		{
			var attacker = projectile->Data.Attacker;
					
			if (f.TryGet<PlayerCharacter>(attacker, out var playerAttacker))
			{
				// Player's projectile hit someone
				f.Events.OnLocalPlayerProjectileHit(playerAttacker.Player, *hitData, projectile->Data);
			}
			else if (f.TryGet<PlayerCharacter>(hitData->TargetHit, out var playerHit))
			{
				// Someone's projectile hit a player
				f.Events.OnLocalPlayerHit(playerHit.Player, *hitData, projectile->Data);
			}
		}

		private void ProjectileHit(Frame f, Projectile* projectile, ProjectileHitData* hitData)
		{
			if (!projectile->Data.IsPiercing)
			{
				f.Add<EntityDestroyer>(hitData->Projectile);
			}
			
			f.Events.OnProjectileHit(*hitData, projectile->Data);
			
			// We create splash explosion only if we hit static
			// If we hit an actor then this actor "consumes" all the explosion while receiving a huge damage
			if (projectile->Data.SplashRadius > FP._0 && hitData->IsStaticHit)
			{
				var splashProjectileProxyData = new ProjectileData
				{
					Attacker = projectile->Data.Attacker,
					ProjectileAssetRef = projectile->Data.ProjectileAssetRef,
					ProjectileId = projectile->Data.ProjectileId,
					NormalizedDirection = FPVector3.Zero,
					SpawnPosition = hitData->HitPosition,
					TeamSource = projectile->Data.TeamSource,
					IsHealing = projectile->Data.IsHealing,
					PowerAmount = projectile->Data.PowerAmount,
					Speed = FP._0,
					Range = projectile->Data.Range,
					StunDuration = projectile->Data.StunDuration,
					SplashRadius = FP._0,
				};
			
				Projectile.CreateSplash(f, splashProjectileProxyData, projectile->Data.SplashRadius);
				
				// In case SplashProjectile carries a Hazard then we create a single hazard here
				// rather than in the HazardSystem
				if (projectile->Data.SpawnHazardId != 0)
				{
					Hazard.Create(f, projectile->Data.SpawnHazardId, hitData->HitPosition, projectile->Data.Attacker, projectile->Data.TeamSource);
				}
			}
			else
			{
				f.Signals.ProjectileTargetHit(hitData);
			}
		}
	}
}