using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// Handles <see cref="Diagonalshot"/> power-up with the data contained in the <see cref="Diagonalshot"/> component
	/// </summary>
	public unsafe class DiagonalshotSystem : SystemSignalsOnly, ISignalProjectileShootTriggered
	{
		/// <inheritdoc />
		public void ProjectileShootTriggered(Frame f, EntityRef projectile)
		{
			var data = f.Get<Projectile>(projectile).Data;
			
			if (!f.TryGet<Diagonalshot>(data.Attacker, out var diagonalshot))
			{
				return;
			}

			var level = diagonalshot.Level - 1;
			var amount = diagonalshot.BaseAmount + diagonalshot.AmountLevelUpStep * level;
			var angle = diagonalshot.BaseAngle + diagonalshot.AngleLevelUpStep * level;
			var skipIndex = FPMath.FloorToInt((amount - 1) / FP._2); 
			var angleStep = angle / (amount - FP._1);
			
			for (var i = 0; i < amount; i++)
			{
				// To skip the main projectile that already fired
				if (i == skipIndex)
				{
					continue;
				}
				
				var shootAngle = angleStep * (i - skipIndex);
				var newData = data;
				
				newData.NormalizedDirection = FPQuaternion.AngleAxis(shootAngle, FPVector3.Up) * data.NormalizedDirection;
				
				Projectile.Create(f, newData);
			}
		}
	}
}