using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This class handles behaviour for the <see cref="SpecialGrenade"/>
	/// </summary>
	public static class SpecialGrenade
	{
		public static unsafe bool Use(Frame f, EntityRef e, ref Special special, FPVector2 aimInput, FP maxRange)
		{
			if (!f.Exists(e) || f.Has<DeadPlayerCharacter>(e))
			{
				return false;
			}
			
			var targetPosition = FPVector3.Zero;
			var attackerPosition = f.Unsafe.GetPointer<Transform3D>(e)->Position;
			attackerPosition.Y += Constants.ACTOR_AS_TARGET_Y_OFFSET;
			var team = f.Unsafe.GetPointer<Targetable>(e)->Team;
			
			if (f.TryGet<BotCharacter>(e, out var bot))
			{
				// Try to find a target in a range automatically
				var iterator = f.Unsafe.GetComponentBlockIterator<Targetable>();
				foreach (var target in iterator)
				{
					if (!QuantumHelpers.IsAttackable(f, target.Entity, team) || 
					    !QuantumHelpers.IsEntityInRange(f, e, target.Entity, FP._0, maxRange))
					{
						continue;
					}
					
					targetPosition = f.Unsafe.GetPointer<Transform3D>(target.Entity)->Position;
					
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
			}
			
			var maxProjectileFlyingTime = special.Speed;
			var targetRange = special.MaxRange;
			var launchTime = maxProjectileFlyingTime * ((targetPosition - attackerPosition).Magnitude / targetRange);

			var hazardData = new Hazard
			{
				Attacker = e,
				EndTime = f.Time + launchTime,
				GameId = special.SpecialId,
				Interval = special.Speed,
				NextTickTime = f.Time + launchTime,
				PowerAmount = special.SpecialPower,
				Radius = special.Radius,
				StunDuration = FP._0,
				TeamSource = team,
				MaxHitCount = uint.MaxValue,
				Knockback = special.Knockback
			};
			
			var hazard = Hazard.Create(f, ref hazardData, targetPosition);
			
			f.Events.OnGrenadeUsed(hazard, targetPosition, hazardData);
			
			return true;
		}
	}
}