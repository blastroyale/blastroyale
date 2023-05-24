using System.Collections.Generic;
using Quantum;

namespace FirstLight.Game.Ids
{
	public enum VfxId
	{
		FootprintL,
		FootprintR,
		SpawnEnemy,
		SpawnPlayer,
		DustCloudSmall,
		SplashProjectile,
		SplatsHitPlayer,
		SplatsHitEnemy,
		ImpactAirStrike,
		DustCloudLarge,
		StatusFxStun,
		StatusFxStar,
		StatusFxRage,
		StatusFxHeal,
		CoinDrop,
		SpecialReticule,
		MolotovParabolic,
		GrenadeParabolic,
		CollectableIndicator,
		GrenadeStunParabolic,
		ImpactGrenade,
		ImpactGrenadeStun,
		EnergyShield,
		Fireworks,
		Airstrike,
		Skybeam,
		Ping,
		Radar,
		LocationPointer,
		WeaponAim,
		CollectableIndicatorLarge,
		TOTAL,            // Used to know the total amount of this type without the need of reflection
	}
	
	/// <summary>
	/// Avoids boxing for Dictionary
	/// </summary>
	public class VfxIdComparer : IEqualityComparer<VfxId>
	{
		public bool Equals(VfxId x, VfxId y)
		{
			return x == y;
		}

		public int GetHashCode(VfxId obj)
		{
			return (int)obj;
		}
	}

	public static class VfxIdLookup
	{
		public static bool TryGetVfx(this StatusModifierType modifier, out VfxId vfx)
		{
			return _modifiers.TryGetValue(modifier, out vfx);
		}
		
		private static readonly Dictionary<StatusModifierType, VfxId> _modifiers =
			new Dictionary<StatusModifierType, VfxId>
			{
				{StatusModifierType.Star, VfxId.StatusFxStar},
				{StatusModifierType.Regeneration, VfxId.StatusFxHeal},
				{StatusModifierType.Rage, VfxId.StatusFxRage},
				{StatusModifierType.Stun, VfxId.StatusFxStun}
			};
	}
}