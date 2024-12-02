using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FirstLight.Editor.Build.Utils;
using FirstLight.Game.Utils;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build;
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
		/// Builds an addressable update - for testing only.
		/// </summary>
		public static void BuildAddressablesUpdateIOS()
		{
			SetupAddressables(true);
			ContentUpdateScript.BuildContentUpdate(AddressableAssetSettingsDefaultObject.Settings, "ServerData/iOS/addressables_content_state.bin");
		}

		/// <summary>
		/// Builds an addressable update - for testing only.
		/// </summary>
		public static void BuildAddressablesUpdateAndroid()
		{
			SetupAddressables(true);
			ContentUpdateScript.BuildContentUpdate(AddressableAssetSettingsDefaultObject.Settings,
				"ServerData/Android/addressables_content_state.bin");
		}

		/// <summary>
		/// Combines the configure and build steps
		/// </summary>
		[UsedImplicitly]
		[MenuItem("FLG/Build/Batch Build")]
		public static void BatchBuild()
		{
			var buildReport = Build(new BuildParameters()
			{
				BuildNumber = BuildUtils.GetBuildNumber(),
				BuildTarget = BuildUtils.GetBuildTarget(),
				DevelopmentBuild = BuildUtils.GetIsDevelopmentBuild(),
				RemoteAddressables = BuildUtils.GetUseRemoteAddressables(),
				UploadSymbolsToUnity = true,
			});

			if (buildReport.summary.result != BuildResult.Succeeded)
			{
				EditorApplication.Exit(1);
			}
		}

		public class BuildParameters
		{
			public BuildTarget BuildTarget;
			public int BuildNumber;
			public bool DevelopmentBuild;
			public bool RemoteAddressables;
			public bool UploadSymbolsToUnity;
			public BuildOptions AdditionalOptions;
		}

		internal static BuildReport Build(BuildParameters @params)
		{
			var buildConfig = new BuildPlayerOptions();

			SetupBuildNumber(@params.BuildNumber);
			SetupDevelopmentBuild(@params.DevelopmentBuild, ref buildConfig);
			SetupAddressables(@params.RemoteAddressables);
			SetupAndroidKeystore();
			SetupScenes(ref buildConfig);
			SetupPath(ref buildConfig, @params.BuildTarget);
			BuildUtils.UpdateCloudDiagnosticEnable(@params.UploadSymbolsToUnity);
			BuildAddressables(@params.DevelopmentBuild);

			AssetDatabase.Refresh();

			// Additional build options
			buildConfig.target = @params.BuildTarget;
			buildConfig.options |= BuildOptions.CompressWithLz4HC;
			buildConfig.options |= @params.AdditionalOptions;

			return BuildPipeline.BuildPlayer(buildConfig);
		}

		private static void SetupPath(ref BuildPlayerOptions buildConfig, BuildTarget buildTarget)
		{
			switch (buildTarget)
			{
				case BuildTarget.Android:
					buildConfig.locationPathName = "BlastRoyale." + (EditorUserBuildSettings.buildAppBundle ? "aab" : "apk");
					break;
				case BuildTarget.iOS:
					buildConfig.locationPathName = "BlastRoyale";
					break;
			}
		}

		private static void SetupScenes(ref BuildPlayerOptions buildConfig)
		{
			buildConfig.scenes = (from editorScene in EditorBuildSettings.scenes where editorScene.enabled select editorScene.path).ToArray();
		}

		private static void SetupDevelopmentBuild(bool isDevelopmentBuild, ref BuildPlayerOptions buildOptions)
		{
			if (isDevelopmentBuild)
			{
				buildOptions.options |= BuildOptions.Development;
			}
		}

		private static void SetupAddressables(bool remoteAddressables)
		{
			var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;

			var profileId = addressableSettings.profileSettings.GetProfileId(remoteAddressables ? "CCD" : "Default");

			AddressableAssetSettingsDefaultObject.Settings.activeProfileId = profileId;
		}

		internal static void BuildAddressables(bool enableDebugPackages)
		{
			if (!enableDebugPackages) RemoveDebugBundles();
			AddressableAssetSettings.BuildPlayerContent(out var result);
			if (!enableDebugPackages) AddDebugBundlesBack();
			if (!string.IsNullOrEmpty(result.Error))
			{
				throw new Exception(result.Error);
			}
		}

		private static bool IsDebugGroup(AddressableAssetGroup assetGroup)
		{
			return !assetGroup.ReadOnly && (assetGroup.name.EndsWith("_Dev") || assetGroup.name.StartsWith("Dev_"));
		}

		private static void RemoveDebugBundles()
		{
			foreach (var group in AddressableAssetSettingsDefaultObject.Settings.groups)
			{
				if (!IsDebugGroup(group)) continue;
				var schema = group.GetSchema<BundledAssetGroupSchema>();
				schema.IncludeInBuild = false;
				schema.IncludeAddressInCatalog = false;
				schema.IncludeLabelsInCatalog = false;
				schema.IncludeGUIDInCatalog = false;
				EditorUtility.SetDirty(schema);
			}
		}

		private static void AddDebugBundlesBack()
		{
			foreach (var group in AddressableAssetSettingsDefaultObject.Settings.groups)
			{
				if (!IsDebugGroup(group)) continue;
				var schema = group.GetSchema<BundledAssetGroupSchema>();
				schema.IncludeInBuild = true;
				schema.IncludeAddressInCatalog = true;
				schema.IncludeLabelsInCatalog = false;
				schema.IncludeGUIDInCatalog = true;
				EditorUtility.SetDirty(schema);
			}
		}

		private static void SetupBuildNumber(int buildNumber)
		{
			PlayerSettings.Android.bundleVersionCode = buildNumber;
			PlayerSettings.iOS.buildNumber = buildNumber.ToString();
		}

		[Conditional("UNITY_ANDROID")]
		private static void SetupAndroidKeystore()
		{
			PlayerSettings.Android.useCustomKeystore = true;
			PlayerSettings.Android.keystoreName =
				Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Certificates", "firstlightgames.keystore"));
			PlayerSettings.Android.keystorePass = "***REMOVED***";
			PlayerSettings.Android.keyaliasName = "blastroyale";
			PlayerSettings.Android.keyaliasPass = "***REMOVED***";
		}
	}
}