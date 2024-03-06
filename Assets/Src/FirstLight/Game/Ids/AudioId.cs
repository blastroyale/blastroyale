using System.Collections.Generic;

namespace FirstLight.Game.Ids
{
	public enum AudioId
	{
		None, 
		
		//Music
		MusicMainStart,
		MusicMainLoop,
		MusicBrSkydiveLoop,
		MusicBrLowLoop,
		MusicBrMidLoop,
		MusicBrHighLoop,
		MusicDmLoop,
		MusicDmHighLoop,
		MusicVictoryJingle,
		MusicDefeatJingle,
		MusicPostMatchLoop,
		MusicHighTransitionJingleBr,

		// Weapons
		PlaHammerWeaponShot,
		PlaCrossbowWeaponShot,
		PlaShotgunWeaponShot,
		PlaMachineGunWeaponShot,
		PlaRifleWeaponShot,
		PlaSniperWeaponShot,
		PlaRocketLauncherWeaponShot,
		PlaRocketLauncherFlyingTrailLoop,
		PlaMiniGunRevUp,
		PlaMiniGunWeaponShot,
		MmsPistolWeaponShot,
		MmsShotgunWeaponShot,
		MmsMachineGunWeaponShot,
		MmsRifleWeaponShot,
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
		EdiElectronThrowerWeaponShot,
		EdiElectronThrowerWeaponStreamLoop,

		// Weapon Misc
		ReloadPlaLoop,
		ReloadMmsLoop,
		ReloadEdiLoop,
		AmmoEmpty,
		
		// Combat
		TakeHealthDamage,
		TakeShieldDamage,
		HitHealthDamage,
		HitShieldDamage,
		ShieldBreak,
	
		PlayerKillLevel1,
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
		DamageAbsorb,
		SdLoop,
		SdStart,
		SdEnd,

		// Gameplay
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
		ChestPickup,
		PlayerHeartBeatLoop,
		PlayerEnterLava,
		PlayerEnterWater,
		PlayerWalkGrass,
		PlayerWalkRoad,
		PlayerWalkTiles,
		PlayerWalkSand,
		PlayerWalkWater,
		PlayerWalkLava,
		PlayerRespawnShine,
		PlayerRespawnLightningBolt,
		
		// UI
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
		
		// Ambience
		ForestAmbientLoop,
		DesertAmbientLoop,
		CentralAmbientLoop,
		UrbanAmbientLoop,
		FrostAmbientLoop,
		LavaAmbientLoop,
		WaterAmbientLoop,
		
		// Late additions
		MusicHighTransitionJingleDm,
		SelfShieldBreak,
		AirdropDropped,
		AirdropLanded,
		AirdropFlare,
		CollectionLoop,
		CollectionStart,
		CollectionStop,
		
		// Announcer
		Vo_GameStart,
		Vo_GameOver,
		Vo_Victory,
		Vo_PerfectVictory,
		Vo_OnDeath,
		Vo_Kills1,
		Vo_Kills2,
		Vo_Kills3,
		Vo_Kills4,
		Vo_Kills5,
		Vo_Kills2Special,
		Vo_KillsLeft3,
		Vo_KillsLeft1,
		Vo_KillStreak3,
		Vo_KillStreak5,
		Vo_KillStreak7,
		Vo_KillStreak9,
		Vo_KillLowHp,
		Vo_Alive10,
		Vo_Alive2,
		Vo_CircleClose,
		Vo_CircleLastClose,
		Deprecated1,
		Deprecated2,
		Vo_AirdropComing,
		Vo_AirdropLanded,
		Vo_LeaderboardTop,
		Vo_LeaderboardOvertaken,
		
		// Late additions
		PlayerKillLevel2,
		PlayerKillLevel3,
		PlayerKillLevel4,
		PlayerKillLevel5,
		PlayerKillLevel6,
		Vo_CircleLastCountdown,
		Vo_Countdown10,
		Vo_Countdown9,
		Vo_Countdown8,
		Vo_Countdown7,
		Vo_Countdown6,
		Vo_Countdown5,
		Vo_Countdown4,
		Vo_Countdown3,
		Vo_Countdown2,
		Vo_Countdown1,
		Vo_CountdownGo,
		Shells,
		
		RocketExplosion,
		AirdropComing,
		GoToSafeZone,
		CircleIsClosing,
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