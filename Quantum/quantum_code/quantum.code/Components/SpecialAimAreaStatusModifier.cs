using System;
using System.Runtime.CompilerServices;
using Photon.Deterministic;
using Quantum.Systems;

namespace Quantum
{
	/// <summary>
	/// This class handles behaviour for the <see cref="SpecialAimAreaStatusModifier"/>
	/// </summary>
	public static class SpecialAimAreaStatusModifier
	{
		public static bool Use(Frame f, EntityRef e, Special special, FPVector2 aimInput, FP maxRange)
		{
			if (aimInput == FPVector2.Zero)
			{
				return false;
			}
			
			var attackerPosition = f.Get<Transform3D>(e).Position;
			var duration = special.PowerAmount;
			var rangeSquared = special.SplashRadius * special.SplashRadius;

			aimInput = FPVector2.ClampMagnitude(aimInput, FP._1);
			var targetPosition = attackerPosition + (aimInput * maxRange).XOY;

			foreach (var alivePlayer in f.GetComponentIterator<AlivePlayerCharacter>())
			{
				var playerPosition = f.Get<Transform3D>(alivePlayer.Entity).Position;
				
				if (FPVector3.DistanceSquared(targetPosition, playerPosition) > rangeSquared)
				{
					continue;
				}
				
				switch (special.SpecialType)
				{
					case SpecialType.RageAimAreaStatus:
						StatusModifiers.AddStatusModifierToEntity(f, alivePlayer.Entity, StatusModifierType.Rage, duration);
						break;
					case SpecialType.InvisibilityAimAreaStatus:
						StatusModifiers.AddStatusModifierToEntity(f, alivePlayer.Entity, StatusModifierType.Invisibility, duration);
						break;
					case SpecialType.ShieldAimAreaStatus:
						StatusModifiers.AddStatusModifierToEntity(f, alivePlayer.Entity, StatusModifierType.Shield, duration);
						break;
				}
			}
			
			return true;
		}
	}
}