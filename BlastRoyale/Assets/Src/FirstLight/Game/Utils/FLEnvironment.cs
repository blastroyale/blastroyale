using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Server.SDK.Modules;
using UnityEngine;
using UnityEngine.Networking;

namespace FirstLight.Game.Utils
{
	public static class FLEnvironment
	{
		private const string BUILD_FILE_TEMPLATE = "https://cdn.blastroyale.com/builds/{0}_{1}.json";

		private const string ENV_KEY = "FLG_ENVIRONMENT";

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
			"***REMOVED***",
			"blast-royale-staging",
			"***REMOVED***",
			"***REMOVED***"
		);

		public static readonly Definition COMMUNITY = new (
			"***REMOVED***",
			"***REMOVED***",
			null,
			"***REMOVED***",
			"***REMOVED***",
			"community",
			"***REMOVED***",
			"***REMOVED***",
			"blast-royale-community",
			"***REMOVED***",
			"***REMOVED***"
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
		public static Definition Current { get; set; } = FromName(UnityEngine.PlayerPrefs.GetString(ENV_KEY,
			UnityEngine.Resources.Load<FLEnvironmentAsset>("FLEnvironmentAsset").EnvironmentName));

		public readonly struct Definition
		{
			/// <summary>
			/// A generic name for the environment.
			/// </summary>
			public string Name => UCSEnvironmentName;

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
			public readonly string FirebaseAndroidAppID;

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
							  string ucsEnvironmentID, string ucsEnvironmentName, string ucsBucketID, string firebaseAndroidAppID,
							  string firebaseProjectID,
							  string firebaseProjectNumber, string firebaseWebApiKey)
			{
				PlayFabTitleID = playFabTitleID;
				PlayFabRecoveryEmailTemplateID = playFabRecoveryEmailTemplateID;
				Web3ID = web3Id;
				PhotonAppIDRealtime = photonAppIDRealtime;
				UCSEnvironmentID = ucsEnvironmentID;
				UCSEnvironmentName = ucsEnvironmentName;
				UCSBucketID = ucsBucketID;
				FirebaseAndroidAppID = firebaseAndroidAppID;
				FirebaseProjectID = firebaseProjectID;
				FirebaseProjectNumber = firebaseProjectNumber;
				FirebaseWebApiKey = firebaseWebApiKey;
			}

			public bool Equals(Definition other)
			{
				return UCSEnvironmentID == other.UCSEnvironmentID;
			}

			public override bool Equals(object obj)
			{
				return obj is Definition other && Equals(other);
			}

			public override int GetHashCode()
			{
				return UCSEnvironmentID != null ? UCSEnvironmentID.GetHashCode() : 0;
			}

			public static bool operator ==(Definition left, Definition right)
			{
				return left.Equals(right);
			}

			public static bool operator !=(Definition left, Definition right)
			{
				return !left.Equals(right);
			}
		}

		public static bool TryGetFromName(string environment, out Definition definition)
		{
			if (environment == "development")
				definition = DEVELOPMENT;
			else if (environment == "staging")
				definition = STAGING;
			else if (environment == "community")
				definition = COMMUNITY;
			else if (environment == "production")
				definition = PRODUCTION;
			else
			{
				definition = default;
				return false;
			}

			return true;
		}

		public static Definition FromName(string environment)
		{
			if (TryGetFromName(environment, out var definition))
			{
				return definition;
			}

			throw new NotSupportedException("Invalid environment type");
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		private static async Task<(bool Success, Definition Definition)> TryGetEnvironmentRedirect(string gameBuild)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			#if !UNITY_EDITOR
			var path = string.Format(BUILD_FILE_TEMPLATE, Application.platform.ToString(), gameBuild);
			FLog.Info($"Downloading redirect info from {path}");
			try
			{
				var start = Time.time;
				var req = UnityWebRequest.Get(path);
				req.timeout = 2000;
				var response = await req.SendWebRequest().ToUniTask();
				FLog.Info("Build info download took " + (Time.time - start) + "s");
				if (response.result == UnityWebRequest.Result.Success)
				{
					var text = response.downloadHandler.text;
					FLog.Info(text);
					var value = ModelSerializer.Deserialize<RemoteVersionData>(text);
					if (FLEnvironment.TryGetFromName(value.EnvironmentOverwrite, out var definition))
					{
						return (true, definition);
					}
				}
			}
			catch (UnityWebRequestException ex)
			{
				if (ex.ResponseCode == 404)
				{
					return (false, default);
				}

				FLog.Warn("Failed to download build redirect", ex);
			}
			catch (Exception ex)
			{
				FLog.Warn("Failed to download build redirect", ex);
			}
			#endif
			return (false, default);
		}

		public static async UniTask CheckForEnvironmentRedirect(string gameBuild)
		{
			var (success, direction) = await TryGetEnvironmentRedirect(gameBuild);
			if (success)
			{
				FLog.Info("Redirecting to " + direction.Name + " environment!");
				Current = direction;
			}
		}

		#region EditorHelpers

#if UNITY_EDITOR

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/development", false, 18)]
		private static void ToggleEnvironmentDevelopment() => SetEnvironment("development");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/development", true, 18)]
		private static bool ValidateEnvironmentDevelopment() => ValidateEnvironment("development");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/staging", false, 18)]
		private static void ToggleEnvironmentStaging() => SetEnvironment("staging");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/staging", true, 18)]
		private static bool ValidateEnvironmentStaging() => ValidateEnvironment("staging");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/community", false, 18)]
		private static void ToggleEnvironmentCommunity() => SetEnvironment("community");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/community", true, 18)]
		private static bool ValidateEnvironmentCommunity() => ValidateEnvironment("community");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/production", false, 18)]
		private static void ToggleEnvironmentProduction() => SetEnvironment("production");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/production", true, 18)]
		private static bool ValidateEnvironmentProduction() => ValidateEnvironment("production");

		private static bool ValidateEnvironment(string environment)
		{
			UnityEditor.Menu.SetChecked($"FLG/Local Flags/Environment/{environment}",
				UnityEngine.PlayerPrefs.GetString(ENV_KEY, "development") == environment);
			return true;
		}

		private static void SetEnvironment(string environment)
		{
			UnityEngine.PlayerPrefs.SetString(ENV_KEY, environment);
		}

#endif

		#endregion
	}
}