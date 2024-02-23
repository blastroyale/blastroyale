using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;

namespace Quantum
{
	[Serializable]
	public class QuantumGameModeConfig
	{
		[PropertyTooltip(DESC_ID)] public string Id;

#region Matchmaking & Room

		[FoldoutGroup("Matchmaking & Room"), PropertyRange(1, 30), ValidateInput("@MaxPlayers >= MinPlayers"),
		 PropertyTooltip(DESC_MAX_PLAYERS)]
		public uint MaxPlayers;

		[FoldoutGroup("Matchmaking & Room"), PropertyRange(1, "MaxPlayers"), ValidateInput("@MaxPlayers >= MinPlayers"),
		 DisableIf("@true"), PropertyTooltip("NOT IMPLEMENTED | " + DESC_MIN_PLAYERS)]
		public uint MinPlayers;

		[ToggleGroup("Matchmaking & Room/Teams"), DisableIf("@true"), PropertyTooltip(DESC_TEAMS)]
		public bool Teams;

		[ToggleGroup("Matchmaking & Room/Teams"), DisableIf("@false"), PropertyTooltip(DESC_MAX_PLAYERS_IN_TEAM)]
		public uint MaxPlayersInTeam;

		[ToggleGroup("Matchmaking & Room/Teams"), DisableIf("@false"), PropertyTooltip(DESC_MIN_PLAYERS_IN_TEAM)]
		public uint MinPlayersInTeam;
		
		[PropertyTooltip(DESC_ALLOWED_MAPS)]
		public List<GameId> AllowedMaps;

		[FoldoutGroup("Ranking"), PropertyTooltip(DESC_RANK_SORTER)]
		public RankSorter RankSorter;

		[FoldoutGroup("Ranking"), PropertyTooltip(DESC_RANK_PROCESSOR)]
		public RankProcessor RankProcessor;

#endregion

#region UI / Visual

		// UI / Visual
		[FoldoutGroup("UI"), PropertyTooltip(DESC_SHOW_MINIMAP)]
		public bool ShowUIMinimap;

		[FoldoutGroup("UI"), PropertyTooltip(DESC_SHOW_TIMER)]
		public bool ShowUITimer;

		[FoldoutGroup("UI"), PropertyTooltip(DESC_SHOW_UI_STANDINGS_EXTRA_INFO)]
		public bool ShowUIStandingsExtraInfo;

		[FoldoutGroup("UI"), PropertyTooltip(DESC_SHOW_WEAPON_SLOTS)]
		public bool ShowWeaponSlots;
		
		[FoldoutGroup("UI"), PropertyTooltip(DESC_SINGLE_SLOT_MODE)]
		public bool SingleSlotMode;
		
		[FoldoutGroup("UI"), PropertyTooltip(DESC_DESCRIPTION_LOCALISATION_KEY)]
		public string DescriptionLocalisationKey;
		
		[FoldoutGroup("UI"), PropertyTooltip(DESC_SHOW_TEAM_COUNT)]
		public bool ShowTeamCount;

#endregion

#region Player

		[FoldoutGroup("Player"), PropertyTooltip(DESC_LIVES),
		 InfoBox("Currently this only works for 1, every other value means infinite lives.", InfoMessageType.Warning)]
		public uint Lives;

		[FoldoutGroup("Player"), PropertyTooltip(DESC_DROP_WEAPON_ON_PICKUP)]
		public bool DropWeaponOnPickup;

		[FoldoutGroup("Player"), PropertyTooltip(DESC_AIRDROP_NEAR_PLAYER)]
		public bool AirdropNearPlayer;
		
		[FoldoutGroup("Player"), PropertyTooltip(DESC_SHRINKING_CIRCLE_CENTERED_ON_PLAYER)]
		public bool ShrinkingCircleCenteredOnPlayer;
		
		[HorizontalGroup("Player/H1"), BoxGroup("Player/H1/Spawning"), PropertyTooltip(DESC_SPAWN_WITH_GEAR)]
		public bool SpawnWithGear;

		[HorizontalGroup("Player/H1"), BoxGroup("Player/H1/Spawning"), PropertyTooltip(DESC_SPAWN_WITH_WEAPON)]
		public bool SpawnWithWeapon;

		[BoxGroup("Player/H1/Spawning"), PropertyTooltip(DESC_SKYDIVE_SPAWN)]
		public bool SkydiveSpawn;

		[BoxGroup("Player/H1/Spawning"), PropertyTooltip(DESC_SPAWN_SELECTION)]
		public bool SpawnSelection;

		[BoxGroup("Player/H1/Spawning"), ShowIf("SpawnSelection"), PropertyTooltip(DESC_SPAWN_PATTERN)]
		public bool SpawnPattern;

