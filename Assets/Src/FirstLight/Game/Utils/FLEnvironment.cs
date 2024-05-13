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
			"development",
			"***REMOVED***",
			"***REMOVED***",
			"blast-royale-dev",
			"***REMOVED***",
			"***REMOVED***4"
		);

		public static readonly Definition STAGING = new (
			"***REMOVED***",
			"***REMOVED***",
			null,
			"***REMOVED***",
			"***REMOVED***",
			"staging",
			"***REMOVED***",
			DEVELOPMENT.FirebaseAppID,
			DEVELOPMENT.FirebaseProjectID,
			DEVELOPMENT.FirebaseProjectNumber,
			DEVELOPMENT.FirebaseWebApiKey
		);

		public static readonly Definition COMMUNITY = new (
			"***REMOVED***",
			"***REMOVED***",
			null,
			"***REMOVED***",
			"***REMOVED***",
			"community",
			"***REMOVED***",
			DEVELOPMENT.FirebaseAppID, // TODO: We need a new environment / firebase app for community
			DEVELOPMENT.FirebaseProjectID,
			DEVELOPMENT.FirebaseProjectNumber,
			DEVELOPMENT.FirebaseWebApiKey
		);

		public static readonly Definition PRODUCTION = new (
			"***REMOVED***",
			"***REMOVED***",
			null,
			"***REMOVED***",
			"***REMOVED***",
			"production",
			"***REMOVED***",
			"***REMOVED***",
			"blast-royale",
			"***REMOVED***",
			"***REMOVED***"
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
			
			/// <summary>
			/// The ID of the CCD bucket for this environment.
			/// </summary>
			public readonly string UCSBucketID;

			/// <summary>
			/// The Firebase app ID.
			/// </summary>
			public readonly string FirebaseAppID;

			/// <summary>
			/// The Firebase project ID.
			/// </summary>
			public readonly string FirebaseProjectID;

			/// <summary>
			/// The Firebase project number.
			/// </summary>
			public readonly string FirebaseProjectNumber;

			/// <summary>
			/// The Firebase web API key.
			/// </summary>
			public readonly string FirebaseWebApiKey;

			public Definition(string playFabTitleID, string playFabRecoveryEmailTemplateID, string web3Id, string photonAppIDRealtime,
							  string ucsEnvironmentID, string ucsEnvironmentName, string ucsBucketID, string firebaseAppID, string firebaseProjectID,
							  string firebaseProjectNumber, string firebaseWebApiKey)
			{
				PlayFabTitleID = playFabTitleID;
				PlayFabRecoveryEmailTemplateID = playFabRecoveryEmailTemplateID;
				Web3ID = web3Id;
				PhotonAppIDRealtime = photonAppIDRealtime;
				UCSEnvironmentID = ucsEnvironmentID;
				UCSEnvironmentName = ucsEnvironmentName;
				UCSBucketID = ucsBucketID;
				FirebaseAppID = firebaseAppID;
				FirebaseProjectID = firebaseProjectID;
				FirebaseProjectNumber = firebaseProjectNumber;
				FirebaseWebApiKey = firebaseWebApiKey;
			}
		}

		#region EditorHelpers

#if UNITY_EDITOR

		private const string ENV_KEY = "FLG_ENVIRONMENT";

		public static Definition FromName(string environment)
		{
			return environment switch
			{
				"development" => DEVELOPMENT,
				"staging"     => STAGING,
				"community"   => COMMUNITY,
				"production"  => PRODUCTION,
				_             => throw new NotSupportedException("Invalid environment type")
			};
		}

		private static Definition GetCurrentEditorEnvironment()
		{
			return FromName(UnityEditor.EditorPrefs.GetString(ENV_KEY, "development"));
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

#else
		private static Definition GetCurrentEditorEnvironment() => throw new NotSupportedException("Invalid environment type");
#endif

		#endregion
	}
}