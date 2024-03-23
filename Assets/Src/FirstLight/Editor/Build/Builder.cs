using System.Diagnostics;
using System.IO;
using FirstLight.Editor.Build.Utils;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

namespace FirstLight.Editor.Build
{
	/// <summary>
	/// Entry point for batch mode build calls.
	/// </summary>
	public static class Builder
	{
		/// <summary>
		/// Combines the configure and build steps
		/// </summary>
		[UsedImplicitly]
		[MenuItem("FLG/Build/Batch Build")]
		public static void BatchBuild()
		{
			var buildTarget = BuildUtils.GetBuildTarget();
			var environment = BuildUtils.GetEnvironment();
			var buildNumber = BuildUtils.GetBuildNumber();
			var isDevelopmentBuild = BuildUtils.GetIsDevelopmentBuild();

			var buildConfig = new BuildPlayerOptions();

			SetupBuildNumber(buildNumber);
			SetupDevelopmentBuild(isDevelopmentBuild, ref buildConfig);
			SetupAddressables(environment, buildTarget);
			SetupServerDefines(environment, ref buildConfig);
			SetupAndroidKeystore();

			AssetDatabase.Refresh();

			// Additional build options
			buildConfig.target = buildTarget;
			buildConfig.options |= BuildOptions.CompressWithLz4HC;
			buildConfig.locationPathName = "app";

			var buildReport = BuildPipeline.BuildPlayer(buildConfig);

			if (buildReport.summary.result != BuildResult.Succeeded)
			{
				EditorApplication.Exit(1);
			}
		}

		private static void SetupDevelopmentBuild(bool isDevelopmentBuild, ref BuildPlayerOptions buildOptions)
		{
			if (isDevelopmentBuild)
			{
				buildOptions.options |= BuildOptions.Development;
			}
		}

		private static void SetupAddressables(string environment, BuildTarget buildTarget)
		{
			var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;

			var profileName = $"CCD-{environment}-{buildTarget.ToString()}";
			var profileId = addressableSettings.profileSettings.GetProfileId(profileName);
			if (string.IsNullOrEmpty(profileId))
			{
				Debug.LogError($"Could not find Addressable profile: {profileName}");
				EditorApplication.Exit(1);
			}

			AddressableAssetSettingsDefaultObject.Settings.activeProfileId = profileId;

			// On dev we have unique catalogs (with null it generates a timestamp suffix)
			if (environment == BuildUtils.ENV_DEV)
			{
				AddressableAssetSettingsDefaultObject.Settings.OverridePlayerVersion = null;
			}

			AssetDatabase.Refresh();
		}

		private static void SetupBuildNumber(int buildNumber)
		{
			PlayerSettings.Android.bundleVersionCode = buildNumber;
			PlayerSettings.iOS.buildNumber = buildNumber.ToString();
		}

		private static void SetupServerDefines(string environment, ref BuildPlayerOptions buildOptions)
		{
			Assert.IsNull(buildOptions.extraScriptingDefines, "Scripting defines should be null here!");

			switch (environment)
			{
				case BuildUtils.ENV_DEV:
					buildOptions.extraScriptingDefines = new[] {"DEV_SERVER"};
					break;
				case BuildUtils.ENV_STAGING:
					buildOptions.extraScriptingDefines = new[] {"STAGE_SERVER"};
					break;
				case BuildUtils.ENV_PROD:
					buildOptions.extraScriptingDefines = new[] {"PROD_SERVER"};
					break;
				default:
					Debug.LogError($"Unrecognised environment: {environment}");
					EditorApplication.Exit(1);
					break;
			}
		}

		[Conditional("UNITY_ANDROID")]
		public static void SetupAndroidKeystore()
		{
			PlayerSettings.Android.useCustomKeystore = true;
			PlayerSettings.Android.keystoreName =
				Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Certificates", "firstlightgames.keystore"));
			PlayerSettings.Android.keystorePass = "***REMOVED***";
			PlayerSettings.Android.keyaliasName = "blastroyale";
			PlayerSettings.Android.keyaliasPass = "***REMOVED***";
		}
	}
}