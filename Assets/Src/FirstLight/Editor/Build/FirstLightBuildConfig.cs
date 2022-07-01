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
		/// Scripting define that disables all non-production features in a release ad hoc environment.
		/// </summary>
		public const string ReleaseSymbol = "RELEASE_BUILD";
		
		/// <summary>
		/// Scripting define the build to publish to the stores.
		/// </summary>
		public const string StoreSymbol = "STORE_BUILD";

		private const string _appEnterpriseIdentifier = "com.firstlightgames.blastroyaleenterprise";
		private const string _appReleaseIdentifier = "com.firstlightgames.blastroyale";
		private const string _firstLightEnterpriseAppleTeamId = "LR745QRAJR";
		private const string _firstLightAppleTeamId = "8UB22L9ZW7";
		private const string _appStoreProvisioningProfile = "1c16ed57-e352-4cca-8950-7e1c7ec1730d";
		private const string _enterpriseProvisioningProfile = "6573b280-9534-4ec7-83f2-b64ea455239e";
		private const string _keystoreName = "firstlightgames.keystore";
		private const string _apkExtension = ".apk";
		private const int _facebookDevAppIdSelectedIndex = 1;
		private const int _facebookAppIdSelectedIndex = 0;
		private const AndroidArchitecture _androidReleaseTargetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
		private static readonly string[] CommonSymbols = new []
		{
			"QUANTUM_ADDRESSABLES",
			"ENABLE_PLAYFAB_BETA",
			"TextMeshPro",
		};
		private static readonly string[] DebugSymbols = new []
		{
			"QUANTUM_REMOTE_PROFILER",
		};

		/// <summary>
		/// Setups the editor for Development build configuration
		/// </summary>
		[MenuItem("FLG/Configure/Development Build")]
		public static void SetupDevelopmentConfig()
		{
			PlayerSettings.Android.useAPKExpansionFiles = false;
			PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
			PlayerSettings.iOS.appleDeveloperTeamID = _firstLightEnterpriseAppleTeamId;
			PlayerSettings.iOS.iOSManualProvisioningProfileID = _enterpriseProvisioningProfile;
			PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Distribution;
			PlayerSettings.iOS.appleEnableAutomaticSigning = false;
			EditorUserBuildSettings.development = true;
			EditorUserBuildSettings.buildAppBundle = false;
			EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
			FacebookSettings.SelectedAppIndex = _facebookDevAppIdSelectedIndex;
			
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, _appEnterpriseIdentifier);
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, _appEnterpriseIdentifier);
			PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
			ConfigureQuantumForDevelopment();
			SetAndroidKeystore();
			PrepareFirebase(DevelopmentSymbol);
			SetScriptingDefineSymbols(DevelopmentSymbol, BuildTargetGroup.Android);
			SetScriptingDefineSymbols(DevelopmentSymbol, BuildTargetGroup.iOS);
			
#if UNITY_ANDROID
			ManifestMod.GenerateManifest();
#endif
		}

		/// <summary>
		/// Setups the editor for Release build configuration
		/// Release build means it is a candidate for the store but using development SKUs
		/// </summary>
		[MenuItem("FLG/Configure/Release Build")]
		public static void SetupReleaseConfig()
		{
			PlayerSettings.Android.useAPKExpansionFiles = false;
			PlayerSettings.Android.targetArchitectures = _androidReleaseTargetArchitectures;
			PlayerSettings.iOS.appleDeveloperTeamID = _firstLightEnterpriseAppleTeamId;
			PlayerSettings.iOS.iOSManualProvisioningProfileID = _enterpriseProvisioningProfile;
			PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Distribution;
			PlayerSettings.iOS.appleEnableAutomaticSigning = false;
			EditorUserBuildSettings.development = false;
			EditorUserBuildSettings.buildAppBundle = false;
			EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
			FacebookSettings.SelectedAppIndex = _facebookDevAppIdSelectedIndex;
			
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, _appEnterpriseIdentifier);
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, _appEnterpriseIdentifier);
			PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
			ConfigureQuantumForRelease();
			SetAndroidKeystore();
			PrepareFirebase(ReleaseSymbol);
			SetScriptingDefineSymbols(ReleaseSymbol, BuildTargetGroup.Android);
			SetScriptingDefineSymbols(ReleaseSymbol, BuildTargetGroup.iOS);

#if UNITY_ANDROID
			ManifestMod.GenerateManifest();
