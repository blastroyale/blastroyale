using System.Collections.Generic;

namespace FirstLight.Game.Ids
{
	public enum AudioId
	{
		ElClassico,
		CollectPickupRage,
		CollectPickupHealth,
		CollectPickupSpecial,
		AdventureBossLoop,
		AdventureMainLoop,
		AdventureRelaxedLoop,
		MenuMainLoop,
		AdventureStart1,
		PlayerRevived1,
		GameOver1,
		Victory1,
		LootDropped01,
		ActorSpawnStart1,
		ActorSpawnEnd1,
		ProjectileFired01,
		ActorHit01,
		ActorDeath01,
		GeneralTap,
		GeneralPopupOpen,
		DoubleKill,
		MultiKill,
		KillingSpree,
		YouTasteDeath,
		OhYeah01,
		OhYeah02,
		LetsRock,
		LockAndLoad,
		ComeGetSome,
		Groovy,
		HastaLaVista,
		
		//New sounds 
		TakeHealthDamage,
		TakeShieldDamage,
		HitHealthDamage,
		HitShieldDamage,
		ShieldBreak,
		PlayerKill,
		PlayerDeath,
		ExplosionSmall,
		ExplosionMedium,
		ExplosionLarge,
		ExplosionSciFi,
		ExplosionFlashbang,
		GettingFlashBanged,
		MissileFlyLoop,
		ChargeStartUp,
		ImpactLoop,
		Dash,
		InvLoop,
		InvStart,
		InvEnd,
		InvDamageAbsorb,
		None, // TODO: Make an Enum selector
	}
	
	/// <summary>
	/// Avoids boxing for Dictionary
	/// </summary>
	public class AudioIdComparer : IEqualityComparer<AudioId>
	{
		public bool Equals(AudioId x, AudioId y)
		{
			return x == y;
		}

		public int GetHashCode(AudioId obj)
		{
			return (int)obj;
		}
	}
}