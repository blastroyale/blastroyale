using Quantum;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Constants static class for game const value types 
	/// </summary>
	public static class GameConstants
	{
		public const string FEEDBACK_FORM_LINK = "https://forms.gle/2V4ffFNmRgoVAba89";
		public const string DISCORD_SERVER_LINK = "https://discord.gg/MehYAERaVp";
		public const string APP_STORE_IOS_LINK = "https://apps.apple.com/gb/app/boss-hunt-heroes/id1557220333";
		public const string APP_STORE_GOOGLE_PLAY_LINK = "https://play.google.com/store/apps/details?id=com.firstlightgames.phoenix";
		
		// Description post fix string tag
		public const string DESCRIPTION_POSTFIX = "Description";
		
		// Multiplier to convert Movement Speed values into more readable
		public const float MOVEMENT_SPEED_BEAUTIFIER = 100f;
		
		// Maximum player rag-doll impulse force amount 
		public const float PLAYER_RAGDOLL_FORCE_MAX = 1f;

		// Minimum player rag-doll impulse force amount 
		public const float PLAYER_RAGDOLL_FORCE_MIN = 0.25f;
		
		// Platform dependent intensity as the vibrations vary greatly between android/iOS
#if UNITY_ANDROID
		public const float HAPTIC_DAMAGE_INTENSITY_MIN = 0.1f;
		public const float HAPTIC_DAMAGE_INTENSITY_MAX = 0.8f;
#else
		public const float HAPTIC_DAMAGE_INTENSITY_MIN = 0.3f;
		public const float HAPTIC_DAMAGE_INTENSITY_MAX = 1f;
#endif

		// Min/max amounts of haptic vibration sharpness when a player is damaged
		public const float HAPTIC_IOS_DAMAGE_SHARPNESS_MIN = 0.3f;
		public const float HAPTIC_IOS_DAMAGE_SHARPNESS_MAX = 1f;

		// Duration of haptic feedback when player is damaged
		public const float HAPTIC_DAMAGE_DURATION = 0.05f;
		
		// The name of the parameter in the animator that decides the time of stun outro animation
		public const string STUN_OUTRO_TIME_ANIMATOR_PARAM = "stun_outro_time_sec";
		
		// The upper limit for separators on reload bar; if the value is bigger then we don't show separators
		public const uint MAX_RELOAD_BAR_SEPARATORS_AMOUNT = 6;

		public const float RadiusToScaleConversionValue = 2f;
		
		// The audios default starting volume
		public const float Sfx2dDefaultVolume = 0.2f;
		public const float Sfx3dDefaultVolume = 0.4f;
		public const float BgmDefaultVolume = 0.45f;

		public const float ExtraMatchmakingRandomTime = 2f;
		public const float DissolveDuration = 1.15f;
		public const float DissolveDelay = 2.5f;
		public const float HitDuration = 0.5f;
		public const float DissolveEndAlphaClipValue = 0.75f;
		public const float STAR_STATUS_CHARACTER_SCALE_MULTIPLIER = 1.5f;
		public const float RADIAL_LOCAL_POS_OFFSET = 0.1f;
		
		public const int FUSION_SLOT_AMOUNT = 5;

		public const string NotificationIdleBoxesChannel = "idle_boxes";
		public const string NotificationBoxesChannel = "loot_boxes";
		
		public static GameId [] TUTORIAL_VIDEO_IDS = { GameId.SniperRifle };

		public const int FtueAdventuresCount = 1;
		public const int OnboardingAdventuresCount = 0;
		public const int RotationAdventuresCount = 1;
	}
}