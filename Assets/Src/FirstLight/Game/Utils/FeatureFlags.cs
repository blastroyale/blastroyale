using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules;
using PlayFab;
using UnityEngine;

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

		/// <summary>
		/// If the tutorial should be skipped
		/// </summary>
		public bool DisableTutorial = true;
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
		/// When true, will send end of match commands using quantum server consensus algorithm.
		/// When false commands will go directly to our backend. 
		/// To use this in our backend the backend needs to be compiled with this flag being False.
		/// </summary>
		public static bool QUANTUM_CUSTOM_SERVER = false;

		/// <summary>
		/// If true will load game configurations from remote server
		/// </summary>
		public static bool REMOTE_CONFIGURATION = false;

		/// <summary>
		/// Enables / disables item durability checks for Non NFTs
		/// </summary>
		public static bool ITEM_DURABILITY_NON_NFTS = true;
		
		/// <summary>
		/// Enables / disables item durability checks for NFTs
		/// </summary>
		public static bool ITEM_DURABILITY_NFTS = false;

		/// <summary>
		/// If true all matches will be handled as ranked matches
		/// </summary>
		public static bool FORCE_RANKED = false;

		/// <summary>
		/// Enables / disables the store button in the home screen
		/// </summary>
		public static bool STORE_ENABLED = false;
		
		/// <summary>
		/// Will try to detect and raise any desyncs server/client finds.
		/// </summary>
		public static bool DESYNC_DETECTION = true;
		
		/// <summary>
		/// Will try to detect and raise any desyncs server/client finds.
		/// </summary>
		public static bool SQUAD_PINGS = true;

		/// <summary>
		/// Flag to determine if we should use playfab matchmaking
		/// </summary>
		public static bool PLAYFAB_MATCHMAKING = false;

		/// <summary>
		/// If the tutorial is active, useful for testing
		/// </summary>
		public static bool TUTORIAL = false;
		
		/// <summary>
		/// If the tutorial is active, useful for testing
		/// </summary>
		public static bool ALLOW_SKIP_TUTORIAL = true;
		
		/// <summary>
		/// If should have specific tutorial battle pass for newbies
		/// </summary>
		public static bool TUTORIAL_BATTLE_PASS = false;
		
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
			
			if (TrySetFlag("REMOTE_CONFIGURATION", overrideData, out var remoteConfig))
			{
				REMOTE_CONFIGURATION = remoteConfig;
			}
			
			if (TrySetFlag("FORCE_RANKED", overrideData, out var forceRanked))
			{
				FORCE_RANKED = forceRanked;
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
			
			if (TrySetFlag("DESYNC_DETECTION", overrideData, out var desyncDetection))
			{
				DESYNC_DETECTION = desyncDetection;
			}
			
			if (TrySetFlag("PLAYFAB_MATCHMAKING", overrideData, out var pfmm))
			{
				PLAYFAB_MATCHMAKING = pfmm;
			}

			if (TrySetFlag("SQUAD_PINGS", overrideData, out var squadPings))
			{
				SQUAD_PINGS = squadPings;
			}
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

			if (_localConfig.DisableTutorial)
			{
				TUTORIAL = false;
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
