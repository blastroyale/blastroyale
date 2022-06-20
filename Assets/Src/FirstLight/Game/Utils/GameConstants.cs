using Quantum;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Constants static class for game const value types 
	/// </summary>
	public static class GameConstants
	{
		public static class Editor
		{
			public const string PREFS_USE_LOCAL_SERVER_KEY = "UseLocalServer";
		}

		public static class Links
		{
			public const string FEEDBACK_FORM = "https://forms.gle/2V4ffFNmRgoVAba89";
			public const string DISCORD_SERVER = "https://discord.gg/blastroyale";
			public const string APP_STORE_IOS = "https://apps.apple.com/gb/app/boss-hunt-heroes/id1557220333";
			public const string APP_STORE_GOOGLE_PLAY = "https://play.google.com/store/apps/details?id=com.firstlightgames.phoenix";
			public const string MARKETPLACE_DEV_URL = "http://flgmarketplacestorage.z33.web.core.windows.net";
			public const string MARKETPLACE_PROD_URL = "https://marketplace.blastroyale.com";
		}

		public static class Balance
		{
			public const float MAP_ROTATION_TIME_MINUTES = 10;
			public const int NFT_AMOUNT_FOR_PLAY = 3;
		}

		public static class Quality
		{
			// Resolution 
			public const float DYNAMIC_RES_HIGH = 1f;
			public const float DYNAMIC_RES_LOW = 0.55f;
		}

		public static class Audio
		{
			// The audios default starting volume
			public const float SFX_2D_DEFFAULT_VOLUME = 0.2f;
			public const float SFX_3D_DEFAULT_VOLUME = 0.4f;
			public const float BGM_DEFAULT_VOLUME = 0.45f;
		}

		public static class Notifications
		{
			public const string NOTIFICATION_IDLE_BOXES_CHANNEL = "idle_boxes";
			public const string NOTIFICATION_BOXES_CHANNEL = "loot_boxes";
		}

		public static class Data
		{
			public const string GAME_HAS_BOTS = "GameHasBots";

			public const int PLAYER_NAME_MIN_LENGTH = 3;
			public const int PLAYER_NAME_MAX_LENGTH = 20;
		}

		public static class Network
		{
			public const int DEFAULT_PLAYER_TTL_MS = 60000;
			public const int EMPTY_ROOM_TTL_MS = 20000;
			public const string PLAYER_PROPS_PRELOAD_IDS = "preloadIds";
			public const string PLAYER_PROPS_LOADED = "propsLoaded";
			public const string ROOM_PROPS_START_TIME = "startTime";
			public const string ROOM_PROPS_COMMIT = "commit";
			public const string ROOM_PROPS_MAP = "mapId";
		}

		public static class Visuals
		{
			public const float DISSOLVE_DURATION = 1.15f;
			public const float DISSOLVE_DELAY = 2.5f;
			public const float HIT_DURATION = 0.5f;
			public const float DISSOLVE_END_ALPHA_CLIP_VALUE = 0.75f;
			public const float STAR_STATUS_CHARACTER_SCALE_MULTIPLIER = 1.5f;
			public const float RADIAL_LOCAL_POS_OFFSET = 0.1f;

			// Description post fix string tag
			public const string DESCRIPTION_POSTFIX = "Description";

			// Multiplier to convert Movement Speed values into more readable
			public const float MOVEMENT_SPEED_BEAUTIFIER = 100f;

			// Maximum player rag-doll impulse force amount 
			public const float PLAYER_RAGDOLL_FORCE_MAX = 1f;

			// Minimum player rag-doll impulse force amount 
			public const float PLAYER_RAGDOLL_FORCE_MIN = 0.25f;

			// The name of the parameter in the animator that decides the time of stun outro animation
			public const string STUN_OUTRO_TIME_ANIMATOR_PARAM = "stun_outro_time_sec";

			public const float RADIUS_TO_SCALE_CONVERSION_VALUE = 2f;
		}

		public static class Haptics
		{
			// Platform dependent intensity as the vibrations vary greatly between android/iOS
#if UNITY_ANDROID
			public const float DAMAGE_INTENSITY_MIN = 0.1f;
			public const float DAMAGE_INTENSITY_MAX = 0.8f;
#else
		public const float DAMAGE_INTENSITY_MIN = 0.3f;
		public const float DAMAGE_INTENSITY_MAX = 1f;
#endif

			// Min/max amounts of haptic vibration sharpness when a player is damaged
			public const float IOS_DAMAGE_SHARPNESS_MIN = 0.3f;
			public const float IOS_DAMAGE_SHARPNESS_MAX = 1f;

			// Duration of haptic feedback when player is damaged
			public const float DAMAGE_DURATION = 0.05f;
		}
	}
}