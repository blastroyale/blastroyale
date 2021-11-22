using System;
using System.Runtime.CompilerServices;
using Photon.Deterministic;
using Quantum.Systems;

namespace Quantum
{
	/// <summary>
	/// This class handles behaviour for the <see cref="SpecialAimAreaDamage"/>
	/// </summary>
	public static class SpecialAimAreaDamage
	{
		public static bool Use(Frame f, EntityRef e, Special special, FPVector2 aimInput, FP maxRange)
		{
			if (aimInput == FPVector2.Zero)
			{
				return false;
			}
			
			var attackerPosition = f.Get<Transform3D>(e).Position;
			var team = f.Get<Targetable>(e).Team;
			var specialType = special.SpecialType;
			
			aimInput = FPVector2.ClampMagnitude(aimInput, FP._1);
			var targetPosition = attackerPosition + (aimInput * maxRange).XOY;
			var spawnPosition = new FPVector3(targetPosition.X, targetPosition.Y + Constants.FAKE_PROJECTILE_Y_OFFSET, targetPosition.Z);
			
			var projectileData = new ProjectileData
			{
				ProjectileId = special.SpecialGameId,
				Attacker = e,
				ProjectileAssetRef = f.AssetConfigs.PlayerBulletPrototype.Id.Value,
				NormalizedDirection = FPVector3.Down,
				SpawnPosition = spawnPosition,
				TeamSource = team,
				IsHealing = specialType == SpecialType.HealAim,
				PowerAmount = special.PowerAmount,
				Speed = special.Speed,
				Range = Constants.FAKE_PROJECTILE_Y_OFFSET,
				SplashRadius = special.SplashRadius,
				StunDuration = FP._0,
				Target = EntityRef.None,
				IsHitOnRangeLimit = true,
				IsHitOnlyOnRangeLimit = true
			};
			
			Projectile.Create(f, projectileData);
			
			return true;
		}
	}
}