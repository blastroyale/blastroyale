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
			public const string PREFS_ENABLE_STATE_MACHINE_DEBUG_KEY = "EnableStateMachineDebug";
		}

		public static class Scenes
		{
			public const string SCENE_MAIN_MENU = "MainMenu";
		}

		public static class Links
		{
			public const string FEEDBACK_FORM = "https://forms.gle/2V4ffFNmRgoVAba89";
			public const string DISCORD_SERVER = "https://discord.gg/blastroyale";
			public const string APP_STORE_IOS = "https://apps.apple.com/gb/app/boss-hunt-heroes/id1557220333";
			public const string APP_STORE_GOOGLE_PLAY = "https://play.google.com/store/apps/details?id=com.firstlightgames.phoenix";
			#if LIVE_SERVER
				public const string MARKETPLACE_URL = "https://marketplace.blastroyale.com/";
			#elif STAGE_SERVER
				public const string MARKETPLACE_URL = "https://marketplace-staging.blastroyale.com/";
			#else
				public const string MARKETPLACE_URL = "https://marketplace-dev.blastroyale.com/";
			#endif
		}

		public static class Balance
		{
			public const float MAP_ROTATION_TIME_MINUTES = 10;
		}

		public static class Audio
		{
			public const string MIXER_MAIN_SNAPSHOT_ID = "Main";
			public const string MIXER_LOBBY_SNAPSHOT_ID = "Lobby";
			public const string MIXER_GROUP_MASTER_ID = "Master";
			public const string MIXER_GROUP_MUSIC_ID = "Music";
			public const string MIXER_GROUP_SFX_2D_ID = "Sfx2d";
			public const string MIXER_GROUP_SFX_3D_ID = "Sfx3d";
			public const string MIXER_GROUP_DIALOGUE_ID = "Dialogue";
			public const string MIXER_GROUP_AMBIENT_ID = "Announcer";
			
			public const int SOUND_QUEUE_BREAK_MS = 75;
			public const float SPATIAL_3D_THRESHOLD = 0.1f;
			
			public const float MIXER_SNAPSHOT_TRANSITION_SECONDS = 0.5f;
			
			public const float SFX_2D_SPATIAL_BLEND = 0f;
			public const float SFX_3D_SPATIAL_BLEND = 1f;
			
			public const float SFX_3D_MIN_DISTANCE = 5f;
			public const float SFX_3D_MAX_DISTANCE = 20f;

			public const float MUSIC_REGULAR_FADE_SECONDS = 2.5f;
			public const float MUSIC_SHORT_FADE_SECONDS = 1.5f;
			
			public const float BR_LOW_PHASE_SECONDS_THRESHOLD = 8f;
			public const float BR_MID_PHASE_SECONDS_THRESHOLD = 90f;
			public const float BR_HIGH_PHASE_SECONDS_THRESHOLD = 180f;
			
			public const float DM_HIGH_PHASE_KILLS_LEFT_THRESHOLD = 3;
			public const float BR_HIGH_PHASE_PLAYERS_LEFT_THRESHOLD = 2;
			public const float HIGH_LOOP_TRANSITION_DELAY = 2f;
			
			public const float LOW_HP_CLUTCH_THERSHOLD_PERCENT = 0.1f;
			public const int VO_DUPLICATE_SFX_PREVENTION_SECONDS = 12;
		}

		public static class Notifications
		{
			public const string NOTIFICATION_IDLE_BOXES_CHANNEL = "idle_boxes";
			public const string NOTIFICATION_BOXES_CHANNEL = "loot_boxes";
		}

		public static class PlayerName
		{
			public const int PLAYER_NAME_MIN_LENGTH = 3;
			public const int PLAYER_NAME_MAX_LENGTH = 20;
			public const string DEFAULT_PLAYER_NAME = "Player Name";
		}

		public static class Data
		{
			public const int MATCH_SPECTATOR_SPOTS = 15;
			public const float SPECTATOR_TOGGLE_TIMEOUT = 2f;
			public const float SERVER_SELECT_CONNECTION_TIMEOUT = 8f;
			public const int PLAYER_NAME_APPENDED_NUMBERS = 5;
		}

		public static class Network
		{
			// Time control values
			public const int DEFAULT_PLAYER_TTL_MS = 30000;
			public const int EMPTY_ROOM_TTL_MS = 15000;
			public const int EMPTY_ROOM_PLAYTEST_TTL_MS = 1000;

			// Player properties
			// Loading properties are split into PLAYER_PROPS_CORE_LOADED and PLAYER_PROPS_ALL_LOADED - this is because
			// the loading flow into match is split into 2 distinct phases (Core assets, player assets), and these properties
			// are used to signal at which point in the loading flow the player is currently during matchmaking screen.
			public const string PLAYER_PROPS_CORE_LOADED = "propsCoreLoaded";
			public const string PLAYER_PROPS_ALL_LOADED = "propsAllLoaded";
			public const string PLAYER_PROPS_PRELOAD_IDS = "preloadIds";
			public const string PLAYER_PROPS_SPECTATOR = "isSpectator";

			// Room properties
			public const string ROOM_NAME_PLAYTEST = "PLAYTEST";
			public const string ROOM_PROPS_START_TIME = "startTime";
			public const string ROOM_PROPS_COMMIT = "commit";
			public const string ROOM_PROPS_MAP = "mapId";
			public const string ROOM_PROPS_GAME_MODE = "gameModeId";
			public const string ROOM_PROPS_MUTATORS = "mutators";
			public const string ROOM_PROPS_BOTS = "gameHasBots";
			public const string ROOM_PROPS_DROP_PATTERN = "dropPattern";
			public const string ROOM_PROPS_MATCH_TYPE = "matchType";

			public const string DEFAULT_REGION = "eu";
			
			public const string LEADERBOARD_LADDER_NAME = "Trophies Ladder";
			public const int LEADERBOARD_TOP_RANK_AMOUNT = 20;
			public const int LEADERBOARD_NEIGHBOR_RANK_AMOUNT = 3;
		}

		public static class Visuals
		{
			public const float DISSOLVE_DURATION = 1.15f;
			public const float DISSOLVE_DELAY = 2.5f;
			public const float HIT_DURATION = 0.5f;
			public const float DISSOLVE_END_ALPHA_CLIP_VALUE = 0.75f;
			public const float STAR_STATUS_CHARACTER_SCALE_MULTIPLIER = 1.5f;
			public const float RADIAL_LOCAL_POS_OFFSET = 0.1f;
			public const float NEAR_DEATH_HEALTH_RATIO_THRESHOLD = 0.4f;

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
			public const float GAMEPLAY_POST_ATTACK_HIDE_DURATION = 2f;

			public const string SHADER_MINIMAP_DRAW_PLAYERS = "MINIMAP_DRAW_PLAYERS";
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
			
			// Duration of haptic feedback when player is damaged
			public const float GAME_START_DURATION = 0.25f;
			public const float GAME_START_INTENSITY = 1f;
			public const float GAME_START_SHARPNESS = 1f;
		}
	}
}