using System;
using System.Runtime.CompilerServices;
using Photon.Deterministic;
using Quantum.Systems;

namespace Quantum
{
	/// <summary>
	/// This class handles behaviour for the <see cref="SpecialHealingMode"/>
	/// </summary>
	public static class SpecialHealingMode
	{
		public static unsafe bool Use(Frame f, EntityRef e, Special special)
		{
			if (!f.Unsafe.TryGetPointer<Weapon>(e, out var weapon))
			{
				return false;
			}
			
			var duration = special.PowerAmount;
			var specialPointer = weapon->Specials.GetPointer(special.SpecialIndex);
			
			weapon->IsHealing = true;
			specialPointer->HealingModeSwitchTime = f.Time + duration;
			
			return true;
		}
	}
}