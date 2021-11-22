using Photon.Deterministic;

namespace Quantum
{
	public partial struct Hazard
	{
		/// <summary>
		/// Create Hazard with id <see cref="hazardId"/>
		/// </summary>
		public static unsafe EntityRef Create(Frame f, GameId hazardId, FPVector3 spawnPosition, EntityRef source, 
		                                      int teamSource)
		{
			var hazardConfig = f.HazardConfigs.QuantumConfigs.Find(config => config.Id == hazardId);
			
			return Create(f, hazardConfig, spawnPosition, source, teamSource);
		}
		
		/// <summary>
		/// Create Hazard with id <see cref="hazardId"/> but with overriding <see cref="damage"/>, <see cref="radius"/> data
		/// </summary>
		public static unsafe EntityRef Create(Frame f, GameId hazardId, FPVector3 spawnPosition, EntityRef source, 
		                                      int teamSource, uint damage, FP radius)
		{
			var hazardConfig = f.HazardConfigs.QuantumConfigs.Find(config => config.Id == hazardId);
			
			hazardConfig.Damage = damage;
			hazardConfig.Radius = radius;
			
			return Create(f, hazardConfig, spawnPosition, source, teamSource);
		}

		/// <summary>
		/// Actually creates Hazard based on config passed
		/// </summary>
		private static unsafe EntityRef Create(Frame f, QuantumHazardConfig hazardConfig, FPVector3 spawnPosition, 
		                                       EntityRef source, int teamSource)
		{
			var entity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.HazardPrototype.Id));
			var hazard = f.Unsafe.GetPointer<Hazard>(entity);
			var hazardTransform = f.Unsafe.GetPointer<Transform3D>(entity);
			var physicsCollider = f.Unsafe.GetPointer<PhysicsCollider3D>(entity);
			var attacker = source;
			
			// If the aiming help is active for this hazard then we look for a target
			if (hazardConfig.AimHelpingRadius > FP._0)
			{
				// Try to find a target in a that radius
				var sqrRadius = hazardConfig.AimHelpingRadius * hazardConfig.AimHelpingRadius;
				var iterator = f.GetComponentIterator<Targetable>();
				foreach (var target in iterator)
				{
					if (!QuantumHelpers.IsAttackable(f, target.Entity) || 
					    (teamSource == target.Component.Team && !hazardConfig.IsHealing) || 
					    (teamSource != target.Component.Team && hazardConfig.IsHealing) ||
					    (f.Get<Transform3D>(target.Entity).Position - spawnPosition).SqrMagnitude > sqrRadius)
					{
						continue;
					}
					
					spawnPosition = f.Get<Transform3D>(target.Entity).Position;
					
					break;
				}
			}
			
			hazardTransform->Position = spawnPosition;
			hazardTransform->Rotation = FPQuaternion.LookRotation(FPVector3.Forward, FPVector3.Up);

			/* TODO: Delete when Hazards, Projectiles & specials are cleaned up
			if (hazardConfig.Id == GameId.AggroBeaconHazard)
			{
				var targetable = new Targetable
				{
					Team = teamSource,
					IsUntargetable = false
				};
				var projectileData = new ProjectileData
				{
					Attacker = source,
					ProjectileAssetRef = f.AssetConfigs.PlayerBulletPrototype.Id.Value,
					NormalizedDirection = FPVector3.Down,
					SpawnPosition = new FPVector3(spawnPosition.X, spawnPosition.Y + Constants.FAKE_PROJECTILE_Y_OFFSET, spawnPosition.Z),
					TeamSource = teamSource,
					IsHealing = false,
					PowerAmount = Constants.AGGRO_OBJECT_EXPLOSION_DAMAGE,
					Speed = Constants.PROJECTILE_MAX_SPEED,
					Range = Constants.FAKE_PROJECTILE_Y_OFFSET,
					SplashRadius = Constants.AGGRO_OBJECT_EXPLOSION_RADIUS,
					StunDuration = FP._0,
					Target = EntityRef.None,
					IsHitOnRangeLimit = true,
					IsHitOnlyOnRangeLimit = true,
					LaunchTime = f.Time + hazardConfig.Lifetime
				};
				
				var collider = *physicsCollider;

				collider.IsTrigger = false;
				collider.Layer = f.PlayerCharacterLayerMask;
				attacker = f.Create();
				
				Projectile.Create(f, projectileData);
				
				f.Add(entity, targetable);
				f.Add(attacker, *hazardTransform);
				f.Add(attacker, targetable);
				f.Add(attacker, collider);
			}*/
			
			hazard->InitData(f, hazardConfig, teamSource, attacker);
			physicsCollider->Shape = Shape3D.CreateSphere(hazardConfig.Radius);
			
			return entity;
		}
		
		/// <summary>
		/// Initializes this Hazard with all the necessary data
		/// </summary>
		private void InitData(Frame f, QuantumHazardConfig config, int teamSource, EntityRef attacker)
		{
			var isPlayerHealing = f.TryGet<Weapon>(attacker, out var weapon) && weapon.IsHealing; 
			
			GameId = config.Id;
			Radius = config.Radius;
			DestroyTime = f.Time + config.Lifetime;
			Interval = config.Interval;
			PowerAmount = config.Damage;
			IsHealing = isPlayerHealing || (QBoolean) config.IsHealing;
			TeamSource = teamSource;
			Attacker = attacker;
			NextApplyTime = f.Time + config.ActivationDelay;
			IsActive = false;
		}
	}
}