
using System.Collections.Generic;
using System.Reflection;
using FirstLight.FLogger;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Simple class to represent feature flags in the game.
	/// Not configurable in editor atm.
	/// </summary>
	public static class FeatureFlags
	{
		/// <summary>
		/// If true will use email/pass authentication.
		/// If false will only use device id authentication.
		/// </summary>
		public static readonly bool EMAIL_AUTH = true;

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
		/// If false, deathmatch game mode selection will be disabled in GameModeSelectionPresenter
		/// </summary>
		public static readonly bool DEATHMATCH_ENABLED = false;
		
		/// <summary>
		/// If false, testing game mode selection will be disabled in GameModeSelectionPresenter
		/// </summary>
		public static readonly bool TESTING_GAME_MODE_ENABLED = true;
		
		/// <summary>
		/// If false, leaderboard button will be disabled on the home screen
		/// </summary>
		public static readonly bool LEADERBOARD_ACCESSIBLE = true;

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