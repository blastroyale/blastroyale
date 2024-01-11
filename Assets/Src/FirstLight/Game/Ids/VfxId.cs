using System.Collections.Generic;
using Quantum;

namespace FirstLight.Game.Ids
{
	public enum VfxId
	{
		SplashProjectile = 1,
		ImpactAirStrike = 2,
		StatusFxStun = 3,
		StatusFxStar = 4,
		StatusFxRage = 5,
		StatusFxHeal = 6,
		SpecialReticule = 7,
		GrenadeParabolic = 8,
		CollectableIndicator = 9,
		GrenadeStunParabolic = 10,
		ImpactGrenade = 11,
		ImpactGrenadeStun = 12,
		EnergyShield = 13,
		Airstrike = 14,
		Skybeam = 15,
		Ping = 16,
		Radar = 17,
		LocationPointer = 18,
		WeaponAim = 19,
		CollectableIndicatorLarge = 20,
		ProjectileFailedSmoke = 21,
		Shell = 22,
		DeathEffect = 23,
		StepSmoke = 24,
		ChestPickupFx = 25,
		HealthPickupFx = 26,
		ShieldPickupFx = 27,
		AmmoPickupFx = 28,
		SpecialAndWeaponPickupFx = 29,
		AirdropPickupFx = 30,
		GoldenEffect = 31,
		CoinPickupFx = 32,
		BppPickupFx = 33
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