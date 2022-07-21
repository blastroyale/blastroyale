using System.Collections.Generic;

namespace FirstLight.Game.Ids
{
	public enum AudioId
	{
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
		//Old Music
		AdventureBossLoop,
		AdventureMainLoop,
		AdventureRelaxedLoop,
		MenuMainLoop,
		
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
		//Old sounds 
		PlayerRevived1,
		ActorSpawnStart,
		ActorSpawnEnd1,
		ProjectileFired,
		ActorDeath,
		ActorHit,
		
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
		
		//Old announcer
		AdventureStart,
		DoubleKill,
		Groovy,
		HastaLaVista,
		KillingSpree,
		LetsGo,
		LetsRock,
		LockAndLoad,
		MultiKill,
		OhYeah,
		PentaKill,
		Perfect,
		Victory,
		WelcomeToTheWasteland,
		YouTasteDeath,
		//Recently added old announcers
		DamnYoureGood,
		Faultless,
		FlawlessVictory,
		GetThisPartyStarted,
		Headshot,
		MissionStart,
		Perfection, 
		PlayerWasted,
		ShallWePlayAGame,
		TotalVictory,
		Victorious,
		WinnerWinner,
		YouWin,
		SmashingIt,
		GameOver,
		ElClassico,
		// New announcer
		AirDrop2,
		AirDrop4,
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