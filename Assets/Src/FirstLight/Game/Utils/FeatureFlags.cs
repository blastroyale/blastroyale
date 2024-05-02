using System;
using System.Collections.Generic;
using System.ComponentModel;
using FirstLight.FLogger;
using FirstLight.Server.SDK.Modules;
using PlayFab;
using UnityEngine;

// ReSharper disable RedundantDefaultMemberInitializer

namespace FirstLight.Game.Utils
{
	public enum FlagOverwrite
	{
		None,
		True,
		False
	}

	public static class FlagOverwriteExt
	{
		public static bool Bool(this FlagOverwrite flag)
		{
			return flag switch
			{
				FlagOverwrite.True  => true,
				FlagOverwrite.False => false,
				_                   => false
			};
		}
	}

	/// <summary>
	/// Class that represents feature flags that can be configured locally for testing purposes
	/// </summary>
	public class LocalFeatureFlagConfig
	{
		/// <summary>
		/// Requests will be routed to local backend. To run, run "StandaloneServer" on backend project.
		/// </summary>
		[Description("Use local server")] public bool UseLocalServer = false;

		/// <summary>
		/// To use local configurations as opposed to remote configurations.
		/// </summary>
		[Description("Use local configs")] public bool UseLocalConfigs = false;

		/// <summary>
		/// If the tutorial should be skipped
		/// </summary>
		[Description("Tutorial")] public FlagOverwrite Tutorial = FlagOverwrite.None;

		/// <summary>
		/// Which environment to connect
		/// </summary>
		// public Environment EnvironmentOverride = Environment.DEV;

		/// <summary>
		/// Record quantum input and save on simulation end
		/// </summary>
		[Description("Record quantum input")] public bool RecordQuantumInput = false;

		/// <summary>
		/// Force authentication connection error
		/// </summary>
		[Description("Force Authentication Connection Error")]
		public bool ForceAuthError = false;

		/// <summary>
		/// Force authentication connection error
		/// </summary>
		[Description("Dev QOL/Disable Pause Behaviour")]
		public bool DisablePauseBehaviour = false;

		/// <summary>
		/// Requests will be routed to local backend. To run, run "StandaloneServer" on backend project.
		/// </summary>
		[Description("Dev QOL/Offline mode")] public bool OfflineMode = false;

		/// <summary>
		/// If automatically starts the test game mode after the game boot up
		/// </summary>
		[Description("Dev QOL/Autostart test game")]
		public bool StartTestGameAutomatically = false;

		/// <summary>
		/// If we should consider if the player has NFTs even if he doens't
		/// </summary>
		[Description("Dev QOL/Unlock all fame stuff")]
		public bool UnlockAllFameStuff = false;

		/// <summary>
		/// If we should consider if the player has NFTs even if he doens't
		/// </summary>
		[Description("Dev QOL/Disable Reconnection")]
		public bool DisableReconnection = false;

		/// <summary>
		/// Force authentication connection error
		/// </summary>
		[Description("Dev QOL/Append Minute to Playtest room")]
		public bool AppendMinuteToPlaytest = true;
	}


	/// <summary>
	/// Simple class to represent feature flags in the game.
	/// Not configurable in editor atm.
	/// </summary>
	public static class FeatureFlags
	{
		private static LocalFeatureFlagConfig _localConfig = null;

		/// <summary>
		/// If true, rooms created/joined will be locked by commit
		/// If false, player can create/join rooms not locked by commit
		/// </summary>
		public static bool COMMIT_VERSION_LOCK = true;

		/// <summary>
		/// When true will display "BETA" in loading screen
		/// </summary>
		public static bool BETA_VERSION = false;

		/// <summary>
		/// When true, will send end of match commands using quantum server consensus algorithm.
		/// When false commands will go directly to our backend. 
		/// To use this in our backend the backend needs to be compiled with this flag being False.
		/// </summary>
		public static bool QUANTUM_CUSTOM_SERVER = false;

