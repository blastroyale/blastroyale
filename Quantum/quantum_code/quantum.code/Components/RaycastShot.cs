using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct RaycastShot
	{
		/// <summary>
		/// Creates a RaycastShot from the given projectile <paramref name="raycastShot"/>
		/// </summary>
		public static EntityRef Create(Frame f, RaycastShot raycastShot)
		{
			raycastShot.StartTime = f.Time;
			raycastShot.PreviousToLastBulletPosition = FPVector3.Zero;
			raycastShot.LastBulletPosition = raycastShot.SpawnPosition;
			
			var e = f.Create();
			f.Add(e, raycastShot);

			return e;
		}
	}
}