using System;
using System.Runtime.CompilerServices;
using Photon.Deterministic;
using Quantum.Systems;

namespace Quantum
{
	/// <summary>
	/// This class handles behaviour for the <see cref="SpecialHealingField"/>
	/// </summary>
	public static class SpecialHealingField
	{
		public static bool Use(Frame f, EntityRef e, Special special)
		{
			if (!f.TryGet<Transform3D>(e, out var spawnTransform) || !f.TryGet<Targetable>(e, out var targetable))
			{
				return false;
			}
			
			spawnTransform.Position.Y += Constants.ACTOR_AS_TARGET_Y_OFFSET;
			
			Hazard.Create(f, special.ExtraGameId, spawnTransform.Position, e, targetable.Team);
			
			return true;
		}
	}
}