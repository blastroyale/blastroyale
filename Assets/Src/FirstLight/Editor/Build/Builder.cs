using System;
using System.Text;
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
		/// Sets the symbols for the Unity build
		/// </summary>
		public static void ConfigureBuild()
		{
			var arguments = Environment.GetCommandLineArgs();
			
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
		/// Execute method for Jenkins builds
		/// </summary>
		public static void JenkinsBuild()
		{
			var buildTarget = BuildTarget.NoTarget;
			var arguments = Environment.GetCommandLineArgs();
			
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

			PlayerSettings.SplashScreen.show = false;
			PlayerSettings.SplashScreen.showUnityLogo = false;
			
			AddressableAssetSettings.BuildPlayerContent();
			
			var options = FirstLightBuildConfig.GetBuildPlayerOptions(buildTarget, fileName, buildSymbol);
			var buildReport = BuildPipeline.BuildPlayer(options);
			
			LogBuildReport(buildReport);

			if (buildReport.summary.result != BuildResult.Succeeded)
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