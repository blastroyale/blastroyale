using System;
using System.Runtime.CompilerServices;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This class handles behaviour for the <see cref="SpecialStunGrenade"/>
	/// </summary>
	public static class SpecialStunGrenade
	{
		public static unsafe bool Use(Frame f, EntityRef e, in Special special, FPVector2 aimInput, FP maxRange)
		{
			if (!f.Exists(e) || f.Has<DeadPlayerCharacter>(e))
			{
				return false;
			}
			
			var targetPosition = FPVector2.Zero;
			var attackerPosition = f.Unsafe.GetPointer<Transform2D>(e)->Position;
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
					
					targetPosition = f.Unsafe.GetPointer<Transform2D>(target.Entity)->Position;
					
					break;
				}
				
				// We make a random deviation based on bot's accuracy with specials
				var randomDirection = FPVector2.Rotate(FPVector2.Left, f.RNG->Next(FP._0, FP.PiTimes2));
				var randomDistance = f.RNG->Next(FP._0, bot.SpecialAimingDeviation);
				targetPosition = targetPosition + randomDirection * randomDistance;
			}
			else
			{
				aimInput = FPVector2.ClampMagnitude(aimInput, FP._1);
				targetPosition = attackerPosition + (aimInput * maxRange);
			}
			
			var targetRange = special.MaxRange;
			var launchTime = special.Speed * ((targetPosition - attackerPosition).Magnitude / targetRange);

			var hazardData = new Hazard
			{
				Attacker = e,
				EndTime = f.Time + launchTime,
				GameId = special.SpecialId,
				Interval = special.Speed,
				NextTickTime = f.Time + launchTime,
				PowerAmount = 0,
				Radius = special.Radius,
				StunDuration = special.SpecialPower,
				TeamSource = team,
				MaxHitCount = uint.MaxValue
			};
			
			var hazard = Hazard.Create(f, ref hazardData, targetPosition);
			
			f.Events.OnStunGrenadeUsed(hazard, targetPosition, hazardData);
			
			return true;
		}
	}
}