using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct Projectile
	{
		/// <summary>
		/// Creates a projectile from the given projectile <paramref name="data"/>
		/// </summary>
		public static EntityRef Create(Frame f, ProjectileData data)
		{
			var projectile = f.Create(f.FindAsset<EntityPrototype>(data.ProjectileAssetRef));

			f.Unsafe.GetPointer<Projectile>(projectile)->Init(f, projectile, data);
			
			// We may not send an event for attacks that have their own specific events, like Melee attack
			if (!data.IsNoEventSending)
			{
				f.Events.OnProjectileFired(projectile, data);
			}
			
			f.Signals.ProjectileFired(projectile);

			return projectile;
		}
		
		/// <summary>
		/// Initializes this projectile with all the necessary data
		/// </summary>
		private void Init(Frame f, EntityRef e, ProjectileData data)
		{
			var transform = f.Unsafe.GetPointer<Transform3D>(e);

			transform->Position = data.SpawnPosition;
			transform->Rotation = FPQuaternion.LookRotation(data.NormalizedDirection, FPVector3.Up);
			Data = data;
		}
		
		/// <summary>
		/// Creates a spherical projectile without a view from the given projectile <paramref name="data"/>
		/// and <paramref name="colliderRadius"/> 
		/// </summary>
		public static EntityRef CreateSplash(Frame f, ProjectileData data, FP colliderRadius)
		{
			var splashSphere = f.Create(f.FindAsset<EntityPrototype>(data.ProjectileAssetRef));
			f.Unsafe.GetPointer<Projectile>(splashSphere)->Init(f, splashSphere, data);
			
			var collider = f.Unsafe.GetPointer<PhysicsCollider3D>(splashSphere);
			collider->Shape = Shape3D.CreateSphere(colliderRadius);
			collider->IsTrigger = true;
			
			// TODO: Create new entity without using a prototype
			// var splashSphere = f.Create();
			// var projectile = new Projectile();
			// var collider = PhysicsCollider3D.Create(f, Shape3D.CreateSphere(colliderRadius), null, true);
			// var transform = Transform3D.Create();
			// var flags = CallbackFlags.OnDynamicTriggerEnter | CallbackFlags.OnStaticTriggerEnter;
			//
			// f.Add(splashSphere, transform);
			// f.Add(splashSphere, projectile);
			// f.Add(splashSphere, collider);
			// f.Physics3D.SetCallbacks(splashSphere, flags);
			//
			// projectile.Init(f, splashSphere, data);
			
			f.Signals.ProjectileFired(splashSphere);
			
			return splashSphere;
		}
		
		/// <summary>
		/// Divert the <paramref name="originalDirection"/> on random <paramref name="angle"/>
		/// </summary>
		public static FPVector3 DivertOnRandomAngle(Frame f, FPVector3 originalDirection, int angle)
		{
			// TODO: Use normal distribution instead of uniform
			var randomAngle = f.RNG->Next(-angle, angle);
			return FPQuaternion.AngleAxis(randomAngle, FPVector3.Up) * originalDirection;
		}
	}
}