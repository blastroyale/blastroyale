using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using FirstLight.Editor.Artifacts;
using FirstLight.Editor.Build.Utils;
using FirstLight.Game.Utils;
using I2.Loc;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FirstLight.Editor.Build
{
	/// <summary>
	/// Stuff we want to do AFTER every build.
	/// </summary>
	public class FirstLightBuildProcess : IPostprocessBuildWithReport, IPreprocessBuildWithReport
	{
		public int callbackOrder => 0;

		public void OnPreprocessBuild(BuildReport report)
		{
			Debug.Log("FirstLightBuildPostProcess.OnPostprocessBuild OnPreprocessBuild");

			var environment = BuildUtils.GetEnvironment();

			PrepareFirebase(environment);
			VersionEditorUtils.SetAndSaveInternalVersion(environment);
			GenerateEnvironment(environment);
			ConfigureQuantum();

			// Probably not needed but why not
			AssetDatabase.Refresh();
		}

		public void OnPostprocessBuild(BuildReport report)
		{
			Debug.Log("FirstLightBuildPostProcess.OnPostprocessBuild Executing");
			WriteBuildPropertiesFile();
			ConfigureXcode(report.summary.outputPath);
			ArtifactCopier.Copy($"{Application.dataPath}/../BuildArtifacts/", ArtifactCopier.All);
		}

		/// <summary>
		/// Write a properties file containing useful information that an external process
		/// (jenkins build job) can use.
		/// </summary>
		private static void WriteBuildPropertiesFile()
		{
			const string FILE_NAME = "build-output.properties";

			var lines = new List<string>();
			var serializedVersionData = VersionEditorUtils.LoadVersionDataSerializedSync();
			var versionData = JsonUtility.FromJson<VersionUtils.VersionData>(serializedVersionData);
			var internalVersion = VersionUtils.FormatInternalVersion(versionData);
			var internalVersionFilename = new StringBuilder(internalVersion);
			var invalidChars = Path.GetInvalidFileNameChars();
			var appIdentifier = PlayerSettings.applicationIdentifier;
			var split = appIdentifier.Split('.');
			var shortAppIdentifier = split[^1];
			var androidVersionCode = PlayerSettings.Android.bundleVersionCode;
			var iOSVersionCode = PlayerSettings.iOS.buildNumber;
			var obbName = $"main.{androidVersionCode.ToString()}.{appIdentifier}.obb";
			var filePath = Path.Combine(Application.dataPath, "..", FILE_NAME);

			internalVersionFilename.Replace('/', '_');
			internalVersionFilename.Replace('\\', '_');

			foreach (var invalidChar in invalidChars)
			{
				internalVersionFilename.Replace(invalidChar, '_');
			}

			lines.Add($"FL_EXTERNAL_VERSION={PlayerSettings.bundleVersion}");
			lines.Add($"FL_INTERNAL_VERSION={internalVersion}");
			lines.Add($"FL_INTERNAL_VERSION_FILENAME={internalVersionFilename}");
			lines.Add($"FL_APP_ID={appIdentifier}");
			lines.Add($"FL_APP_ID_SHORT={shortAppIdentifier}");
			lines.Add($"FL_ANDROID_VERSION_CODE={androidVersionCode.ToString()}");
			lines.Add($"FL_OBB_NAME={obbName}");
			lines.Add($"FL_IOS_VERSION_CODE={iOSVersionCode}");

			Debug.Log($"Writing build properties file: {filePath}");
			Debug.Log(string.Join("\n", lines));

			File.WriteAllLines(filePath, lines, Encoding.ASCII);
		}

		private static void ConfigureQuantum()
		{
#if !DEVELOPMENT_BUILD
			Debug.Log("IS NOT DEVELOPMENT BUILD");

			var guids = AssetDatabase.FindAssets($"t:{nameof(DeterministicSessionConfigAsset)}");
			var path = AssetDatabase.GUIDToAssetPath(guids[0]);
			var deterministicConfig = AssetDatabase.LoadAssetAtPath<DeterministicSessionConfigAsset>(path);

			deterministicConfig.Config.ChecksumInterval = 0;
			deterministicConfig.Config.ChecksumCrossPlatformDeterminism = false;

			EditorUtility.SetDirty(deterministicConfig);
			AssetDatabase.SaveAssets();
#endif
		}

		private static void PrepareFirebase(string environment)
		{
			var suffix = environment == BuildUtils.ENV_PROD ? "-prod" : "-dev";
			var origPath = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Configs");
			var destPath = Application.streamingAssetsPath;
			var iosOrig = Path.Combine(origPath, $"GoogleService-Info{suffix}.plist");
			var iosDest = Path.Combine(destPath, "GoogleService-Info.plist");
			var androidOrig = Path.Combine(origPath, $"google-services{suffix}.json");
			var androidDest = Path.Combine(destPath, "google-services.json");

			if (!Directory.Exists(destPath))
			{
				Directory.CreateDirectory(destPath);
			}

			File.Copy(iosOrig, iosDest, true);
			File.Copy(androidOrig, androidDest, true);
		}

		/// <summary>
		/// Generates the version CS file for the Unity Cloud environment.
		/// </summary>
		private static void GenerateEnvironment(string environment)
		{
			var path = Path.Combine(Application.dataPath, "Src", "FirstLight", "Game", "Utils", "FLEnvironment.cs");
			var content = File.ReadAllText(path).Replace(
				" = GetCurrentEditorEnvironment();",
				$" = {environment.ToUpperInvariant()};"
			);

			File.WriteAllText(path, content);
		}

		[Conditional("UNITY_IOS")]
		private static void ConfigureXcode(string pathToXcode)
		{
			var projectPath = UnityEditor.iOS.Xcode.PBXProject.GetPBXProjectPath(pathToXcode);
			var schemePath = pathToXcode + "/Unity-iPhone.xcodeproj/xcshareddata/xcschemes/Unity-iPhone.xcscheme";
			var plistPath = pathToXcode + "/Info.plist";
			var pbxProject = new UnityEditor.iOS.Xcode.PBXProject();
			var scheme = new UnityEditor.iOS.Xcode.XcScheme();
			var plist = new UnityEditor.iOS.Xcode.PlistDocument();

			pbxProject.ReadFromFile(projectPath);

			var mainTargetGuid = pbxProject.GetUnityMainTargetGuid();
			var frameworkTargetGuid = pbxProject.GetUnityFrameworkTargetGuid();

			scheme.ReadFromFile(schemePath);
			scheme.SetDebugExecutable(false);
			scheme.WriteToFile(schemePath);

			plist.ReadFromFile(plistPath);
			plist.root.SetString("NSUserTrackingUsageDescription", ScriptLocalization.General.ATTDescription);
			plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
			plist.WriteToFile(plistPath);

			pbxProject.SetBuildProperty(frameworkTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
			pbxProject.SetBuildProperty(mainTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
			pbxProject.SetBuildProperty(mainTargetGuid, "SWIFT_VERSION", "5.1");

			// Xcode 15 fix
			pbxProject.AddBuildProperty(frameworkTargetGuid, "OTHER_LDFLAGS", "-ld64");

			// Disable bitcode
			pbxProject.SetBuildProperty(mainTargetGuid, "ENABLE_BITCODE", "NO");
			pbxProject.SetBuildProperty(frameworkTargetGuid, "ENABLE_BITCODE", "NO");

			pbxProject.WriteToFile(projectPath);
		}
	}
}