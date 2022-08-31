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
				EndProjectile(f,filter.Entity, filter.Entity, *filter.Projectile);
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
			var targetHit = info.Other;
			var hitSource = info.Entity;

			if (!f.TryGet<Projectile>(hitSource, out var projectile) || targetHit == hitSource || info.StaticData.IsTrigger ||projectile.Attacker == hitSource 
				|| projectile.Attacker == targetHit || f.Has<EntityDestroyer>(hitSource))
			{
				return;
			}
			EndProjectile(f, targetHit, hitSource, projectile);
		}
		
		private void EndProjectile(Frame f, EntityRef targetHit, EntityRef hitSource, Projectile projectile)
		{
			var position = f.Get<Transform3D>(hitSource).Position;
			var spell = Spell.CreateInstant(f, targetHit, projectile.Attacker, hitSource, projectile.PowerAmount,
			                                projectile.KnockbackAmount, position, projectile.TeamSource);

			f.Add<EntityDestroyer>(hitSource);
			if(targetHit == hitSource)
				f.Events.OnProjectileFailedHit(hitSource, projectile, position);
			else
				f.Events.OnProjectileSuccessHit(hitSource, targetHit, projectile, position);

			if (projectile.SplashRadius > FP._0)
			{
				var splashSpell = Spell.CreateInstant(f, targetHit, projectile.Attacker, hitSource, 
					(uint)(projectile.PowerAmount * projectile.SplashDamageRatio), 
					(uint)(projectile.KnockbackAmount * projectile.SplashDamageRatio), position,
					projectile.TeamSource);

				QuantumHelpers.ProcessAreaHit(f, projectile.SplashRadius, splashSpell, uint.MaxValue, OnHit);
				f.Events.OnProjectileExplosion(projectile.SourceId, position);
			}

			if (QuantumHelpers.ProcessHit(f, spell))
			{
				OnHit(f, spell);
			}
				
		}

		private void OnHit(Frame f, Spell spell)
		{
			var source = f.Get<Projectile>(spell.SpellSource);
			
			if (source.StunDuration > FP._0)
			{
				StatusModifiers.AddStatusModifierToEntity(f, spell.Victim, StatusModifierType.Stun, source.StunDuration);
			}
			
			f.Events.OnProjectileTargetableHit(spell.SpellSource, spell.Victim, source, spell.OriginalHitPosition);
		}
	}
}