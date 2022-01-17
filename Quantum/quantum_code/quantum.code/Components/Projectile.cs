using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct Projectile
	{
		/// <summary>
		/// Creates a projectile from the given projectile <paramref name="projectile"/>
		/// </summary>
		public static EntityRef Create(Frame f, Projectile projectile)
		{
			var e = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.PlayerBulletPrototype.Id));
			var transform = f.Unsafe.GetPointer<Transform3D>(e);

			transform->Position = projectile.SpawnPosition;
			transform->Rotation = FPQuaternion.LookRotation(projectile.Direction, FPVector3.Up);

			f.Add(e, projectile);
			f.Events.OnProjectileFired(e, projectile);

			return e;
		}
	}
}