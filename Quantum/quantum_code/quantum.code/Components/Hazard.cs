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
					if (!QuantumHelpers.IsAttackable(f, target.Entity, hazard->TeamSource) || 
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
			
			hazard->InitData(f, hazardConfig, teamSource, attacker);
			physicsCollider->Shape = Shape3D.CreateSphere(hazardConfig.Radius);
			
			return entity;
		}
		
		/// <summary>
		/// Initializes this Hazard with all the necessary data
		/// </summary>
		private void InitData(Frame f, QuantumHazardConfig config, int teamSource, EntityRef attacker)
		{
			GameId = config.Id;
			Radius = config.Radius;
			DestroyTime = f.Time + config.Lifetime;
			Interval = config.Interval;
			PowerAmount = config.Damage;
			IsHealing = config.IsHealing;
			TeamSource = teamSource;
			Attacker = attacker;
			NextApplyTime = f.Time + config.ActivationDelay;
			IsActive = false;
		}
	}
}