		/// <summary>
		/// When true will wait rewards to be synced before allowing players to continue playing
		/// </summary>
		public static bool WAIT_REWARD_SYNC = false;

		/// <summary>
		/// Forces to stop the game when pausing
		/// </summary>
		public static bool PAUSE_FREEZE = true;

		/// <summary>
		/// Enables / disables item durability checks for Non NFTs
		/// </summary>
		public static bool ITEM_DURABILITY_NON_NFTS = false;

		/// <summary>
		/// Enables / disables item durability checks for NFTs
		/// </summary>
		public static bool ITEM_DURABILITY_NFTS = false;

		/// <summary>
		/// Enables / disables the store button in the home screen
		/// </summary>
		public static bool STORE_ENABLED = false;

		/// <summary>
		/// Enables / disables the player stats button in the home screen
		/// </summary>
		public static bool PLAYER_STATS_ENABLED = true;

		/// <summary>
		/// Will try to detect and raise any desyncs server/client finds.
		/// </summary>
		public static bool DESYNC_DETECTION = false;

		/// <summary>
		/// If the tutorial is active, useful for testing
		/// </summary>
		public static bool TUTORIAL = true;

		/// <summary>
		/// If the main menu systems start locked, useful for testing
		/// </summary>
		public static bool SYSTEM_LOCKS = true;

		/// <summary>
		/// If the squads button is enabled in the UI
		/// </summary>
		public static bool DISPLAY_SQUADS_BUTTON = true;

		/// <summary>
		/// When enabled will enable aiming deadzone to avoid missfires
		/// </summary>
		public static bool AIM_DEADZONE = true;

		/// <summary>
		/// Will replace map music by ambience sound effects
		/// </summary>
		public static bool NEW_SFX = true;

		/// <summary>
		/// If true will be slightly more delayed aim but will be precise to Quantum inputs
		/// If false it will be more accurate visually but not necessarily shoot where you aim
		/// </summary>
		public static bool QUANTUM_PREDICTED_AIM = false;

		/// <summary>
		/// Should specials use new input system
		/// </summary>
		public static bool SPECIAL_NEW_INPUT = true;

		/// <summary>
		/// Camera shake when player receives damage
		/// </summary>
		public static bool DAMAGED_CAMERA_SHAKE = false;

		/// <summary>
		/// Should game fetch remote web3 collections
		/// </summary>
		public static bool REMOTE_COLLECTIONS = false;

		/// <summary>
		/// Only for testing.
		/// When true, you will become invisible when entering bushes.
		/// </summary>
		public static bool ALWAYS_TOGGLE_INVISIBILITY_AREAS = false;

		/// <summary>
		/// When not null, will means the game can connect to WEB3.
		/// When the ID is set it will be used by the client to authenticate the web3 
		/// client.
		/// </summary>
		public static string IMX_ID = null;

		/// <summary>
		/// Feature flag to toggle noob in the game
		/// </summary>
		public static bool ENABLE_NOOB = false;

