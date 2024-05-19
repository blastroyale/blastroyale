using System.IO;
using FirstLight.Editor.Build.Utils;
using FirstLight.Game.Utils;
using SRDebugger.Editor;
using Unity.Services.PushNotifications;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace FirstLight.Editor.Build
{
	public class FLBuildProcessor : BuildPlayerProcessor
	{
		public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
		{
			var environment = BuildUtils.GetEnvironment();
			var environmentDefinition = FLEnvironment.FromName(environment);
			var developmentBuild = buildPlayerContext.BuildPlayerOptions.options.HasFlag(BuildOptions.Development);

			PrepareFirebase(environment);
			VersionEditorUtils.SetAndSaveInternalVersion(environment);
			GenerateEnvironment(environment);
			ConfigureQuantum(developmentBuild);
			SetupSRDebugger(developmentBuild);
			SetupPushNotifications(environmentDefinition);

			// Probably not needed but why not
			AssetDatabase.Refresh();
		}

		private static void ConfigureQuantum(bool developmentBuild)
		{
			var guids = AssetDatabase.FindAssets($"t:{nameof(DeterministicSessionConfigAsset)}");
			var path = AssetDatabase.GUIDToAssetPath(guids[0]);
			var deterministicConfig = AssetDatabase.LoadAssetAtPath<DeterministicSessionConfigAsset>(path);

			deterministicConfig.Config.ChecksumInterval = developmentBuild ? 60 : 0;

			EditorUtility.SetDirty(deterministicConfig);
			AssetDatabase.SaveAssets();
		}

		private static void PrepareFirebase(string environment)
		{
			var configDirectory = Path.Combine(Application.dataPath, "../", "Configs");

			// We force DEV environment on staging
			if (environment == FLEnvironment.STAGING.Name) environment = FLEnvironment.DEVELOPMENT.Name;

			// iOS
			File.Copy(Path.Combine(configDirectory, $"GoogleService-Info-{environment}.plist"),
				Path.Combine(Application.streamingAssetsPath, "GoogleService-Info.plist"), true);

			// Android
			File.Copy(Path.Combine(configDirectory, $"google-services-{environment}.json"),
				Path.Combine(Application.streamingAssetsPath, "google-services.plist"), true);
		}

		private static void GenerateEnvironment(string environment)
		{
			var envAsset = AssetDatabase.LoadAssetAtPath<FLEnvironmentAsset>("Assets/Resources/FLEnvironmentAsset.asset");
			envAsset.EnvironmentName = environment;
			EditorUtility.SetDirty(envAsset);
		}

		private static void SetupSRDebugger(bool developmentBuild)
		{
			SRDebugEditor.SetEnabled(developmentBuild);
		}

		private void SetupPushNotifications(FLEnvironment.Definition environment)
		{
			var settings = AssetDatabase.LoadAssetAtPath<PushNotificationSettings>("Assets/Resources/pushNotificationsSettings.asset");

			settings.firebaseAppID = environment.FirebaseAppID;
			settings.firebaseProjectID = environment.FirebaseProjectID;
			settings.firebaseProjectNumber = environment.FirebaseProjectNumber;
			settings.firebaseWebApiKey = environment.FirebaseWebApiKey;

			EditorUtility.SetDirty(settings);
			AssetDatabase.SaveAssetIfDirty(settings);
		}
	}
}