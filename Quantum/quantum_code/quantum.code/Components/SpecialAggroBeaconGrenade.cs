using System;
using System.Runtime.CompilerServices;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This class handles behaviour for the <see cref="SpecialAggroBeaconGrenade"/>
	/// </summary>
	public static class SpecialAggroBeaconGrenade
	{
		public static bool Use(Frame f, EntityRef e, Special specialComponent, FPVector2 aimInput, FP maxRange)
		{
			if (aimInput == FPVector2.Zero)
			{
				return false;
			}
			
			var attackerPosition = f.Get<Transform3D>(e).Position;
			attackerPosition.Y += Constants.ACTOR_AS_TARGET_Y_OFFSET;
			var team = f.Get<Targetable>(e).Team;
			var targetPosition = attackerPosition + (FPVector2.ClampMagnitude(aimInput, FP._1) * maxRange).XOY;
			var spawnPosition = new FPVector3(targetPosition.X, targetPosition.Y + Constants.FAKE_PROJECTILE_Y_OFFSET, targetPosition.Z);
			var maxProjectileFlyingTime = specialComponent.Speed;
			var targetRange = specialComponent.MaxRange;
			var launchTime = maxProjectileFlyingTime * ((targetPosition - attackerPosition).Magnitude / targetRange);
			
			// Projectile creates a hazard with a specified id when hits
			var projectileData = new ProjectileData
			{
				Attacker = e,
				ProjectileAssetRef = f.AssetConfigs.PlayerBulletPrototype.Id.Value,
				NormalizedDirection = FPVector3.Down,
				SpawnPosition = spawnPosition,
				TeamSource = team,
				IsHealing = false,
				PowerAmount = 0,
				Speed = Constants.PROJECTILE_MAX_SPEED,
				Range = Constants.FAKE_PROJECTILE_Y_OFFSET,
				SplashRadius = FP._0,
				StunDuration = FP._0,
				Target = EntityRef.None,
				LaunchTime = f.Time + launchTime,
				IsHitOnRangeLimit = true,
				IsHitOnlyOnRangeLimit = true,
				SpawnHazardId = GameId.AggroBeaconHazard
			};
			
			Projectile.Create(f, projectileData);
			
			f.Events.OnAggroBeaconGrenadeUsed(targetPosition, e, f.Time + launchTime);
			
			return true;
		}
	}
}