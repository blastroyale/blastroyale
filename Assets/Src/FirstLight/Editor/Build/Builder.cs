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
		public static void SetSymbols()
		{
			var arguments = Environment.GetCommandLineArgs();
			
			if (!FirstLightBuildUtil.TryGetBuildSymbolFromCommandLineArgs(out var buildSymbol, arguments))
			{
				Debug.LogError("Could not get build symbol from command line args.");
				EditorApplication.Exit(1);
			}

			BuildTargetGroup targetGroup;
#if UNITY_ANDROID
			targetGroup = BuildTargetGroup.Android;
#elif UNITY_IOS
			targetGroup = BuildTargetGroup.iOS;
#else
			Debug.LogError("No builds configured for this platform.");
			EditorApplication.Exit(1);
			return;
#endif

			FirstLightBuildConfig.PrepareFirebase(buildSymbol);
		
			FirstLightBuildConfig.SetScriptingDefineSymbols(buildSymbol, targetGroup);
		}

		/// <summary>
		/// Execute method for Jenkins builds
		/// </summary>
		public static void JenkinsBuild()
		{
#if !UNITY_ANDROID && !UNITY_IOS
			Debug.LogError("No builds configured for this platform.");
			EditorApplication.Exit(1);
			return;
#endif
			
			var arguments = Environment.GetCommandLineArgs();
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
			
			BuildReport buildReport;
#if UNITY_ANDROID
			VersionEditorUtils.TrySetAndroidVersionCodeFromCommandLineArgs(arguments);
			buildReport = BuildForAndroid(buildSymbol, fileName);
#elif UNITY_IOS
			VersionEditorUtils.TrySetIosBuildNumberFromCommandLineArgs(arguments);
			buildReport = BuildForIos(buildSymbol, fileName);
#endif

			LogBuildReport(buildReport);

			if (buildReport.summary.result != BuildResult.Succeeded)
			{
				EditorApplication.Exit(1);
			}
		}

		private static BuildReport BuildForAndroid(string buildSymbol, string outputPath)
		{
			Debug.Log($"Build Defined Symbols {PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android)}");
			
			BuildReport buildReport;
			switch (buildSymbol)
			{
				case FirstLightBuildConfig.DevelopmentSymbol:
				{
					buildReport = FirstLightBuildJobs.BuildAndroidDevelopment(outputPath);
					break;
				}
				case FirstLightBuildConfig.ReleaseSymbol:
				{
					buildReport = FirstLightBuildJobs.BuildAndroidRelease(outputPath);
					break;
				}
				default:
				{
					Debug.LogError($"Unrecognised build symbol: {buildSymbol}");
					EditorApplication.Exit(1);
					return null;
				}
			}

			return buildReport;
		}
	
		private static BuildReport BuildForIos(string buildSymbol, string outputPath)
		{
			Debug.Log($"Build Defined Symbols {PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS)}");
			
			BuildReport buildReport;
			switch (buildSymbol)
			{
				case FirstLightBuildConfig.DevelopmentSymbol:
				{
					buildReport = FirstLightBuildJobs.BuildIosDevelopment(outputPath);
					break;
				}
				case FirstLightBuildConfig.ReleaseSymbol:
				{
					buildReport = FirstLightBuildJobs.BuildIosRelease(outputPath);
					break;
				}
				default:
				{
					Debug.LogError($"Unrecognised build symbol: {buildSymbol}");
					EditorApplication.Exit(1);
					return null;
				}
			}

			return buildReport;
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