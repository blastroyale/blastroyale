using System;
using System.Diagnostics;
using System.IO;
using FirstLight.Game.Utils;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FirstLight.Editor.Build.Utils
{
	/// <summary>
	/// Static utility methods for use in build scripts.
	/// </summary>
	public static class BuildUtils
	{
		private const string ENVAR_BUILD_NUMBER = "FL_BUILD_NUMBER";
		private const string ENVAR_ENVIRONMENT = "FL_ENVIRONMENT";
		private const string ENVAR_DEVELOPMENT_BUILD = "FL_DEVELOPMENT_BUILD";
		private const string ENVAR_REMOTE_ADDRESSABLES = "FL_REMOTE_ADDRESSABLES";

		private const string ARG_ENVIRONMENT = "-FLEnvironment";
		private const string ARG_BUILD_NUMBER = "-FLBuildNumber";
		private const string ARG_DEV_BUILD = "-FLDevelopmentBuild";
		private const string ARG_REMOTE_ADDRESSABLES = "-FLRemoteAddressables";
		public static FLEnvironment.Definition OverwriteEnvironment;

		/// <summary>
		/// The version override for the addressable catalog.
		/// </summary>
		[UsedImplicitly]
		public static string AddressableVersionOverride =>
			GetEnvironment() == FLEnvironment.DEVELOPMENT.Name ? DateTime.UtcNow.ToString("yyyy.MM.dd.hh.mm.ss") : PlayerSettings.bundleVersion;

		/// <summary>
		/// Gets the current UCS / Backend environment, defaults to development.
		/// </summary>
		public static string GetEnvironment()
		{
			if (!string.IsNullOrEmpty(OverwriteEnvironment.Name)) return OverwriteEnvironment.Name;
			return Environment.GetEnvironmentVariable(ENVAR_ENVIRONMENT) ?? GetCMDArgument(ARG_ENVIRONMENT) ?? FLEnvironment.DEVELOPMENT.Name;
		}

		/// <summary>
		/// Gets the current build number. In UCS DevOps this is ADDED to the DevOps build number.
		/// </summary>
		public static int GetBuildNumber()
		{
			return int.Parse(Environment.GetEnvironmentVariable(ENVAR_BUILD_NUMBER) ?? GetCMDArgument(ARG_BUILD_NUMBER) ?? "1");
		}

		/// <summary>
		/// Gets whether the current build should be a development build.
		/// </summary>
		public static bool GetIsDevelopmentBuild()
		{
			return bool.Parse(Environment.GetEnvironmentVariable(ENVAR_DEVELOPMENT_BUILD) ?? GetCMDArgument(ARG_DEV_BUILD) ?? "true");
		}

		/// <summary>
		/// If we should use remote or bundled addressables.
		/// </summary>
		public static bool GetUseRemoteAddressables()
		{
			return bool.Parse(Environment.GetEnvironmentVariable(ENVAR_REMOTE_ADDRESSABLES) ?? GetCMDArgument(ARG_REMOTE_ADDRESSABLES) ?? "false");
		}

		public static BuildTarget GetBuildTarget()
		{
			var buildTarget = BuildTarget.NoTarget;
#if UNITY_ANDROID
			buildTarget = BuildTarget.Android;
#elif UNITY_IOS
			buildTarget = BuildTarget.iOS;
#endif

			return buildTarget;
		}

		private static string GetCMDArgument(string argumentName)
		{
			var args = Environment.GetCommandLineArgs();
			for (var i = 0; i < args.Length; i++)
			{
				if (args[i] == argumentName && args.Length > i + 1)
				{
					return args[i + 1];
				}
			}

			return null;
		}

		[UsedImplicitly]
		[Conditional("UNITY_CLOUD_BUILD")]
		public static void PreExport(UnityEngine.CloudBuild.BuildManifestObject manifest)
		{
			// In UCS DevOps we add the ENV build number to the cloud build number
			var buildNumber = manifest.GetValue<int>("buildNumber") + GetBuildNumber();
			Debug.Log("Setting build number to " + buildNumber);
			PlayerSettings.Android.bundleVersionCode = buildNumber;
			PlayerSettings.iOS.buildNumber = buildNumber.ToString();
		}

		public static void UpdateCloudDiagnosticEnable(bool enabled)
		{
			var projectDir = Path.GetDirectoryName(Application.dataPath) ?? "";
			var assetFile = Path.Combine(projectDir, "ProjectSettings", "UnityConnectSettings.asset");
			if (!File.Exists(assetFile))
			{
				Debug.LogError($"CloudBuildSettingsHelper._UpdateCloudDiagnosticEnable({enabled}) -> file {assetFile} does not exists");
				return;
			}

			var lines = File.ReadAllLines(assetFile);
			var isCrashReporting = false;
			for (var i = 0; i < lines.Length; i++)
			{
				var line = lines[i];
				if (isCrashReporting && line is "    m_Enabled: 1" or "    m_Enabled: 0")
				{
					var isEnabled = line == "    m_Enabled: 1";
					if (isEnabled != enabled)
					{
						lines[i] = enabled ? "    m_Enabled: 1" : "    m_Enabled: 0";
						File.WriteAllLines(assetFile, lines);
					}

					break;
				}

				if (line == "  CrashReportingSettings:")
				{
					isCrashReporting = true;
				}
			}
		}
	}
}