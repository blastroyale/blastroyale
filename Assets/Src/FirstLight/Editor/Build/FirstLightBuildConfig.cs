using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		/// Scripting define that enables development builds and allows to profile the game in Xcode and other external tools
		/// Uses development SKU
		/// </summary>
		public const string LocalSymbol = "LOCAL_BUILD";

		/// <summary>
		/// Scripting define that enables dev builds
		/// Uses development SKU
		/// </summary>
		public const string DevelopmentSymbol = "DEVELOPMENT_BUILD";

		/// <summary>
		/// Scripting define the build to publish to the NON-stores distribution channels.
		/// This build is signed and is possible to download and run from anywhere the link is available.
		/// Uses development SKU
		/// </summary>
		public const string ReleaseSymbol = "RELEASE_BUILD";

		/// <summary>
		/// Scripting define the build to publish to the stores distribution channels.
		/// This build is signed and is only possible to run the client after downloading from the store.
		/// Uses Production SKU
		/// </summary>
		public const string StoreSymbol = "STORE_BUILD";

		public const string CCDEnvironmentDev = "development";
		public const string CCDEnvironmentStaging = "staging";
		public const string CCDEnvironmentProd = "production";

		private const string _appIdentifier = "com.firstlightgames.blastroyale";
		private const string _firstLightAppleTeamId = "8UB22L9ZW7";
		private const string _distributionProvisioningProfile = "1c16ed57-e352-4cca-8950-7e1c7ec1730d";
		private const string _developmentProvisioningProfile = "f97b416e-d6ba-4676-9d98-974eac657421";
		private const string _adHocProvisioningProfile = "1369c3c2-10f9-4093-923d-19e50d0f3e52";
		private const string _keystoreName = "firstlightgames.keystore";
		private const string _apkExtension = ".apk";
		private const string _aabExtension = ".aab";
		private const AndroidArchitecture _targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

		private static readonly string InfoLogLevelSymbol = "LOG_LEVEL_INFO";
		private static readonly string VerboseLogLevelSymbol = "LOG_LEVEL_VERBOSE";

		private static readonly string[] CommonSymbols =
		{
			"QUANTUM_ADDRESSABLES",
			"ENABLE_PLAYFAB_BETA",
			"TextMeshPro",
		};

		private static readonly string[] DebugSymbols =
		{
			"QUANTUM_REMOTE_PROFILER",
			VerboseLogLevelSymbol
		};

		private static readonly string[] StoreReleaseSymbols =
		{
			"DISABLE_SRDEBUGGER",
			InfoLogLevelSymbol
		};

		private static readonly string[] ReleaseSymbols =
		{
			"DISABLE_SRDEBUGGER",
			VerboseLogLevelSymbol
		};


		/// <summary>
		/// <inheritdoc cref="DevelopmentSymbol"/>
		/// </summary>
		/// <remarks>
		/// Setups the editor for Development build configuration
		/// </remarks>
		[MenuItem("FLG/Configure/Development Build")]
		public static void SetupDevelopmentConfig()
		{
			PlayerSettings.Android.useAPKExpansionFiles = false;
			PlayerSettings.Android.targetArchitectures = _targetArchitectures;
			PlayerSettings.iOS.appleDeveloperTeamID = _firstLightAppleTeamId;
			PlayerSettings.iOS.iOSManualProvisioningProfileID = _developmentProvisioningProfile;
			PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Development;
			PlayerSettings.iOS.appleEnableAutomaticSigning = false;
			EditorUserBuildSettings.development = true;
			EditorUserBuildSettings.buildAppBundle = false;
			EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

			VersionEditorUtils.SetAndSaveInternalVersion(false);
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, _appIdentifier);
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, _appIdentifier);
			PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
			ConfigureQuantumForDevelopment();
			SetAndroidKeystore();
			PrepareFirebase(DevelopmentSymbol);
		}

		/// <summary>
		/// <inheritdoc cref="ReleaseSymbol"/>
		/// </summary>
		/// <remarks>
		/// Setups the editor for Release build configuration
		/// </remarks>
		[MenuItem("FLG/Configure/ReleaseConfig")]
		public static void SetupReleaseConfig()
		{
			PlayerSettings.Android.useAPKExpansionFiles = false;
			PlayerSettings.Android.targetArchitectures = _targetArchitectures;
			PlayerSettings.iOS.appleDeveloperTeamID = _firstLightAppleTeamId;
			PlayerSettings.iOS.iOSManualProvisioningProfileID = _adHocProvisioningProfile;
			PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Distribution;
			PlayerSettings.iOS.appleEnableAutomaticSigning = false;
			EditorUserBuildSettings.development = false;
			EditorUserBuildSettings.buildAppBundle = false;
			EditorUserBuildSettings.exportAsGoogleAndroidProject = false;


			VersionEditorUtils.SetAndSaveInternalVersion(false);
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, _appIdentifier);
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, _appIdentifier);
			PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
			ConfigureQuantumForRelease();
			SetAndroidKeystore();
			PrepareFirebase(ReleaseSymbol);
		}

		/// <summary>
		/// <inheritdoc cref="StoreSymbol"/>
		/// </summary>
		/// <remarks>
		/// Setups the editor for Store build configuration
		/// </remarks>
		[MenuItem("FLG/Configure/Store Build")]
		public static void SetupStoreConfig()
		{
			PlayerSettings.Android.useAPKExpansionFiles = true;
			PlayerSettings.Android.targetArchitectures = _targetArchitectures;
			PlayerSettings.iOS.appleDeveloperTeamID = _firstLightAppleTeamId;
			PlayerSettings.iOS.iOSManualProvisioningProfileID = _distributionProvisioningProfile;
			PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Distribution;
			PlayerSettings.iOS.appleEnableAutomaticSigning = false;
			PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
			PlayerSettings.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
			EditorUserBuildSettings.development = false;
			EditorUserBuildSettings.buildAppBundle = true;
			EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

			VersionEditorUtils.SetAndSaveInternalVersion(true);
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, _appIdentifier);
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, _appIdentifier);
			PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
			ConfigureQuantumForRelease();
			SetAndroidKeystore();
			PrepareFirebase(StoreSymbol);
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
		public static void SetScriptingDefineSymbols(BuildTargetGroup targetGroup, params string[] buildSymbols)
		{
			var symbols = new List<string>(CommonSymbols);

			if (buildSymbols.Contains(DevelopmentSymbol))
			{
				symbols.AddRange(DebugSymbols);
			}

			if (buildSymbols.Contains(StoreSymbol))
			{
				symbols.AddRange(StoreReleaseSymbols);
			}

			if (buildSymbols.Contains(ReleaseSymbol))
			{
				symbols.AddRange(ReleaseSymbols);
			}

			symbols.AddRange(buildSymbols);

			PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, symbols.ToArray());
		}

		/// <summary>
		/// Requests the <see cref="BuildPlayerOptions"/> based on the given data
		/// </summary>
		public static BuildPlayerOptions GetBuildPlayerOptions(BuildTarget target, string outputPath, string symbol)
		{
			var isLocalBuild = symbol == LocalSymbol;
			var isStoreBuild = !isLocalBuild && symbol == StoreSymbol;

			if (target == BuildTarget.Android)
			{
				outputPath = Path.ChangeExtension(outputPath, isStoreBuild ? _aabExtension : _apkExtension);
			}

			var buildConfig = new BuildPlayerOptions
			{
				target = target,
				locationPathName = outputPath
			};

			if (isLocalBuild || symbol == DevelopmentSymbol)
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

			buildConfig.options |= BuildOptions.CompressWithLz4HC;

			SetScenesFromEditor(ref buildConfig);

			return buildConfig;
		}

		private static void SetScenesFromEditor(ref BuildPlayerOptions buildPlayerOptions)
		{
			var scenesToInclude = new List<string>();

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