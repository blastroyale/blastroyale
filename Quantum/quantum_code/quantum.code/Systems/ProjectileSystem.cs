using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="Projectile"/> collisions
	/// </summary>
	public unsafe class ProjectileSystem : SystemMainThreadFilter<ProjectileSystem.ProjectileFilter>, ISignalOnTriggerEnter3D
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
			if ((filter.Transform->Position - filter.Projectile->SpawnPosition).SqrMagnitude > filter.Projectile->RangeSquared)
			{
				f.Destroy(filter.Entity);
				return;
			}

			if (filter.Projectile->DespawnTime != FP._0 && f.Time > filter.Projectile->DespawnTime)
			{
				f.Destroy(filter.Entity);
				return;
			}
			
			filter.Transform->Position += filter.Projectile->Direction;
		}
		
		public void OnTriggerEnter3D(Frame f, TriggerInfo3D info)
		{
			if (!f.TryGet<Projectile>(info.Entity, out var projectile) || info.Other == info.Entity || info.StaticData.IsTrigger ||projectile.Attacker == info.Entity 
				|| projectile.Attacker == info.Other || f.Has<EntityDestroyer>(info.Entity))
			{
				return;
			}
			OnProjectileHit(f, info.Other, info.Entity, projectile);
		}

		/// <summary>
		/// Can be used for sub projectiles (e.g area explosions, fire fields, ricochets etc)
		/// This creates a sub-projectile based on the parent projectile just changing its entity prototype and
		/// a couple specific veriables specified per projectile hit type
		/// </summary>
		private void CreateSubProjectile(Frame f, Projectile p, FPVector3 hitPosition)
		{
			var cfg = f.WeaponConfigs.GetConfig(p.SourceId);
			var subProjectile = p;
			if (cfg.HitType == ProjectileHitType.AreaOfEffect)
			{
				subProjectile.Speed = 0;
				subProjectile.Direction = FPVector3.Zero;
				subProjectile.SpawnPosition = hitPosition;
				subProjectile.DespawnTime = f.Time + FP._0_50;
				subProjectile.DamagePct = (byte)cfg.SplashDamageRatio.AsInt;
			}
			
			subProjectile.Iteration = (byte)(p.Iteration + 1);
			var entity = f.Create(f.FindAsset<EntityPrototype>(cfg.BulletHitPrototype.Id));
			var transform = f.Unsafe.GetPointer<Transform3D>(entity);
			transform->Position = hitPosition;
			f.Add(entity, subProjectile);
		}

		private void OnProjectileHit(Frame f, EntityRef targetHit, EntityRef projectileEntity, Projectile projectile)
		{
			var power = (uint)projectile.GetPower(f);
			var position = f.Get<Transform3D>(projectileEntity).Position;
			var spell = Spell.CreateInstant(f, targetHit, projectile.Attacker, projectileEntity, power,
			                                projectile.KnockbackAmount, position, projectile.TeamSource);
			
			if(targetHit == projectileEntity)
				f.Events.OnProjectileFailedHit(projectile, position);
			else
				f.Events.OnProjectileSuccessHit(projectile, targetHit, position);
			
			if (projectile.ShouldPerformSubProjectile(f))
			{
				CreateSubProjectile(f, projectile, position);
			}
			else if (QuantumHelpers.ProcessHit(f, &spell))
			{
				OnHit(f, &spell);
			}

			if (projectile.Speed > 0)
			{
				f.Destroy(projectileEntity);
			}
		}

		private void OnHit(Frame f, Spell* spell)
		{
			var source = f.Get<Projectile>(spell->SpellSource);
			
			if (source.StunDuration > FP._0)
			{
				StatusModifiers.AddStatusModifierToEntity(f, spell->Victim, StatusModifierType.Stun, source.StunDuration);
			}
			
			f.Events.OnProjectileTargetableHit(spell->SpellSource, spell->Victim, spell->OriginalHitPosition);
		}
	}
}