using System;
using System.Collections.Generic;
using System.IO;
using Facebook.Unity.Editor;
using Facebook.Unity.Settings;
using Photon.Deterministic;
using UnityEditor;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace FirstLight.Editor.Build
{
	/// <summary>
	/// Static methods and constants for setting different build configurations.
	/// </summary>
	public static class FirstLightBuildConfig
	{
		/// <summary>
		/// Scripting define that enables dev builds and SR debugger.
		/// </summary>
		public const string DevelopmentSymbol = "DEVELOPMENT_BUILD";
		
		/// <summary>
		/// Scripting define that disables all non-production features.
		/// </summary>
		public const string ReleaseSymbol = "RELEASE_BUILD";

		/// <summary>
		/// Scripting defines used in every build.
		/// </summary>
		public static readonly string[] CommonSymbols = new []
		{
			"QUANTUM_ADDRESSABLES",
			"ENABLE_PLAYFAB_BETA",
			"TextMeshPro",
		};
		
		private const string _appEnterpriseIdentifier = "com.firstlightgames.phoenixenterprise";
		private const string _appReleaseIdentifier = "com.firstlightgames.phoenix";
		private const string _firstLightEnterpriseAppleTeamId = "LR745QRAJR";
		private const string _firstLightAppleTeamId = "8UB22L9ZW7";
		private const string _appStoreProvisioningProfile = "2a3b4bba-c2eb-48be-8071-26e10fe30bb3";
		private const string _enterpriseUniversalDistributionProvisioningProfile = "60c610e2-d50d-4ae4-a72e-e84bac64eabc";
		private const int _facebookDevAppIdSelectedIndex = 1;
		private const int _facebookAppIdSelectedIndex = 0;
		
		private const AndroidArchitecture _androidReleaseTargetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

		/// <summary>
		/// Configure an android development build.
		/// </summary>
		[MenuItem("First Light Games/Simulate/Android/Local Build")]
		public static BuildPlayerOptions ConfigureAndroidLocalBuild()
		{
			var buildConfig = new BuildPlayerOptions { target = BuildTarget.Android };
			
			PlayerSettings.Android.useAPKExpansionFiles = false;

			SetScenesFromEditor(ref buildConfig);
			SetCommonAndroidConfig();
			PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
			SetLocalBuildConfig(BuildTargetGroup.Android, ref buildConfig);
			
			return buildConfig;
		}

		/// <summary>
		/// Configure an android development build.
		/// </summary>
		[MenuItem("First Light Games/Simulate/Android/Development Build")]
		public static BuildPlayerOptions ConfigureAndroidDevelopmentBuild()
		{
			var buildConfig = new BuildPlayerOptions { target = BuildTarget.Android };
			
			PlayerSettings.Android.useAPKExpansionFiles = false;

			SetScenesFromEditor(ref buildConfig);
			SetCommonAndroidConfig();
			PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
			SetDevelopmentBuildConfig(BuildTargetGroup.Android, ref buildConfig);
			
			return buildConfig;
		}
		
		/// <summary>
		/// Configure an android release build.
		/// </summary>
		[MenuItem("First Light Games/Simulate/Android/Release Build")]
		public static BuildPlayerOptions ConfigureAndroidReleaseBuild()
		{
			var buildConfig = new BuildPlayerOptions { target = BuildTarget.Android };
			
			PlayerSettings.Android.useAPKExpansionFiles = true;
			
			SetScenesFromEditor(ref buildConfig);
			SetCommonAndroidConfig();
			PlayerSettings.Android.targetArchitectures = _androidReleaseTargetArchitectures;
			SetReleaseBuildConfig(BuildTargetGroup.Android);
			
			return buildConfig;
		}
		
		/// <summary>
		/// Configure an android local build.
		/// </summary>
		[MenuItem("First Light Games/Simulate/iOS/Local Build")]
		public static BuildPlayerOptions ConfigureIosLocalBuild()
		{
			var buildConfig = new BuildPlayerOptions { target = BuildTarget.iOS };
			
			PlayerSettings.iOS.appleDeveloperTeamID = _firstLightEnterpriseAppleTeamId;
			PlayerSettings.iOS.iOSManualProvisioningProfileID = _enterpriseUniversalDistributionProvisioningProfile;
			PlayerSettings.iOS.appleEnableAutomaticSigning = true;
			
			SetScenesFromEditor(ref buildConfig);
			SetCommonIosConfig(ref buildConfig);
			SetLocalBuildConfig(BuildTargetGroup.iOS, ref buildConfig);
			
			return buildConfig;
		}

		/// <summary>
		/// Configure an ios development build.
		/// </summary>
		[MenuItem("First Light Games/Simulate/iOS/Development Build")]
		public static BuildPlayerOptions ConfigureIosDevelopmentBuild()
		{
			var buildConfig = new BuildPlayerOptions { target = BuildTarget.iOS };
			
			PlayerSettings.iOS.appleDeveloperTeamID = _firstLightEnterpriseAppleTeamId;
			PlayerSettings.iOS.iOSManualProvisioningProfileID = _enterpriseUniversalDistributionProvisioningProfile;
			PlayerSettings.iOS.appleEnableAutomaticSigning = false;
			
			SetScenesFromEditor(ref buildConfig);
			SetCommonIosConfig(ref buildConfig);
			SetDevelopmentBuildConfig(BuildTargetGroup.iOS, ref buildConfig);
			
			return buildConfig;
		}
		
		/// <summary>
		/// Configure an ios release build.
		/// </summary>
		[MenuItem("First Light Games/Simulate/iOS/Release Build")]
		public static BuildPlayerOptions ConfigureIosReleaseBuild()
		{
			var buildConfig = new BuildPlayerOptions { target = BuildTarget.iOS };
			
			PlayerSettings.iOS.appleDeveloperTeamID = _firstLightAppleTeamId;
			PlayerSettings.iOS.iOSManualProvisioningProfileID = _appStoreProvisioningProfile;
			PlayerSettings.iOS.appleEnableAutomaticSigning = false;
			
			SetScenesFromEditor(ref buildConfig);
			SetCommonIosConfig(ref buildConfig);
			SetReleaseBuildConfig(BuildTargetGroup.iOS);
			
			return buildConfig;
		}

		private static void SetCommonAndroidConfig()
		{
			PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
		}

		private static void SetCommonIosConfig(ref BuildPlayerOptions buildConfig)
		{
			if (BuildPipeline.BuildCanBeAppended(BuildTarget.iOS, "app") == CanAppendBuild.Yes)
			{
				buildConfig.options |= BuildOptions.AcceptExternalModificationsToPlayer;
			}
			
			PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Distribution;
		}

		private static void SetLocalBuildConfig(BuildTargetGroup target, ref BuildPlayerOptions buildConfig)
		{
			buildConfig.options |= BuildOptions.AutoRunPlayer;
			buildConfig.options |= BuildOptions.ConnectToHost;
			buildConfig.options |= BuildOptions.ConnectWithProfiler;
			buildConfig.options |= BuildOptions.DetailedBuildReport;
			buildConfig.options |= BuildOptions.ShowBuiltPlayer;
			buildConfig.options |= BuildOptions.SymlinkSources;

			SetDevelopmentBuildConfig(target, ref buildConfig);
		}

		private static void SetDevelopmentBuildConfig(BuildTargetGroup target, ref BuildPlayerOptions buildConfig)
		{
			EditorUserBuildSettings.development = true;
			ConfigureQuantumForDevelopment();
			buildConfig.options |= BuildOptions.Development;
			
			PlayerSettings.SetApplicationIdentifier(target, _appEnterpriseIdentifier);
			FacebookSettings.SelectedAppIndex = _facebookDevAppIdSelectedIndex;

#if UNITY_ANDROID
			ManifestMod.GenerateManifest();
#endif
		}
		
		private static void SetReleaseBuildConfig(BuildTargetGroup target)
		{
			EditorUserBuildSettings.development = false;
			ConfigureQuantumForRelease();
			PlayerSettings.SetApplicationIdentifier(target, _appReleaseIdentifier);
			FacebookSettings.SelectedAppIndex = _facebookAppIdSelectedIndex;

#if UNITY_ANDROID
			ManifestMod.GenerateManifest();
#endif
		}
		
		/// <summary>
		/// Set the correct keystore and key to sign android builds with.
		/// </summary>
		[InitializeOnLoadMethod]
		public static void SetAndroidKeystore()
		{
			PlayerSettings.Android.useCustomKeystore = true;
			PlayerSettings.Android.keystoreName = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Certificates", "phoenix.keystore"));
			PlayerSettings.Android.keystorePass = "***REMOVED***";
			PlayerSettings.Android.keyaliasName = "upload";
			PlayerSettings.Android.keyaliasPass = "JuMREKEi&B5^55mv";
		}

		private static void SetScenesFromEditor(ref BuildPlayerOptions buildPlayerOptions)
		{
			var  scenesToInclude = new List<string>();
			
			foreach (var editorScene in EditorBuildSettings.scenes)
			{
				if (!editorScene.enabled)
				{
					continue;
				}

				scenesToInclude.Add(editorScene.path);
			}

			buildPlayerOptions.scenes = scenesToInclude.ToArray();
		}
		
		/// <summary>
		/// Setups the Firebase config files to the current system build config
		/// </summary>
		[MenuItem("First Light Games/Setup/Firebase")]
		public static void SetupFirebase()
		{
			PrepareFirebase("-dev");
		}

		public static void SetScriptingDefineSymbols(string buildSymbol, BuildTargetGroup targetGroup)
		{
			var commonSymbolsLength = CommonSymbols.Length;
			var symbols = new string[commonSymbolsLength + 1];
			Array.Copy(FirstLightBuildConfig.CommonSymbols, symbols, commonSymbolsLength);
			symbols[commonSymbolsLength] = buildSymbol;
			
			PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, symbols);
		}

		public static void PrepareFirebase(string symbol)
		{
			Debug.Log($"FirstLightBuildConfig.PrepareFirebase Executing {symbol}");
			
			var environment = symbol == "RELEASE_BUILD" ? "-prod" : "-dev";
			
#if RELEASE_BUILD
			environment = "-prod";
#endif
			
			var origPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Configs");
			var destPath = Application.streamingAssetsPath;
			var iosOrig = Path.Combine(origPath, $"GoogleService-Info{environment}.plist");
			var iosDest = Path.Combine(destPath, "GoogleService-Info.plist");
			var androidOrig = Path.Combine(origPath, $"google-services{environment}.json");
			var androidDest = Path.Combine(destPath, "google-services.json");

			if (!Directory.Exists(destPath))
			{
				Directory.CreateDirectory(destPath);
			}
			
			File.Copy(iosOrig, iosDest, true);
			File.Copy(androidOrig, androidDest, true);
		}

		private static void ConfigureQuantumForDevelopment()
		{
			var guids = AssetDatabase.FindAssets($"t:{nameof(DeterministicSessionConfigAsset)}");
			Assert.IsTrue(guids.Length == 1);
			var path = AssetDatabase.GUIDToAssetPath(guids[0]);
			var deterministicConfig = AssetDatabase.LoadAssetAtPath<DeterministicSessionConfigAsset>(path);
			deterministicConfig.Config.ChecksumInterval = 1;
			deterministicConfig.Config.ChecksumCrossPlatformDeterminism = true;
			EditorUtility.SetDirty(deterministicConfig);
			AssetDatabase.SaveAssets();
		}
		
		private static void ConfigureQuantumForRelease()
		{
			var guids = AssetDatabase.FindAssets($"t:{nameof(DeterministicSessionConfigAsset)}");
			Assert.IsTrue(guids.Length == 1);
			var path = AssetDatabase.GUIDToAssetPath(guids[0]);
			var deterministicConfig = AssetDatabase.LoadAssetAtPath<DeterministicSessionConfigAsset>(path);
			deterministicConfig.Config.ChecksumInterval = 0;
			deterministicConfig.Config.ChecksumCrossPlatformDeterminism = false;
			EditorUtility.SetDirty(deterministicConfig);
			AssetDatabase.SaveAssets();
		}
	}
}
