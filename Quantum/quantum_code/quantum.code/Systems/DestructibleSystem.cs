using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the changes in a <see cref="Destructible"/> entity
	/// </summary>
	public unsafe class DestructibleSystem : SystemMainThreadFilter<DestructibleSystem.DestructibleFilter>,
	                                         ISignalHealthIsZero
	{
		public struct DestructibleFilter
		{
			public EntityRef Entity;
			public Destructible* Destructible;
		}
		
		/// <inheritdoc />
		public override void Update(Frame f, ref DestructibleFilter filter)
		{
			if (filter.Destructible->IsDestructing && f.Time >= filter.Destructible->TimeToDestroy)
			{
				f.Add<EntityDestroyer>(filter.Entity);
			}
		}
		
		/// <inheritdoc />
		public void HealthIsZero(Frame f, EntityRef entity, EntityRef attacker)
		{
			if (!f.Unsafe.TryGetPointer<Destructible>(entity, out var destructible) || destructible->IsDestructing)
			{
				return;
			}
			
			var transform = f.Unsafe.GetPointer<Transform3D>(entity);
			var stats = f.Get<Stats>(entity);
			var spawnPosition = transform->Position;
			var projectileData = new ProjectileData
			{
				Attacker = entity,
				ProjectileAssetRef = destructible->ProjectileAssetRef.Id.Value,
				NormalizedDirection = FPVector3.Down,
				SpawnPosition = spawnPosition + FPVector3.Up * Constants.FAKE_PROJECTILE_Y_OFFSET,
				TeamSource = (int) TeamType.Neutral,
				IsHealing = false,
				PowerAmount = (uint) stats.Values[(int) StatType.Power].StatValue.AsInt,
				Speed = Constants.PROJECTILE_MAX_SPEED,
				Range = Constants.FAKE_PROJECTILE_Y_OFFSET,
				SplashRadius = destructible->SplashRadius,
				StunDuration = FP._0,
				Target = EntityRef.None,
				IsHitOnRangeLimit = true,
				IsHitOnlyOnRangeLimit = true,
				LaunchTime = f.Time + destructible->DestructionLengthTime
			};
			
			var projectile = Projectile.Create(f, projectileData);
			
			destructible->TimeToDestroy = f.Time + destructible->DestructionLengthTime;
			destructible->IsDestructing = true;
			
			f.Events.OnDestructibleScheduled(entity, *destructible, projectile, projectileData);
		}
	}
}