		/// <summary>
		/// Parses the feature flags from a given input dictionary.
		/// Keys of the dictionary will be matched as title feature flag keys referenced on the attributes.
		/// Values will be converted to boolean ('true' or 'false)
		/// </summary>
		public static void ParseFlags(Dictionary<string, string> overrideData)
		{
			if (TrySetFlag("QUANTUM_CUSTOM_SERVER", overrideData, out var customServer))
			{
				QUANTUM_CUSTOM_SERVER = customServer;
			}

			if (TrySetFlag("COMMIT_VERSION_LOCK", overrideData, out var commitVersionLock))
			{
				COMMIT_VERSION_LOCK = commitVersionLock;
			}

			if (TrySetFlag("ITEM_DURABILITY_NON_NFTS", overrideData, out var itemDurabilityNonNFTs))
			{
				ITEM_DURABILITY_NON_NFTS = itemDurabilityNonNFTs;
			}

			if (TrySetFlag("ITEM_DURABILITY_NFTS", overrideData, out var itemDurabilityNFTs))
			{
				ITEM_DURABILITY_NFTS = itemDurabilityNFTs;
			}

			if (TrySetFlag("STORE_ENABLED", overrideData, out var storeEnabled))
			{
				STORE_ENABLED = storeEnabled;
			}

			if (TrySetFlag("PLAYER_STATS_ENABLED", overrideData, out var playerStatsEnabled))
			{
				PLAYER_STATS_ENABLED = playerStatsEnabled;
			}

			if (TrySetFlag("DESYNC_DETECTION", overrideData, out var desyncDetection))
			{
				DESYNC_DETECTION = desyncDetection;
			}

			if (TrySetFlag("TUTORIAL", overrideData, out var tutorial))
			{
				TUTORIAL = tutorial;
			}

			if (TrySetFlag("DISPLAY_SQUADS_BUTTON", overrideData, out var displaySquadsButton))
			{
				DISPLAY_SQUADS_BUTTON = displaySquadsButton;
			}

			if (TrySetFlag("PAUSE_FREEZE", overrideData, out var pauseFreeze))
			{
				PAUSE_FREEZE = pauseFreeze;
			}

			if (TrySetFlag("WAIT_REWARD_SYNC", overrideData, out var waitSync))
			{
				WAIT_REWARD_SYNC = waitSync;
			}

			if (TrySetStringFlag("IMX_ID", overrideData, out var imxId))
			{
				IMX_ID = imxId;
			}

			if (TrySetFlag("BETA_VERSION", overrideData, out var beta))
			{
				BETA_VERSION = beta;
			}

			if (TrySetFlag("ENABLE_NOOB", overrideData, out var enableNoob))
			{
				ENABLE_NOOB = enableNoob;
			}
		}

		/// <summary>
		/// Reads locally set feature flags to override feature flags or perform setup needed.
		/// </summary>
		public static void ParseLocalFeatureFlags()
		{
			LoadLocalConfig();

			if (_localConfig.UseLocalServer)
			{
				PlayFabSettings.LocalApiServer = "http://localhost:7274";
			}

			if (_localConfig.Tutorial != FlagOverwrite.None)
			{
				TUTORIAL = _localConfig.Tutorial.Bool();
			}
		}

		/// <summary>
		/// Reads the local configuration. Will return default object when not present.
		/// </summary>
		public static LocalFeatureFlagConfig GetLocalConfiguration()
		{
			if (_localConfig == null)
			{
				LoadLocalConfig();
			}

			return _localConfig;
		}

		/// <summary>
		/// Saves local feature flag configs to disk
		/// </summary>
		public static void SaveLocalConfig()
		{
#if UNITY_EDITOR
			UnityEditor.EditorPrefs.SetString("LocalFeatureFlags", ModelSerializer.Serialize(_localConfig).Value);
			Debug.Log("Saved local config for feature flags");
#endif
		}

		/// <summary>
		/// Loads local feature flag configs from disk
		/// </summary>
		public static void LoadLocalConfig()
		{
#if UNITY_EDITOR
			var localConfig = UnityEditor.EditorPrefs.GetString("LocalFeatureFlags", null);
			if (!string.IsNullOrEmpty(localConfig))
			{
				_localConfig = ModelSerializer.Deserialize<LocalFeatureFlagConfig>(localConfig);
				return;
			}
#endif
			_localConfig = new ();
		}

		private static bool TrySetFlag(string flagName, Dictionary<string, string> titleData, out bool flag)
		{
			if (TrySetStringFlag(flagName, titleData, out var stringFlag))
			{
				flag = stringFlag.ToLower() == "true";
				return true;
			}
			else
			{
				flag = false;
				return false;
			}
		}

		private static bool TrySetStringFlag(string flagName, Dictionary<string, string> titleData, out string flag)
		{
			if (titleData.TryGetValue(flagName, out var flagValue))
			{
				flag = flagValue;
				FLog.Verbose($"Setting title flag {flagName} to {flag}");
				return true;
			}
			else
			{
				FLog.Verbose($"Disabling flag {flagName}");
				flag = null;
			}

			return false;
		}
	}
}