#endif
		}

		/// <summary>
		/// Setups the editor for store build configuration
		/// </summary>
		[MenuItem("FLG/Configure/Store Build")]
		public static void SetupStoreConfig()
		{
			PlayerSettings.Android.useAPKExpansionFiles = true;
			PlayerSettings.Android.targetArchitectures = _androidReleaseTargetArchitectures;
			PlayerSettings.iOS.appleDeveloperTeamID = _firstLightAppleTeamId;
			PlayerSettings.iOS.iOSManualProvisioningProfileID = _appStoreProvisioningProfile;
			PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Distribution;
			PlayerSettings.iOS.appleEnableAutomaticSigning = false;
			EditorUserBuildSettings.development = false;
			EditorUserBuildSettings.buildAppBundle = true;
			EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
			FacebookSettings.SelectedAppIndex = _facebookAppIdSelectedIndex;
			
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, _appReleaseIdentifier);
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, _appReleaseIdentifier);
			PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
			ConfigureQuantumForRelease();
			SetAndroidKeystore();
			PrepareFirebase(StoreSymbol);
			SetScriptingDefineSymbols(ReleaseSymbol, BuildTargetGroup.Android);
			SetScriptingDefineSymbols(ReleaseSymbol, BuildTargetGroup.iOS);

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
			PlayerSettings.Android.keystoreName = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Certificates", _keystoreName));
			PlayerSettings.Android.keystorePass = "***REMOVED***";
			PlayerSettings.Android.keyaliasName = "blastroyale";
			PlayerSettings.Android.keyaliasPass = "***REMOVED***";
		}
		
		/// <summary>
		/// Setups the Firebase config files to the current system build config
		/// </summary>
		[MenuItem("FLG/Setup Firebase")]
		public static void SetupFirebase()
		{
			SetupDevelopmentConfig();
		}

		/// <summary>
		/// Sets the defining symbols defined by this build in the player settings mapping list
		/// </summary>
		public static void SetScriptingDefineSymbols(string buildSymbol, BuildTargetGroup targetGroup)
		{
			var symbols = new List<string>(CommonSymbols);

			if (buildSymbol == DevelopmentSymbol)
			{
				symbols.AddRange(DebugSymbols);
			}
			
			symbols.Add(buildSymbol);
			
			PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, symbols.ToArray());
		}

		/// <summary>
		/// Requests the <see cref="BuildPlayerOptions"/> based on the given data
		/// </summary>
		public static BuildPlayerOptions GetBuildPlayerOptions(BuildTarget target, string outputPath, bool isDevelopment, 
		                                                       bool isLocalBuild = false)
		{
			if (target == BuildTarget.Android)
			{
				outputPath = Path.ChangeExtension(outputPath, _apkExtension);
			}
			
			var buildConfig = new BuildPlayerOptions
			{
				target = target,
				locationPathName = outputPath
			};

			if (isDevelopment)
			{
				buildConfig.options = BuildOptions.Development;
			}

			if (isLocalBuild)
			{
				SetLocalBuildConfig(ref buildConfig);
			}

			if (target == BuildTarget.iOS && BuildPipeline.BuildCanBeAppended(BuildTarget.iOS, "app") == CanAppendBuild.Yes)
			{
				buildConfig.options |= BuildOptions.AcceptExternalModificationsToPlayer;
			}

			SetScenesFromEditor(ref buildConfig);

			return buildConfig;
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

		private static void SetLocalBuildConfig(ref BuildPlayerOptions buildConfig)
		{
			buildConfig.options |= BuildOptions.AutoRunPlayer;
			buildConfig.options |= BuildOptions.ConnectWithProfiler;
			buildConfig.options |= BuildOptions.DetailedBuildReport;
		}

		private static void ConfigureQuantumForDevelopment()
		{
			var guids = AssetDatabase.FindAssets($"t:{nameof(DeterministicSessionConfigAsset)}");
			var path = AssetDatabase.GUIDToAssetPath(guids[0]);
			var deterministicConfig = AssetDatabase.LoadAssetAtPath<DeterministicSessionConfigAsset>(path);
			
			deterministicConfig.Config.ChecksumInterval = 60;
			deterministicConfig.Config.ChecksumCrossPlatformDeterminism = true;
			
			EditorUtility.SetDirty(deterministicConfig);
			AssetDatabase.SaveAssets();
		}
		
		private static void ConfigureQuantumForRelease()
		{
			var guids = AssetDatabase.FindAssets($"t:{nameof(DeterministicSessionConfigAsset)}");
			var path = AssetDatabase.GUIDToAssetPath(guids[0]);
			var deterministicConfig = AssetDatabase.LoadAssetAtPath<DeterministicSessionConfigAsset>(path);
			
			deterministicConfig.Config.ChecksumInterval = 0;
			deterministicConfig.Config.ChecksumCrossPlatformDeterminism = false;
			
			EditorUtility.SetDirty(deterministicConfig);
			AssetDatabase.SaveAssets();
		}
		
		private static void PrepareFirebase(string symbol)
		{
			Debug.Log($"FirstLightBuildConfig.PrepareFirebase Executing {symbol}");
			
			var environment = symbol == StoreSymbol ? "-prod" : "-dev";
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
	}
}