		[BoxGroup("Player/H1/Death drops"), PropertyTooltip(DESC_DEATH_DROP_STRATEGY)]
		public DeathDropsStrategy DeathDropStrategy;

		[BoxGroup("Player/H1/Death drops"), PropertyTooltip(DESC_DEATH_MARKER)]
		public bool DeathMarker;
		
		[FoldoutGroup("Player"), PropertyTooltip(DESC_MINIMUM_HEALTH),
		 InfoBox("If more than zero the player will never die. Only works with real players, not bots.", InfoMessageType.Warning)]
		public uint MinimumHealth;
        
		/// <summary>
		/// If this gamemode skips the pre game loading screen with the plane and maps election
		/// </summary>
		public bool InstantLoad;

#endregion

#region Bots

		[FoldoutGroup("Bots"), PropertyTooltip(DESC_ALLOW_BOTS)]
		public bool AllowBots;
		
		[FoldoutGroup("Bots"), ShowIf("AllowBots"), PropertyTooltip(DESC_BOT_USE_ANOTHER_MODE_BOTS)]
		public string UseBotsFromGamemode;

		[FoldoutGroup("Bots"), ShowIf("AllowBots"), PropertyTooltip(DESC_BOT_SEARCH_FOR_CRATES)]
		public bool BotSearchForCrates;

		[FoldoutGroup("Bots"), ShowIf("AllowBots"), PropertyTooltip(DESC_BOT_RESPAWN)]
		public bool BotRespawn;

		[FoldoutGroup("Bots"), ShowIf("AllowBots"), PropertyTooltip(DESC_BOT_WEAPON_SEARCH_STRATEGY)]
		public BotWeaponSearchStrategy BotWeaponSearchStrategy;

		[FoldoutGroup("Bots"), ShowIf("AllowBots"), PropertyTooltip(DESC_BOT_TEAM_OVERRIDE)]
		public int BotsTeamOverride;

#endregion

#region State Machines

		[FoldoutGroup("State Machines"), PropertyTooltip(DESC_GAME_SIMULATION_SM)] public GameSimulationStateMachine GameSimulationStateMachine;
		[FoldoutGroup("State Machines"), PropertyTooltip(DESC_AUDIO_SM)] public AudioStateMachine AudioStateMachine;

#endregion

#region Endgame

		[FoldoutGroup("Endgame"), PropertyTooltip(DESC_COMPLETION_STRATEGY)]
		public GameCompletionStrategy CompletionStrategy;

		[FoldoutGroup("Endgame"), ShowIf("@CompletionStrategy == GameCompletionStrategy.KillCount"), 
		 PropertyTooltip(DESC_COMPLETION_KILL_COUNT)]
		public uint CompletionKillCount;
		
		[FoldoutGroup("Endgame"), PropertyTooltip(DESC_ALLOW_EARLY_REWARDS)]
		public bool AllowEarlyRewards;
		
		[FoldoutGroup("Endgame"), PropertyTooltip(DESC_COINS_MULT)]
		public ushort MetaCoinsMultiplier = 1;
		
		[FoldoutGroup("Endgame"), PropertyTooltip(DESC_BPP_MULT)]
		public ushort MetaBppMultiplier = 1;

#endregion

#region Debug

		[FoldoutGroup("Debug"), PropertyTooltip(DESC_IS_DEBUG_ONLY)]
		public bool IsDebugOnly;

#endregion

		[ValueDropdown("GetOptionalSystems"), ListDrawerSettings(Expanded = true), PropertyTooltip(DESC_SYSTEMS)]
		public List<string> Systems;

		#region Odin Helpers

		private IEnumerable<string> GetOptionalSystems()
		{
			return from assembly in AppDomain.CurrentDomain.GetAssemblies()
			       from type in assembly.GetTypes()
			       where type.IsDefined(typeof(OptionalSystemAttribute))
			       select type.AssemblyQualifiedName;
		}

#endregion

#region Tooltips

