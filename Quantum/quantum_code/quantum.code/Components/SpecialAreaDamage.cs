using System;
using System.Runtime.CompilerServices;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This class handles behaviour for the <see cref="SpecialAreaDamage"/>
	/// </summary>
	public static class SpecialAreaDamage
	{
		public static bool Use(Frame f, EntityRef e, Special special)
		{
			var position = f.Get<Transform3D>(e).Position;
			position.Y += Constants.ACTOR_AS_TARGET_Y_OFFSET;
			// Spawn projectile slightly higher than actor to prevent
			// it from spawning below the ground
			var spawnPosition = new FPVector3(position.X, position.Y + Constants.FAKE_PROJECTILE_Y_OFFSET, position.Z);
			var team = f.Get<Targetable>(e).Team;
			var specialType = special.SpecialType;
			
			var projectileData = new ProjectileData
			{
				ProjectileId = special.SpecialGameId,
				Attacker = e,
				ProjectileAssetRef = f.AssetConfigs.PlayerBulletPrototype.Id.Value,
				NormalizedDirection = FPVector3.Down,
				SpawnPosition = spawnPosition,
				TeamSource = team,
				IsHealing = specialType == SpecialType.HealAround,
				PowerAmount = specialType == SpecialType.HealAround ? special.PowerAmount : 0,
				Speed = special.Speed,
				Range = Constants.FAKE_PROJECTILE_Y_OFFSET,
				SplashRadius = special.SplashRadius,
				StunDuration = specialType == SpecialType.StunSplash ? special.PowerAmount : FP._0,
				Target = EntityRef.None,
				IsHitOnRangeLimit = true,
				IsHitOnlyOnRangeLimit = true
			};
			
			Projectile.Create(f, projectileData);
			
			return true;
		}
	}
}