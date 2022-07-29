using System.Collections.Generic;

namespace FirstLight.Game.Ids
{
	public enum AudioId
	{
		None, 
		
		//Music
		MusicMainMenuStart,
		MusicMainMenuLoop,
		MusicBrSkydiveLoop,
		MusicBrLowLoop,
		MusicBrMidLoop,
		MusicBrFinalDuelLoop,
		MusicDmLoop,
		MusicDmFinalDuelLoop,
		MusicVictoryJingle,
		MusicDefeatJingle,
		MusicPostMatchLoop,
		MusicFinalDuelTransitionJingle,

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
		
		// Ambients
		ForestAmbientLoop,
		DesertAmbientLoop,
		CentralAmbientLoop,
		UrbanAmbientLoop,
		FrostAmbientLoop,
		LavaAmbientLoop,
		WaterAmbientLoop,
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