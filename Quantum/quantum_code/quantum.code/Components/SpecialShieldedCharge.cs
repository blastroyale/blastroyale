using System;
using System.Runtime.CompilerServices;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This class handles behaviour for the <see cref="SpecialShieldedCharge"/>
	/// </summary>
	public static class SpecialShieldedCharge
	{
		public static unsafe bool Use(Frame f, EntityRef e, Special special, FPVector2 aimInput, FP maxRange)
		{
			var targetPosition = FPVector3.Zero;
			var attackerPosition = f.Get<Transform3D>(e).Position;
			attackerPosition.Y += Constants.ACTOR_AS_TARGET_Y_OFFSET;
			var teamSource = f.Get<Targetable>(e).Team;
			
			if (f.TryGet<BotCharacter>(e, out var bot))
			{
				// Try to find a target in a range automatically
				var iterator = f.GetComponentIterator<Targetable>();
				foreach (var target in iterator)
				{
					if (!QuantumHelpers.IsAttackable(f, target.Entity, teamSource) ||
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
			}
			
			// Testing 2 or more positions on the way towards targetPosition to find the last navmesh valid position before unsuitable one
			var previousTestPos = attackerPosition;
			var stepsToCheck = FPMath.Max(FPMath.CeilToInt((targetPosition - attackerPosition).Magnitude / Constants.CHARGE_VALIDITY_CHECK_DISTANCE_STEP), FP._2);
			for (var i = 0; i < stepsToCheck; i++)
			{
				var testPos = FPVector3.Lerp(attackerPosition, targetPosition, (FP)i / (stepsToCheck - 1));
				
				if (f.NavMesh.Contains(testPos, NavMeshRegionMask.Default))
				{
					previousTestPos = testPos;
				}
				else
				{
					break;
				}
			}
			targetPosition = previousTestPos;
			
			var chargeDistance = (targetPosition - attackerPosition).Magnitude;
			
			// If there's no valid position to move then we cancel the usage of a special
			if (chargeDistance == FP._0)
			{
				return false;
			}
			
			var chargeDuration = chargeDistance / special.Speed;

			var chargeComponent = new PlayerCharging
			{
				ChargeDuration = chargeDuration,
				ChargeStartPos = attackerPosition,
				ChargeEndPos = targetPosition,
				ChargeStartTime = f.Time,
				PowerAmount = (uint)special.SpecialPower,
				PercentHealthDamage = special.PercentHealthDamage
			};
			
			QuantumHelpers.LookAt2d(f, e, targetPosition);
			StatusModifiers.AddStatusModifierToEntity(f, e, StatusModifierType.Shield, chargeDuration, true);
			f.Unsafe.GetPointer<PhysicsCollider3D>(e)->IsTrigger = true;
			
			f.Add(e, chargeComponent);
			
			f.Events.OnShieldedChargeUsed(e, chargeDuration);
			
			return true;
		}
	}
}