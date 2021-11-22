using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// Handles <see cref="PowerUpType.Frontshot"/> power-up with the data contained in the <see cref="Frontshot"/> component
	/// </summary>
	public unsafe class FrontshotSystem : SystemSignalsOnly, ISignalProjectileShootTriggered
	{
		/// <inheritdoc />
		public void ProjectileShootTriggered(Frame f, EntityRef projectile)
		{
			var projectileData = f.Get<Projectile>(projectile).Data;
			
			if (!f.TryGet<Frontshot>(projectileData.Attacker, out var frontshot))
			{
				return;
			}

			var amount = (frontshot.BaseAmount + frontshot.AmountLevelUpStep * (frontshot.Level - 1));
			var directionToRight = FPQuaternion.AngleAxis(90, FPVector3.Up) * projectileData.NormalizedDirection;
			var halfTotalWidth = directionToRight * frontshot.Spread * FPMath.FloorToInt(amount / FP._2);
			var initialPosition = projectileData.SpawnPosition - halfTotalWidth;
			var skipProjectileIndex = FPMath.FloorToInt(amount / FP._2);
			
			for (var i = 0; i < amount; i++)
			{
				// Skip the main projectile that already fired and triggered Frontshot
				if (i == skipProjectileIndex)
				{
					continue;
				}
				
				var newData = projectileData;
				
				newData.SpawnPosition = initialPosition + (directionToRight * frontshot.Spread * i);
				
				Projectile.Create(f, newData);
			}
		}
	}
}