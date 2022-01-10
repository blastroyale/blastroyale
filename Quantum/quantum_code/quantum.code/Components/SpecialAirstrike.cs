using System;
using System.Runtime.CompilerServices;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This class handles behaviour for the <see cref="SpecialAirstrike"/>
	/// </summary>
	public static class SpecialAirstrike
	{
		public static unsafe bool Use(Frame f, EntityRef e, Special airstrike, FPVector2 aimInput, FP maxRange)
		{
			var targetPosition = FPVector3.Zero;
			var attackerPosition = f.Get<Transform3D>(e).Position;
			var team = f.Get<Targetable>(e).Team;
			attackerPosition.Y += Constants.ACTOR_AS_TARGET_Y_OFFSET;
			
			if (f.TryGet<BotCharacter>(e, out var bot))
			{
				// Try to find a target in a range automatically
				var iterator = f.GetComponentIterator<Targetable>();
				foreach (var target in iterator)
				{
					if (!QuantumHelpers.IsAttackable(f, target.Entity, team) || 
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
				targetPosition = attackerPosition + (FPVector2.ClampMagnitude(aimInput, FP._1) * maxRange).XOY;
				targetPosition = QuantumHelpers.TryFindPosOnNavMesh(f, e, targetPosition, out var newPos) ? newPos : targetPosition;
			}
			
			var spawnPosition = new FPVector3(targetPosition.X, targetPosition.Y + Constants.ProjectileSkyRange, targetPosition.Z);
			var direction = targetPosition - spawnPosition;
			var projectileData = new Projectile
			{
				Attacker = e,
				Direction = direction.Normalized,
				IsPiercing = false,
				PowerAmount = airstrike.PowerAmount,
				Range = Constants.ProjectileSkyRange,
				SourceId = airstrike.SpecialId,
				SpawnPosition = spawnPosition,
				Speed = airstrike.Speed,
				SplashRadius = airstrike.Radius,
				StunDuration = FP._0,
				Target = EntityRef.None,
				TeamSource = team
			};
			
			var projectile = Projectile.Create(f, projectileData);
			
			f.Events.OnAirstrikeUsed(projectile, targetPosition, projectileData);
			
			return true;
		}
	}
}