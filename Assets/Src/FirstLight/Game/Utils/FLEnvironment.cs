using System;

namespace FirstLight.Game.Utils
{
	public static class FLEnvironment
	{
		public static readonly Definition DEVELOPMENT = new (
			"***REMOVED***",
			"***REMOVED***",
			"***REMOVED***",
			"***REMOVED***",
			"***REMOVED***",
			"development"
		);

		public static readonly Definition STAGING = new (
			"***REMOVED***",
			"***REMOVED***",
			null,
			"***REMOVED***",
			"***REMOVED***",
			"staging"
		);

		public static readonly Definition COMMUNITY = new (
			"***REMOVED***",
			"***REMOVED***",
			null,
			"***REMOVED***",
			"***REMOVED***",
			"community"
		);

		public static readonly Definition PRODUCTION = new (
			"***REMOVED***",
			"***REMOVED***",
			null,
			"***REMOVED***",
			"***REMOVED***",
			"production"
		);

		/// <summary>
		/// The currently used environment.
		///
		/// NOTE: This line is regenerated on build, so don't change it.
		/// </summary>
		public static Definition Current { get; set; } = GetCurrentEditorEnvironment();

		public struct Definition
		{
			/// <summary>
			/// A generic name for the environment.
			/// </summary>
			public string Name => Current.UCSEnvironmentName;

			/// <summary>
			/// Identifies the playfab title for this environment.
			/// </summary>
			public readonly string PlayFabTitleID;

			/// <summary>
			/// Playfab template ID that contains the password recovery html template.
			/// </summary>
			public readonly string PlayFabRecoveryEmailTemplateID;

			/// <summary>
			/// Identifies the web3 id that will be used to identify it on the web3 layer.
			/// </summary>
			public readonly string Web3ID;

			/// <summary>
			/// Identify the photon application id that will be used by quantum.
			/// </summary>
			public readonly string PhotonAppIDRealtime;

			/// <summary>
			/// The ID of the Unity Cloud Services environment.
			/// </summary>
			public readonly string UCSEnvironmentID;

			/// <summary>
			/// The name of the Unity Cloud Services environment.
			/// </summary>
			public readonly string UCSEnvironmentName;

			public Definition(string playFabTitleID, string playFabRecoveryEmailTemplateID, string web3Id, string photonAppIDRealtime,
							  string ucsEnvironmentID, string ucsEnvironmentName)
			{
				PlayFabTitleID = playFabTitleID;
				PlayFabRecoveryEmailTemplateID = playFabRecoveryEmailTemplateID;
				Web3ID = web3Id;
				PhotonAppIDRealtime = photonAppIDRealtime;
				UCSEnvironmentID = ucsEnvironmentID;
				UCSEnvironmentName = ucsEnvironmentName;
			}
		}

		#region EditorHelpers

#if UNITY_EDITOR

		private const string ENV_KEY = "FLG_ENVIRONMENT";

		private static Definition GetCurrentEditorEnvironment()
		{
			return UnityEditor.EditorPrefs.GetString(ENV_KEY, "development") switch
			{
				"development" => DEVELOPMENT,
				"staging"     => STAGING,
				"community"   => COMMUNITY,
				"production"  => PRODUCTION,
				_             => throw new NotSupportedException("Invalid environment type")
			};
		}

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/development", false, 18)]
		private static void ToggleEnvironmentDevelopment() => ToggleEnvironment("development");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/development", true, 18)]
		private static bool ValidateEnvironmentDevelopment() => ValidateEnvironment("development");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/staging", false, 18)]
		private static void ToggleEnvironmentStaging() => ToggleEnvironment("staging");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/staging", true, 18)]
		private static bool ValidateEnvironmentStaging() => ValidateEnvironment("staging");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/community", false, 18)]
		private static void ToggleEnvironmentCommunity() => ToggleEnvironment("community");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/community", true, 18)]
		private static bool ValidateEnvironmentCommunity() => ValidateEnvironment("community");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/production", false, 18)]
		private static void ToggleEnvironmentProduction() => ToggleEnvironment("production");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/production", true, 18)]
		private static bool ValidateEnvironmentProduction() => ValidateEnvironment("production");

		private static bool ValidateEnvironment(string environment)
		{
			UnityEditor.Menu.SetChecked($"FLG/Local Flags/Environment/{environment}",
				UnityEditor.EditorPrefs.GetString(ENV_KEY, "development") == environment);
			return true;
		}

		private static void ToggleEnvironment(string environment)
		{
			UnityEditor.EditorPrefs.SetString(ENV_KEY, environment);
		}

#endif

		#endregion
	}
}