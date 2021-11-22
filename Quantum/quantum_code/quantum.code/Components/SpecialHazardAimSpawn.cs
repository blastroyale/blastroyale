using System;
using System.Runtime.CompilerServices;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This class handles behaviour for the <see cref="SpecialHazardAimSpawn"/>
	/// </summary>
	public static class SpecialHazardAimSpawn
	{
		public static unsafe bool Use(Frame f, EntityRef e, Special special, FPVector2 aimInput, FP maxRange)
		{
			var targetPosition = FPVector3.Zero;
			var attackerPosition = f.Get<Transform3D>(e).Position;
			attackerPosition.Y += Constants.ACTOR_AS_TARGET_Y_OFFSET;
			var team = f.Get<Targetable>(e).Team;
			
			if (f.TryGet<BotCharacter>(e, out var bot))
			{
				// Try to find a target in a range automatically
				var iterator = f.GetComponentIterator<Targetable>();
				foreach (var target in iterator)
				{
					if (!QuantumHelpers.IsAttackable(f, target.Entity) || (team == target.Component.Team) || 
					    !QuantumHelpers.IsEntityInRange(f, e, target.Entity, FP._0, maxRange))
					{
						continue;
					}
					
					targetPosition = f.Get<Transform3D>(target.Entity).Position;
					
					break;
				}
				
				// We make a random deviation based on bot's accuracy with specials
				var randomDirection = FPVector2.Rotate(FPVector2.Left, f.RNG->Next(FP._0, FP.PiTimes2));
				var randomDistance = f.RNG->Next(FP._0, bot.SpecialAimingDeviation);
				targetPosition = targetPosition + randomDirection.XOY * randomDistance;
			}
			else
			{
				aimInput = FPVector2.ClampMagnitude(aimInput, FP._1);
				targetPosition = attackerPosition + (aimInput * maxRange).XOY;
				targetPosition = QuantumHelpers.TryFindPosOnNavMesh(f, e, targetPosition, out var newPos) ? newPos : targetPosition;
			}
			
			var spawnPosition = new FPVector3(targetPosition.X, targetPosition.Y + Constants.FAKE_PROJECTILE_Y_OFFSET, targetPosition.Z);
			
			var projectileData = new ProjectileData
			{
				ProjectileId = GameId.BulletInvisible,
				Attacker = e,
				ProjectileAssetRef = f.AssetConfigs.PlayerBulletPrototype.Id.Value,
				NormalizedDirection = FPVector3.Down,
				SpawnPosition = spawnPosition,
				TeamSource = team,
				IsHealing = false,
				PowerAmount = special.PowerAmount,
				Speed = Constants.PROJECTILE_MAX_SPEED,
				Range = Constants.FAKE_PROJECTILE_Y_OFFSET,
				SplashRadius = special.SplashRadius,
				StunDuration = FP._0,
				Target = EntityRef.None,
				LaunchTime = f.Time + special.Speed,
				IsHitOnRangeLimit = true,
				IsHitOnlyOnRangeLimit = true,
				SpawnHazardId = special.ExtraGameId
			};
			
			var projectile = Projectile.Create(f, projectileData);
			
			f.Events.OnSkyBeamUsed(projectile, targetPosition, projectileData);
			
			return true;
		}
	}
}