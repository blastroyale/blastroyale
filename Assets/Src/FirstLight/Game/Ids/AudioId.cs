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
		MusicBrFinalDuelLoop,
		MusicDmLoop,
		MusicDmFinalDuelLoop,
		MusicVictoryJingle,
		MusicDefeatJingle,
		MusicPostMatchLoop,
		MusicFinalDuelTransitionJingleBr,

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
		MusicFinalDuelTransitionJingleDm,
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