using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Server.SDK.Modules;
using PlayFab;

// ReSharper disable RedundantDefaultMemberInitializer

namespace FirstLight.Game.Utils
{
	
	/// <summary>
	/// Class that represents feature flags that can be configured locally for testing purposes
	/// </summary>
	public class LocalFeatureFlagConfig
	{
		/// <summary>
		/// Requests will be routed to local backend. To run, run "StandaloneServer" on backend project.
		/// </summary>
		public bool UseLocalServer = false;

		/// <summary>
		/// To use local configurations as opposed to remote configurations.
		/// </summary>
		public bool UseLocalConfigs = false;
	}
	
	
	/// <summary>
	/// Simple class to represent feature flags in the game.
	/// Not configurable in editor atm.
	/// </summary>
	public static class FeatureFlags
	{
		private static LocalFeatureFlagConfig _localConfig = null;
		
		/// <summary>
		/// If true will use email/pass authentication.
		/// If false will only use device id authentication.
		/// </summary>
		public static bool EMAIL_AUTH = true;

		/// <summary>
		/// If true, rooms created/joined will be locked by commit
		/// If false, player can create/join rooms not locked by commit
		/// </summary>
		public static bool COMMIT_VERSION_LOCK = true;

		/// <summary>
		/// When true, will send end of match commands using quantum server consensus algorithm.
		/// When false commands will go directly to our backend. 
		/// To use this in our backend the backend needs to be compiled with this flag being False.
		/// </summary>
		public static bool QUANTUM_CUSTOM_SERVER = false;

		/// <summary>
		/// If false, testing game mode selection will be disabled in GameModeSelectionPresenter
		/// </summary>
		public static readonly bool TESTING_GAME_MODE_ENABLED = true;

		/// <summary>
		/// If false, leaderboard button will be disabled on the home screen
		/// </summary>
		public static readonly bool LEADERBOARD_ACCESSIBLE = true;

		/// <summary>
		/// If true will load game configurations from remote server
		/// </summary>
		public static bool REMOTE_CONFIGURATION = false;

		/// <summary>
		/// If true we award BattlePass points (BPP) and show the BattlePass button on the home screen.
		/// </summary>
		public static bool BATTLE_PASS_ENABLED = false;

		/// <summary>
		/// Parses the feature flags from a given input dictionary.
		/// Keys of the dictionary will be matched as title feature flag keys referenced on the attributes.
		/// Values will be converted to boolean ('true' or 'false)
		/// </summary>
		public static void ParseFlags(Dictionary<string, string> titleData)
		{
			if (TrySetFlag("QUANTUM_CUSTOM_SERVER", titleData, out var customServer))
			{
				QUANTUM_CUSTOM_SERVER = customServer;
			}

			if (TrySetFlag("COMMIT_VERSION_LOCK", titleData, out var commitVersionLock))
			{
				COMMIT_VERSION_LOCK = commitVersionLock;
			}

			if (TrySetFlag("REMOTE_CONFIGURATION", titleData, out var remoteConfig))
			{
				REMOTE_CONFIGURATION = remoteConfig;
			}
			ParseLocalFeatureFlags();
		}

		/// <summary>
		/// Reads locally set feature flags to override feature flags or perform setup needed.
		/// </summary>
		public static void ParseLocalFeatureFlags()
		{
			LoadLocalConfig();
			if (_localConfig.UseLocalConfigs)
			{
				REMOTE_CONFIGURATION = false;
			}
			if (_localConfig.UseLocalServer)
			{
				PlayFabSettings.LocalApiServer = "http://localhost:7274";
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
			FLog.Verbose("Saved local config for feature flags");
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
				FLog.Verbose($"Loaded local configs from local storage: {localConfig}");
				return;
			}
#endif
			_localConfig = new();
		}

		private static bool TrySetFlag(string flagName, Dictionary<string, string> titleData, out bool flag)
		{
			if (titleData.TryGetValue(flagName, out var flagValue))
			{
				flag = flagValue.ToLower() == "true";
				FLog.Verbose($"Setting title flag {flagName} to {flag}");
				return true;
			}
			else
			{
				FLog.Verbose($"Disabling flag {flagName}");
				flag = false;
			}

			return false;
		}
	}
}