		// @formatter:off
		private const string DESC_ID = "A UNIQUE ID that identifies this game mode.";
		private const string DESC_MAX_PLAYERS = "The maximum number of players that can join a room.";
		private const string DESC_MIN_PLAYERS = "The minimum number of players that can join a room.";
		private const string DESC_TEAMS = "Whether or not this game mode supports teams.";
		private const string DESC_MAX_PLAYERS_IN_TEAM = "What is the maxumum number of players in a team.";
		private const string DESC_MIN_PLAYERS_IN_TEAM = "What is the minimum number of players in a team.";
		private const string DESC_SHOW_STAT_CHANGES = "Displays floating texts above the player when their stats change (e.g. on weapon pickup)";
		private const string DESC_SHOW_MINIMAP = "Displays the Minimap";
		private const string DESC_SHOW_TIMER = "Displays the countdown timer at the top center of the screen, along with other BR specific UIs. Requires ShrinkingCircle system to be enabled.";
		private const string DESC_SHOW_UI_STANDINGS_EXTRA_INFO = "Displays additional information on the standings / leaderboards, like XP and Trophy count.";
		private const string DESC_SHOW_WEAPON_SLOTS = "Displays weapon slots and enables the player to switch between weapons.";
		private const string DESC_SINGLE_SLOT_MODE = "Only shows the melee slot and the first weapon slot";
		private const string DESC_DESCRIPTION_LOCALISATION_KEY = "Localisation key for the description of this game mode";
		private const string DESC_SHOW_TEAM_COUNT = "Displays the number of teams lef";
		private const string DESC_LIVES = "How many lives does the player have. Use 0 for infinite lives";
		private const string DESC_MINIMUM_HEALTH = "Health will not fall below this value. Not for bots.";
		private const string DESC_DROP_WEAPON_ON_PICKUP = "Drops the player's equipped weapon if they pick up a better one.";
		private const string DESC_AIRDROP_NEAR_PLAYER = "Airdrop will spawn near player.";
		private const string DESC_SHRINKING_CIRCLE_CENTERED_ON_PLAYER = "Shrinking circle center is set on player.";
		private const string DESC_SPAWN_WITH_GEAR = "Spawns the player with their loadout gear equipped.";
		private const string DESC_SPAWN_WITH_WEAPON = "Spawns the player with their loadout weapon equipped.";
		private const string DESC_SKYDIVE_SPAWN = "Drops the player from a height when spawning.";
		private const string DESC_SPAWN_SELECTION = "Enables the player to select a spawn position on the map.";
		private const string DESC_SPAWN_PATTERN = "Limits spawn selection to a path on the map.";
		private const string DESC_DEATH_DROP_STRATEGY = "Which strategy of drop we use when the player dies.";
		private const string DESC_DEATH_MARKER = "If we should spawn a death marker on the position where a player died.";
		private const string DESC_ENABLE_BOX_RARITY_MODIFIERS = "If we change boxes rarities based on ChestTypeModifiers inside Chest config";
		private const string DESC_BOT_SEARCH_FOR_CRATES = "Should the bots search / look for crates.";
		private const string DESC_BOT_USE_ANOTHER_MODE_BOTS = "Use bot configs from another gamemode";
		private const string DESC_ALLOW_BOTS = "If bots can be enabled for this game mode.";
		private const string DESC_BOT_RESPAWN = "Allows bots to respawn when they get killed.";
		private const string DESC_BOT_WEAPON_SEARCH_STRATEGY = "How should bots search for weapons on the map.";
		private const string DESC_BOT_TEAM_OVERRIDE = "If >0 it will override all the bot's team number";
		private const string DESC_RANK_SORTER = "How should we sort the players on the leaderboards.";
		private const string DESC_RANK_PROCESSOR = "How should we modify the player's rank on the leaderboards.";
		private const string DESC_ALLOWED_MAPS = "Which maps are allowed to be played with this game mode.";
		private const string DESC_COMPLETION_STRATEGY = "What should mark the end of a match.";
		private const string DESC_ALLOW_EARLY_REWARDS = "Can player earn rewards mid-game ?";
		private const string DESC_COINS_MULT = "Meta Coins multiplier";
		private const string DESC_BPP_MULT = "Meta BPP multiplier";
		private const string DESC_COMPLETION_KILL_COUNT = "How many kills must a player have to win the match.";
		private const string DESC_GAME_SIMULATION_SM = "Which state machine to use for game simulation";
		private const string DESC_AUDIO_SM = "Which state machine to use for audio.";
		private const string DESC_SYSTEMS = "Which Quantum systems should be enabled for this game mode.";
		private const string DESC_IS_DEBUG_ONLY = "Marks this game mode to be available only in Debug builds.";
		// @formatter:on

#endregion
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumGameModeConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = true)]
	public partial class QuantumGameModeConfigs
	{
		public List<QuantumGameModeConfig> QuantumConfigs = new List<QuantumGameModeConfig>();

		private IDictionary<string, QuantumGameModeConfig> _dictionary;

		private object _lock = new object();

		/// <summary>
		/// Requests the <see cref="QuantumGameModeConfig"/> defined by the given <paramref name="name"/>
		/// </summary>
		public QuantumGameModeConfig GetConfig(string name)
		{
			if (_dictionary == null)
			{
				lock (_lock)
				{
					var dictionary = new Dictionary<string, QuantumGameModeConfig>();
					for (var i = 0; i < QuantumConfigs.Count; i++)
					{
						dictionary.Add(QuantumConfigs[i].Id, QuantumConfigs[i]);
					}
					_dictionary = dictionary;
				}
			}
			return _dictionary[name];
		}
	}
}