using System;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Editor.Artifacts;
using FirstLight.Editor.EditorTools;
using Photon.Realtime;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FirstLight.Editor.Build
{
	/// <summary>
	/// Entry point for batch mode build calls from Jenkins.
	/// </summary>
	public static class Builder
	{
		/// <summary>
		/// Exports the necessary backend dlls & configurations to the correct folders
		/// for building & running our logic service & quantum server
		/// </summary>
		public static void ConfigureServer()
		{
			BackendMenu.MoveBackendDlls();
			BackendMenu.ExportQuantumAssets();
		}

		public static void SetBasicPlayerSettings()
		{
			// Include graphics apis so device can pick best case
			PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, true);
			PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, true);

			// Always build using master IL2CPP for best performance
			PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, Il2CppCompilerConfiguration.Master);
			PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.iOS, Il2CppCompilerConfiguration.Master);

			// Smaller GC sweeps to avoid lag spikes
			PlayerSettings.gcIncremental = true;

			// Faster
			PlayerSettings.colorSpace = ColorSpace.Gamma;
		}

		/// <summary>
		/// Sets the symbols for the Unity build
		/// </summary>
		public static void ConfigureBuild(string[] arguments)
		{
			if (!FirstLightBuildUtil.TryGetBuildSymbolFromCommandLineArgs(out var buildSymbol, arguments))
			{
				Debug.LogError("Could not get build symbol from command line args.");
				EditorApplication.Exit(1);
			}

			if (!FirstLightBuildUtil.TryGetBuildServerSymbolFromCommandLineArgs(out var serverSymbol, arguments))
			{
				Debug.LogError("Could not get the server symbol from command line args.");
				EditorApplication.Exit(1);
			}

			VersionEditorUtils.TrySetBuildNumberFromCommandLineArgs(arguments);
			FirstLightBuildConfig.SetScriptingDefineSymbols(BuildTargetGroup.Android, buildSymbol, serverSymbol);
			FirstLightBuildConfig.SetScriptingDefineSymbols(BuildTargetGroup.iOS, buildSymbol, serverSymbol);

			switch (buildSymbol)
			{
				case FirstLightBuildConfig.DevelopmentSymbol:
				{
					FirstLightBuildConfig.SetupDevelopmentConfig();
					break;
				}
				case FirstLightBuildConfig.ReleaseSymbol:
				{
					FirstLightBuildConfig.SetupReleaseConfig();
					break;
				}
				case FirstLightBuildConfig.StoreSymbol:
				{
					FirstLightBuildConfig.SetupStoreConfig();
					break;
				}
				default:
				{
					Debug.LogError($"Unrecognised build symbol: {buildSymbol}");
					EditorApplication.Exit(1);
					break;
				}
			}
		}

		/// <summary>
		/// Combines the configure and build steps
		/// </summary>
		public static void AzureBuild()
		{
			var args = Environment.GetCommandLineArgs();
			ConfigureBuild(args);
			JenkinsBuild(args);
		}

		[MenuItem("FLG/Build/Store Azure Build")]
		public static async void EditorBuild()
		{
			var args = "-flBuildSymbol STORE_BUILD -flBuildServer TESTNET_SERVER -flBuildNumber 3000 -flBuildFileName app".Split(" ");
			ConfigureBuild(args);
			JenkinsBuild(args, false);
		}


		/// <summary>
		/// Execute method for Jenkins builds
		/// </summary>
		public static void JenkinsBuild(string[] arguments, bool quit = true)
		{
			var buildTarget = BuildTarget.NoTarget;

#if UNITY_ANDROID
			buildTarget = BuildTarget.Android;
#elif UNITY_IOS
			buildTarget = BuildTarget.iOS;
#else
			Debug.LogError("No builds configured for this platform.");
			EditorApplication.Exit(1);
			return;
#endif

			if (!FirstLightBuildUtil.TryGetBuildSymbolFromCommandLineArgs(out var buildSymbol, arguments))
			{
				Debug.LogError("Could not get build symbol from command line args.");
				EditorApplication.Exit(1);
			}

			if (!FirstLightBuildUtil.TryGetBuildFileNameFromCommandLineArgs(out var fileName, arguments))
			{
				Debug.LogError("Could not get app file name from command line args.");
				EditorApplication.Exit(1);
			}
			
			// Copy Dlls to a folder that will be publish as a pipeline artifact
			ArtifactCopier.Copy($"{Application.dataPath}/../BuildArtifacts/", ArtifactCopier.All);

			PlayerSettings.SplashScreen.show = false;
			PlayerSettings.SplashScreen.showUnityLogo = false;

			// Search all generic implementations to pre-compile them with IL2CPP
			PlayerSettings.SetAdditionalIl2CppArgs("--generic-virtual-method-iterations=10");

			AddressableAssetSettings.BuildPlayerContent();

			var options = FirstLightBuildConfig.GetBuildPlayerOptions(buildTarget, fileName, buildSymbol);
			var buildReport = BuildPipeline.BuildPlayer(options);

			LogBuildReport(buildReport);

			if (buildReport.summary.result != BuildResult.Succeeded && quit)
			{
				EditorApplication.Exit(1);
			}
		}

		private static void LogBuildReport(BuildReport buildReport)
		{
			var stringBuilder = new StringBuilder();
			foreach (var step in buildReport.steps)
			{
				stringBuilder.AppendLine($"Build Step {step.depth} - {step.name} : {step.duration}");

				foreach (var stepMessage in step.messages)
				{
					if (stepMessage.type != LogType.Log && stepMessage.type != LogType.Warning)
					{
						stringBuilder.AppendLine($"[{stepMessage.type}] {stepMessage.content}");
					}
				}
			}

			Debug.Log($"Build Result: {buildReport.summary.result.ToString()}\n " +
				$"Errors {buildReport.summary.totalErrors}\n " +
				$"Size {buildReport.summary.totalSize}\n " +
				$"Duration {buildReport.summary.totalTime} Ended {buildReport.summary.buildEndedAt} \n" +
				$"{stringBuilder}");
		}
	}
}