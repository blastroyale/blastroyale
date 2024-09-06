using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Constants static class for game const value types 
	/// </summary>
	public static class GameConstants
	{
		public static class Scenes
		{
			public const string SCENE_MAIN_MENU = "MainMenu";
		}

		public static class Links
		{
			public const string FEEDBACK_FORM = "https://forms.gle/2V4ffFNmRgoVAba89";
			public const string ZENDESK_SUPPORT_FORM = "https://firstlightgames.zendesk.com/hc/en-gb/requests/new";

			public const string DISCORD_SERVER = "https://discord.gg/blastroyale";
			public const string YOUTUBE_LINK = "https://www.youtube.com/@BlastRoyaleGame/?sub_confirmation=1";
			public const string INSTAGRAM_LINK = "https://www.instagram.com/blastroyale";
			public const string TIKTOK_LINK = "https://www.tiktok.com/@blastroyale";

			public const string APP_STORE_IOS = "https://apps.apple.com/app/blast-royale/id1621071488";
			public const string APP_STORE_GOOGLE_PLAY = "https://play.google.com/store/apps/details?id=com.firstlightgames.blastroyale";
		}

		public static class Balance
		{
			public const float MAP_ROTATION_TIME_MINUTES = 10;
			public const float MAP_DROPZONE_POS_RADIUS_PERCENT = 0.2f;
		}

		public static class Stats
		{
			//Player Current Currency Statistics
			public const string COINS_TOTAL = "Coins Total";
			public const string NOOB_TOTAL = "NOOB Total";
			
			//Player Persistent General Statistics
			public const string ITEMS_OBTAINED = "Items Obtained";
			public const string KD_RATIO = "K/D Ratio";
			public const string WL_RATIO = "W/L Ratio";
			public const string FAME = "Fame";
			
			//General Season Statistics
			public const string COINS_EARNED = "Coins Earned";
			public const string SEASON_XP_EARNED = "XP Earned";
			public const string SEASON_BPP_EARNED = "BPP Earned";
			public const string SEASON_BP_LEVEL = "BP Level";
			public const string SEASON_KD_RATIO = "Season K/D Ratio";
			public const string SEASON_WL_RATIO = "Season W/L Ratio";
			
			// Metrics that should not be used in leaderboards seasons as they should never be reset
			//General Ranked Persistent Statistics
			public const string RANKED_DAMAGE_DONE_EVER = "Ranked Damage Done Ever";
			public const string RANKED_DEATHS_EVER = "Ranked Deaths Ever";
			public const string RANKED_GAMES_PLAYED_EVER = "Ranked Games Played Ever";
			public const string RANKED_GAMES_WON_EVER = "Ranked Games Won Ever";
			public const string RANKED_KILLS_EVER = "Ranked Kills Ever";
			public const string RANKED_AIRDROP_OPENED_EVER = "Ranked Airdrops Opened Ever";
			public const string RANKED_SUPPLY_CRATES_OPENED_EVER = "Ranked Supply Crates Opened Ever";
			public const string RANKED_GUNS_COLLECTED_EVER = "Ranked Guns Collected Ever";
			public const string RANKED_PICKUPS_COLLECTED_EVER = "Ranked Pickups Collected Ever";
			
			//General Ranked InGame Season Statistics
			public const string RANKED_DAMAGE_DONE = "Ranked Damage Done";
			public const string RANKED_DEATHS = "Ranked Deaths";
			public const string RANKED_GAMES_PLAYED = "Ranked Games Played";
			public const string RANKED_GAMES_WON = "Ranked Games Won";
			public const string RANKED_KILLS = "Ranked Kills";
			public const string RANKED_AIRDROP_OPENED = "Ranked Airdrops Opened";
			public const string RANKED_SUPPLY_CRATES_OPENED = "Ranked Supply Crates Opened";
			public const string RANKED_GUNS_COLLECTED = "Ranked Guns Collected";
			public const string RANKED_PICKUPS_COLLECTED = "Ranked Pickups Collected";
			public const string RANKED_LEADERBOARD_LADDER_NAME = "Trophies Ladder";
			
			//Solo Ranked Season statistics name
			public const string SOLO_RANKED_DAMAGE_DONE = "Solo Ranked Damage Done";
			public const string SOLO_RANKED_DEATHS = "Solo Ranked Deaths";
			public const string SOLO_RANKED_GAMES_PLAYED = "Solo Ranked Games Played";
			public const string SOLO_RANKED_GAMES_WON = "Solo Ranked Games Won";
			public const string SOLO_RANKED_KILLS = "Solo Ranked Kills";
			public const string SOLO_RANKED_AIRDROP_OPENED = "Solo Ranked Airdrops Opened";
			public const string SOLO_RANKED_SUPPLY_CRATES_OPENED = "Solo Ranked Supply Crates Opened";
			public const string SOLO_RANKED_GUNS_COLLECTED = "Solo Ranked Guns Collected";
			public const string SOLO_RANKED_PICKUPS_COLLECTED = "Solo Ranked Pickups Collected";
			public const string SOLO_LEADERBOARD_LADDER_NAME = "Solo Trophies Ladder";
			
			//Duo Ranked Season statistics name
			public const string DUO_RANKED_DAMAGE_DONE = "Duo Ranked Damage Done";
			public const string DUO_RANKED_DEATHS = "Duo Ranked Deaths";
			public const string DUO_RANKED_GAMES_PLAYED = "Duo Ranked Games Played";
			public const string DUO_RANKED_GAMES_WON = "Duo Ranked Games Won";
			public const string DUO_RANKED_KILLS = "Duo Ranked Kills";
			public const string DUO_RANKED_AIRDROP_OPENED = "Duo Ranked Airdrops Opened";
			public const string DUO_RANKED_SUPPLY_CRATES_OPENED = "Duo Ranked Supply Crates Opened";
			public const string DUO_RANKED_GUNS_COLLECTED = "Duo Ranked Guns Collected";
			public const string DUO_RANKED_PICKUPS_COLLECTED = "Duo Ranked Pickups Collected";
			public const string DUO_LEADERBOARD_LADDER_NAME = "Duo Trophies Ladder";
			
			//Quad  Ranked Season statistics name
			public const string QUAD_RANKED_DAMAGE_DONE = "Quad Ranked Damage Done";
			public const string QUAD_RANKED_DEATHS = "Quad Ranked Deaths";
			public const string QUAD_RANKED_GAMES_PLAYED = "Quad Ranked Games Played";
			public const string QUAD_RANKED_GAMES_WON = "Quad Ranked Games Won";
			public const string QUAD_RANKED_KILLS = "Quad Ranked Kills";
			public const string QUAD_RANKED_AIRDROP_OPENED = "Quad Ranked Airdrops Opened";
			public const string QUAD_RANKED_SUPPLY_CRATES_OPENED = "Quad Ranked Supply Crates Opened";
			public const string QUAD_RANKED_GUNS_COLLECTED = "Quad Ranked Guns Collected";
			public const string QUAD_RANKED_PICKUPS_COLLECTED = "Quad Ranked Pickups Collected";
			public const string QUAD_LEADERBOARD_LADDER_NAME = "Quad Trophies Ladder";
			
		}

		public static class Audio
		{
			public const string MIXER_MAIN_SNAPSHOT_ID = "Main";
			public const string MIXER_LOBBY_SNAPSHOT_ID = "Lobby";
			public const string MIXER_INDOOR_SNAPSHOT_ID = "Indoor";

			public const string MIXER_GROUP_MASTER_ID = "Master";
			public const string MIXER_GROUP_MUSIC_ID = "Music";
			public const string MIXER_GROUP_SFX_2D_ID = "Sfx2d";
			public const string MIXER_GROUP_SFX_3D_ID = "Sfx3d";
			public const string MIXER_GROUP_DIALOGUE_ID = "Dialogue";
			public const string MIXER_GROUP_AMBIENT_ID = "Ambient";

			public const int SOUND_QUEUE_BREAK_MS = 75;
			public const float SPATIAL_3D_THRESHOLD = 0.1f;

			public const float MIXER_OCCLUSION_TRANSITION_SECONDS = 0.25f;
			public const float MIXER_MUSIC_TRANSITION_SECONDS = 0.5f;

			public const float SFX_2D_SPATIAL_BLEND = 0f;
			public const float SFX_3D_SPATIAL_BLEND = 1f;

			public const float SFX_3D_MIN_DISTANCE = 0.5f;
			public const float SFX_3D_MAX_DISTANCE = 32f;

			public const float MUSIC_REGULAR_FADE_SECONDS = 2.5f;
			public const float MUSIC_SHORT_FADE_SECONDS = 1.5f;

			public const float AMBIENCE_FADE_SECONDS = 0.75f;

			public const float BR_LOW_PHASE_SECONDS_THRESHOLD = 8f;
			public const float BR_MID_PHASE_SECONDS_THRESHOLD = 90f;
			public const float BR_HIGH_PHASE_SECONDS_THRESHOLD = 180f;

			public const int DM_HIGH_PHASE_KILLS_LEFT_THRESHOLD = 3;
			public const int BR_HIGH_PHASE_PLAYERS_LEFT_THRESHOLD = 2;
			public const float HIGH_LOOP_TRANSITION_DELAY = 2f;

			public const float LOW_HP_CLUTCH_THERSHOLD_PERCENT = 0.1f;
			public const int VO_SFX_SINGLE_KILL_PREVENTION_SECONDS = 12;
			public const int VO_SFX_LEADERBOARD_PREVENTION_SECONDS = 3;
		}

		public static class Screenshake
		{
			public const float SCREENSHAKE_DISSAPATION_DISTANCE_MIN = 1;
			public const float SCREENSHAKE_DISSAPATION_DISTANCE_MAX = 15;

			public const float SCREENSHAKE_SMALL_SHOT_STRENGTH = 0.08f;
			public const float SCREENSHAKE_SMALL_SHOT_DURATION = 0.06f;

			public const float SCREENSHAKE_SHOT_STRENGTH = 0.12f;
			public const float SCREENSHAKE_SHOT_DURATION = 0.08f;

			public const float SCREENSHAKE_SMALL_STRENGTH = 0.25f;
			public const float SCREENSHAKE_SMALL_DURATION = 0.2f;

			public const float SCREENSHAKE_MEDIUM_STRENGTH = 0.4f;
			public const float SCREENSHAKE_MEDIUM_DURATION = 0.4f;

			public const float SCREENSHAKE_LARGE_STRENGTH = 0.6f;
			public const float SCREENSHAKE_LARGE_DURATION = 0.8f;
		}

		public static class PlayerName
		{
			public const int PLAYER_NAME_MIN_LENGTH = 5;
			public const int PLAYER_NAME_MAX_LENGTH = 12;
			public const string DEFAULT_PLAYER_NAME = "Player Name";
			public static readonly Color GOLD_COLOR = new (247 / 255f, 198 / 255f, 46 / 255f);
			public static readonly Color SILVER_COLOR = new (247 / 255f, 198 / 255f, 46 / 255f);
			public static readonly Color BRONZE_COLOR = new (247 / 255f, 198 / 255f, 46 / 255f);
			public static readonly Color DEFAULT_COLOR = new (233 / 255f, 226 / 255f, 225 / 255f);
		}

		public static class Data
		{
			public const int MAX_SQUAD_MEMBERS = 3;
			public const int MATCH_SPECTATOR_SPOTS = 15; 
			public const float ROOM_SELECT_DROP_POSITION_SECONDS = 5f;
			public const float SPECTATOR_TOGGLE_TIMEOUT = 2f;
			public const float SERVER_SELECT_CONNECTION_TIMEOUT = 8f;
			public const int PLAYER_NAME_APPENDED_NUMBERS = 5;
			public const uint PLAYER_FAME_MAX_LEVEL = 999;

			// TODO: Move leaderboard entries to configs
			public const short LEADERBOARD_GOLD_ENTRIES = 10;
			public const short LEADERBOARD_SILVER_ENTRIES = 20;
			public const short LEADERBOARD_BRONZE_ENTRIES = 50;

			public static List<GameId> AllowedGameRewards = new List<GameId>()
			{
				GameId.XP,
				GameId.CS,
				GameId.BPP,
				GameId.Trophies,
				GameId.NOOB,
				GameId.COIN,
			};
		}

		public static class PlayFab
		{
			public const string VERSION_KEY = nameof(Application.version);
			public const string MAINTENANCE_KEY = "version block";
		}

		public static class Network
		{
			// Network state time settings
			public const float NETWORK_ATTEMPT_RECONNECT_SECONDS = 1;

			public const float CRITICAL_DISCONNECT_THRESHOLD_SECONDS = 10f;

			// Time control values
			public const int PLAYER_GAME_TTL_MS = 99999999;
#if UNITY_EDITOR
			public const int EMPTY_ROOM_GAME_TTL_MS = 1000 * 60 * 1; // 1 minute
#else
			public const int EMPTY_ROOM_GAME_TTL_MS = 1000 * 60 * 5; // 5 minutes

#endif
			public const int TIMEOUT_SNAPSHOT_SECONDS = EMPTY_ROOM_GAME_TTL_MS / 1000;


			// Room properties
			public const string ROOM_NAME_PLAYTEST = "PLAYTEST";

			public const string DEFAULT_REGION = "eu";

			public const char ROOM_META_SEPARATOR = '#';

			public const string MANUAL_TEAM_ID_PREFIX = "manual_";
		}

		public static class Visuals
		{
			public const float RESOURCE_POOL_UPDATE_TIME_SECONDS = 3;
			public const float STAR_STATUS_CHARACTER_SCALE_MULTIPLIER = 1.5f;
			public const float RADIAL_LOCAL_POS_OFFSET = 0.1f;
			public const float TEAMMATE_BORDER_RADIUS = 6f;
			public static readonly Color HIT_COLOR = new Color(0x7B / 255f, 0x7B / 255f, 0x7B / 255f);

			// The name of the parameter in the animator that decides the time of stun outro animation
			public const string STUN_OUTRO_TIME_ANIMATOR_PARAM = "stun_outro_time_sec";

			// This conversion is the "true" one, works for plain circles with no decor, like weapon aiming range circle
			public const float RADIUS_TO_SCALE_CONVERSION_VALUE = 2f;

			// This conversion is manually chosen based on the visual of special/danger indicators that have decorative elements
			public const float RADIUS_TO_SCALE_CONVERSION_VALUE_NON_PLAIN_INDICATORS = 2.2f;
			public const long GAMEPLAY_BUSH_ATTACK_REVEAL_SECONDS = 2;
			public const long GAMEPLAY_POST_ATTACK_HEALTHBAR_HIDE_DURATION = 2000;
			public const float CHEST_CONSUMABLE_POPOUT_HEIGHT = 2f;
		}

		public static class Tutorial
		{
			public const string FIRST_TUTORIAL_GAME_MODE_ID = "Tutorial";
			public const string SECOND_BOT_MODE_ID = "BattleRoyale First Game";
			public const int SECONDS_TO_START_SECOND_MATCH = 15;

			public const int TIME_250MS = 250;
			public const int TIME_500MS = 500;
			public const int TIME_750MS = 750;
			public const int TIME_1000MS = 1000;

			public const int TIME_4000MS = 4000;

			public const int TIME_HIGHLIGHT_FADE = 450;

			public const string TAG_INDICATORS = "GroundIndicator";
			public const string TAG_GUIDE_UI = "GuideUiTarget";

			public const string TRIGGER_FIRST_MOVE_AREA = "FirstMoveArea";
			public const string TRIGGER_DUMMY_AREA = "DummyArea";
			public const string TRIGGER_CHEST_AREA = "ChestArea";
			public const string TRIGGER_GATE_AREA = "GateArea";
			public const string TRIGGER_ARENA_AREA = "ArenaArea";

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
			public const string INDICATOR_SPECIAL_PICKUP = "SpecialSpawner";
			public const string INDICATOR_ARENA_DROPDOWN = "ArenaDropDown";
		}

		public static class GameModeId
		{
			public static string FAKEGAMEMODE_CUSTOMGAME = "Custom Game";
			public static string TESTING = "Testing";
		}
	}
}