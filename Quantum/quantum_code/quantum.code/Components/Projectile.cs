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