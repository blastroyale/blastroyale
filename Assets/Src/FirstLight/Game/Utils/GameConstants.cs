using System;
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
	
		}

		public static class Scenes
		{
			public const string SCENE_MAIN_MENU = "MainMenu";
		}

		public static class Links
		{
			public const string FEEDBACK_FORM = "https://forms.gle/2V4ffFNmRgoVAba89";
			public const string DISCORD_SERVER = "https://discord.gg/blastroyale";
			public const string APP_STORE_IOS = "https://apps.apple.com/app/blast-royale/id1621071488";
			public const string APP_STORE_GOOGLE_PLAY = "https://play.google.com/store/apps/details?id=com.firstlightgames.blastroyale";
			#if LIVE_SERVER
				public const string MARKETPLACE_URL = "https://marketplace.blastroyale.com/";
			#elif STAGE_SERVER
				public const string MARKETPLACE_URL = "https://marketplace-staging.blastroyale.com/";
			#else
				public const string MARKETPLACE_URL = "https://marketplace-dev.blastroyale.com/";
			#endif
		}

		public static class Servers
		{
			public enum ServerEnvironments
			{
				Dev,
				Stage,
				Live,
				LiveTesnet
			}
			
#if LIVE_SERVER
			public const ServerEnvironments SERVER_ENVIRONMENT = ServerEnvironments.Live;
			public const string PLAYFAB_TITLE_ID = "***REMOVED***";
			public const string PASSWORD_RECOVER_EMAIL = "***REMOVED***";
			public const string QUANTUM_ID = "***REMOVED***";
#elif LIVE_TESTNET_SERVER
			public const ServerEnvironments SERVER_ENVIRONMENT = ServerEnvironments.LiveTesnet;
			public const string PLAYFAB_TITLE_ID = "***REMOVED***";
			public const string PASSWORD_RECOVER_EMAIL = "***REMOVED***";
			public const string QUANTUM_ID = "81262db7-24a2-4685-b386-65427c73ce9d";
#elif STAGE_SERVER
			public const ServerEnvironments SERVER_ENVIRONMENT = ServerEnvironments.Stage;
			public const string PLAYFAB_TITLE_ID = "***REMOVED***";
			public const string PASSWORD_RECOVER_EMAIL = "***REMOVED***";
			public const string QUANTUM_ID = "***REMOVED***";
#else
			public const ServerEnvironments SERVER_ENVIRONMENT = ServerEnvironments.Dev;
			public const string PLAYFAB_TITLE_ID = "***REMOVED***";
			public const string PASSWORD_RECOVER_EMAIL = "***REMOVED***";
			public const string QUANTUM_ID = "***REMOVED***";
#endif
			public static readonly string SERVER_ENVIRONMENT_STRING = SERVER_ENVIRONMENT.ToString();
		}

		public static class Balance
		{
			public const float MAP_ROTATION_TIME_MINUTES = 10;
			public const float MAP_DROPZONE_POS_RADIUS_PERCENT = 0.2f;
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
			public const int VO_SFX_SINGLE_KILL_PREVENTION_SECONDS = 12;
			public const int VO_SFX_LEADERBOARD_PREVENTION_SECONDS = 3;
		}

		public static class Screenshake
		{
			public const float SCREENSHAKE_DISSAPATION_DISTANCE_MIN = 1;
			public const float SCREENSHAKE_DISSAPATION_DISTANCE_MAX= 15;

			public const float SCREENSHAKE_SMALL_STRENGTH = 0.25f;
			public const float SCREENSHAKE_SMALL_DURATION = 0.2f;

			public const float SCREENSHAKE_MEDIUM_STRENGTH = 0.4f;
			public const float SCREENSHAKE_MEDIUM_DURATION = 0.4f;

			public const float SCREENSHAKE_LARGE_STRENGTH = 0.6f;
			public const float SCREENSHAKE_LARGE_DURATION = 0.8f;
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
			public const float ROOM_SELECT_DROP_POSITION_SECONDS = 15f;
			public const float SPECTATOR_TOGGLE_TIMEOUT = 2f;
			public const float SERVER_SELECT_CONNECTION_TIMEOUT = 8f;
			public const int PLAYER_NAME_APPENDED_NUMBERS = 5;
		}
		

		public static class PlayFab
		{
			public const string VERSION_KEY = nameof(Application.version);
			public const string MAINTENANCE_KEY = "version block";
		}

		public static class Stats
		{
			public const string GAMES_PLAYED = "Games Played";
			public const string GAMES_WON = "Games Won";
			public const string KILLS = "Kills";
			public const string DEATHS = "Deaths";
			public const string NFT_ITEMS = "Nft Items";
			public const string NON_NFTS = "Non Nft Items";
			public const string BROKEN_ITEMS = "Broken Items";
			public const string LEADERBOARD_LADDER_NAME = "Trophies Ladder";
		}
		
		public static class Network
		{
			// Network state time settings
			public const float NETWORK_ATTEMPT_RECONNECT_SECONDS = 1;
			
			public const float CRITICAL_DISCONNECT_THRESHOLD_SECONDS = 10f;
			
			// Time control values
			public const int PLAYER_GAME_TTL_MS = 99999999;
			public const int EMPTY_ROOM_GAME_TTL_MS = 1000 * 60 * 5; // 5 minutes
			public const int EMPTY_ROOM_PLAYTEST_TTL_MS = 3000; 
			public const int TIMEOUT_SNAPSHOT_SECONDS = EMPTY_ROOM_GAME_TTL_MS / 1000; 

			// Player properties
			// Loading properties are split into PLAYER_PROPS_CORE_LOADED and PLAYER_PROPS_ALL_LOADED - this is because
			// the loading flow into match is split into 2 distinct phases (Core assets, player assets), and these properties
			// are used to signal at which point in the loading flow the player is currently during matchmaking screen.
			public const string PLAYER_PROPS_CORE_LOADED = "propsCoreLoaded";
			public const string PLAYER_PROPS_ALL_LOADED = "propsAllLoaded";
			public const string PLAYER_PROPS_PRELOAD_IDS = "preloadIds";
			public const string PLAYER_PROPS_SPECTATOR = "isSpectator";
			public const string PLAYER_PROPS_TEAM_ID = "teamId";
			public const string PLAYER_PROPS_DROP_POSITION = "dropPosition";

			// Room properties
			public const string ROOM_NAME_PLAYTEST = "PLAYTEST";
			public const string ROOM_PROPS_CREATION_TICKS = "creationTicks";
			public const string ROOM_PROPS_COMMIT = "commit";
			public const string ROOM_PROPS_MAP = "mapId";
			public const string ROOM_PROPS_GAME_MODE = "gameModeId";
			public const string ROOM_PROPS_SETUP = "roomSetup";
			public const string ROOM_PROPS_MUTATORS = "mutators";
			public const string ROOM_PROPS_BOTS = "gameHasBots";
			public const string DROP_ZONE_POS_ROT = "dropzonePosRot";
			public const string ROOM_PROPS_MATCH_TYPE = "matchType";
			public const string ROOM_PROPS_STARTED_GAME = "startedGame";

			public const string DEFAULT_REGION = "eu";
			
			public const char ROOM_META_SEPARATOR = '#';
			
			public const string LEADERBOARD_LADDER_NAME = "Trophies Ladder";
			public const int LEADERBOARD_TOP_RANK_AMOUNT = 100;
			public const int LEADERBOARD_NEIGHBOR_RANK_AMOUNT = 1;
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

			public const int LOW_FPS_MODE_TARGET = 30;
			public const int HIGH_FPS_MODE_TARGET = 60;
			
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

			// This conversion is the "true" one, works for plain circles with no decor, like weapon aiming range circle
			public const float RADIUS_TO_SCALE_CONVERSION_VALUE = 2f;
			// This conversion is manually chosen based on the visual of special/danger indicators that have decorative elements
			public const float RADIUS_TO_SCALE_CONVERSION_VALUE_NON_PLAIN_INDICATORS = 2.2f;
			
			public const float GAMEPLAY_POST_ATTACK_HIDE_DURATION = 2f;

			public const string SHADER_MINIMAP_DRAW_PLAYERS = "MINIMAP_DRAW_PLAYERS";
			
			public const int REWARD_POPUP_CLOSE_MS = 300;
			public const int SCREEN_SWIPE_TRANSITION_MS = 1500;
		}

		public static class Camera
		{
			public const float DYNAMIC_CAMERA_PAN_TO_AIM_TIME = 0.5f;
			public const float DYNAMIC_CAMERA_PAN_TO_CENTER_TIME = 0.25f;
			public const float DYNAMIC_CAMERA_PAN_DISTANCE_DEFAULT = 1.75f;
			public const float DYNAMIC_CAMERA_PAN_NEGATIVE_Y_DIR_MULTIPLIER = 1.3f;
		}

		public static class Controls
		{
			public const float DYNAMIC_JOYSTICK_THRESHOLD_MULT = 1f;
			public const float MOVEMENT_JOYSTICK_RADIUS_MULT = 1f;
			public const float JOYSTICK_MOVEMENT_MAX_RADIUS_MULTIPLIER = 8f;
			
			public const float SPECIAL_BUTTON_MAX_RADIUS_MULT = 1.75f;
			public const float SPECIAL_BUTTON_FIRST_CANCEL_RADIUS_MULT = 1.15f;
			public const float SPECIAL_BUTTON_CANCEL_RADIUS_MULT = 0.75f;
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

			public const float PLAYER_KILL_DURATION = 0.1f;
			public const float PLAYER_KILL_INTENSITY = 1;
			public const float PLAYER_KILL_SHARPNESS = 1;
		}

		public static class Tutorial
		{
			public const string FIRST_TUTORIAL_GAME_MODE_ID = "Tutorial";
			public const string SECOND_BOT_MODE_ID = "BattleRoyale First Game";

			public const int WAIT_TIME_250MS = 250;
			public const int WAIT_TIME_500MS = 500;
			public const int WAIT_TIME_750MS = 750;
			public const int WAIT_TIME_1000MS = 1000;
			public const int WAIT_TIME_1250MS = 1250;
			
			public const string TAG_INDICATORS = "GroundIndicator";
			public const string TAG_GUIDE_UI = "GuideUiTarget";
			
			public const string TRIGGER_FIRST_MOVE_AREA = "FirstMoveArea";
			public const string TRIGGER_DUMMY_AREA = "DummyArea";
			public const string TRIGGER_CHEST_AREA = "ChestArea";
			public const string TRIGGER_GATE_AREA = "GateArea";
			public const string TRIGGER_ARENA_AREA = "ArenaArea";

			public const string GUIDE_UI_MOVEMENT_JOYSTICK = "MovementUiJoystick";
			public const string GUIDE_UI_SHOOTING_JOYSTICK = "ShootingUiJoystick";
			public const string GUIDE_UI_SPECIAL_BUTTON = "TutorialSpecialTarget";
			
			public const string INDICATOR_FIRST_MOVE = "FirstMove";
			public const string INDICATOR_WOODEN_BARRIER = "WoodenBarrier";
			public const string INDICATOR_FIRST_WEAPON = "FirstWeapon";
			public const string INDICATOR_BOT_AREA = "BotArea";
			public const string INDICATOR_BOT1 = "Bot1";
			public const string INDICATOR_BOT2 = "Bot2";
			public const string INDICATOR_BOT3 = "Bot3";
			public const string INDICATOR_IRON_GATE = "IronGate";
			public const string INDICATOR_TOP_PLATFORM = "TopPlatform";
			public const string INDICATOR_EQUIPMENT_CHEST = "EquipmentChest";
			public const string INDICATOR_ARENA_DROPDOWN = "ArenaDropDown";
		}

		public static class GameModeId
		{
			public static string FAKEGAMEMODE_CUSTOMGAME = "Custom Game";
		}
	}
}