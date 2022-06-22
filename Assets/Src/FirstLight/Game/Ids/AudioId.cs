using System.Collections.Generic;

namespace FirstLight.Game.Ids
{
	public enum AudioId
	{ 
		// Check all
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
		
		//Music
		MainMenuStart,
		MainMenuLoopNew,
		BrLowLoop,
		BrMidLoop,
		BrFinalDuelLoop,
		DmLoop,
		DmFinalDuelLoop,
		VictoryJingle,
		DefeatJingle,
		PostMatchLoop,
		FinalDuelTransitionJingle,
		
		//Weapons
		PlaCrossbowWeaponShot,
		PlaShotgunWeaponShot,
		PlaMachineGunWeaponShot,
		PlaRiffleWeaponShot,
		PlaSniperWeaponShot,
		PlaRocketLauncherWeaponShot,
		PlaRocketLauncherFlyingTrailLoop,
		PlaMiniGunRevUp,
		PlaMiniGunWeaponShot,
		MmsPistolWeaponShot,
		MmsShotgunWeaponShot,
		MmsMachineGunWeaponShot,
		MmsRiffleWeaponShot,
		MmsSniperRifleWeaponShot,
		MmsGrenadeLauncherWeaponShot,
		MmsGrenadeLauncherFlyingTrailLoop,
		MmsHeavyMachineGunWeaponShot,
		EdiLaserPistolWeaponShot,
		EdiLaserBlasterWeaponShot,
		EdiNeedleGunWeaponShot,
		EdiLaserRifleWeaponShot,
		EdiPulseRifleWeaponShot,
		EdiPulseRifleWeaponChargeUp,
		EdiPlasmaCannonWeaponShot,
		EdiPlasmaCannonFlyingTrailLoop,
		EdiElectronThrowerWeaponStreamLoop,
		
		//Weapon Misc
		ReloadPlaLoop,
		ReloadMmsLoop,
		ReloadEdiLoop,
		AmmoEmpty,
		
		//Combat sounds
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
		ExplosionFlashBang,
		GettingFlashBanged,
		MissileFlyLoop,
		ChargeStartUp,
		ImpactLoop,
		Dash,
		InvLoop,
		InvStart,
		InvEnd,
		InvDamageAbsorb,
		SdLoop,
		SdStart,
		SdEnd,
		
		//GameplaySounds
		AmmoPickup,
		LargeAmmoPickup,
		ShieldPickup,
		LargeShieldPickup,
		HealthPickup,
		LargeHealthPickup,
		WeaponPickup,
		GearPickup,
		WeaponSwitch,
		SkydiveJetpackDiveLoop,
		SkydiveEnd,
		ChestProgressLoop,
		ChestLootDrop,
		PlayerHeartBeatLoop,
		PlayerEnterLava,
		PlayerEnterWater,
		PlayerWalkGrassLoop,
		PlayerWalkRoadLoop,
		PlayerWalkTilesLoop,
		PlayerWalkSandLoop,
		PlayerWalkWaterLoop,
		PlayerWalkLavaLoop,
		PlayerRespawnShine,
		PlayerRespawnLightningBolt,
		
		//UI sounds
		ButtonClickForward,
		ButtonClickBackward,
		SettingsToggle,
		EnterGame,
		ChooseGameMode,
		TrophiesEarnedTick,
		TrophiesLostTick,
		CounterTick1,
		CounterTick2,
		CounterTick3,
		EquipEquipment,
		UnequipEquipment,
		HeroPicked,
		PopupAppearInfo,
		PopupAppearChoice,
		PopupAppearError,
		DisconnectScreenAppear,
		MarketplaceJingle,
		
		//Ambient sounds
		ForestAmbientLoop,
		DesertAmbientLoop,
		CentralAmbientLoop,
		UrbanAmbientLoop,
		FrostAmbientLoop,
		LavaAmbientLoop,
		WaterAmbientLoop,
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