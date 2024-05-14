using System;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace FirstLight.Editor.Build.Utils
{
	/// <summary>
	/// Static utility methods for use in build scripts.
	/// </summary>
	public static class BuildUtils
	{
		// TODO: This should be somewhere else - need to unify environment with backend service.
		public const string ENV_DEV = "development";
		public const string ENV_STAGING = "staging";
		public const string ENV_COMMUNITY = "community";
		public const string ENV_PROD = "production";

		private const string ENVAR_BUILD_NUMBER = "FL_BUILD_NUMBER";
		private const string ENVAR_ENVIRONMENT = "FL_ENVIRONMENT";
		private const string ENVAR_DEVELOPMENT_BUILD = "FL_DEVELOPMENT_BUILD";

		private const string ARG_ENVIRONMENT = "-FLEnvironment";
		private const string ARG_BUILD_NUMBER = "-FLBuildNumber";
		private const string ARG_DEV_BUILD = "-FLDevelopmentBuild";

		/// <summary>
		/// The version with dashes instead of dots, used for Addressables.
		/// </summary>
		[UsedImplicitly]
		public static string AddressableBadge => PlayerSettings.bundleVersion.Replace(".", "_");

		/// <summary>
		/// The version override for the addressable catalog.
		/// </summary>
		[UsedImplicitly]
		public static string AddressableVersionOverride => 
			GetEnvironment() == ENV_DEV ? DateTime.UtcNow.ToString("yyyy.MM.dd.hh.mm.ss") : PlayerSettings.bundleVersion;

		/// <summary>
		/// Gets the current UCS / Backend environment, defaults to development.
		/// </summary>
		public static string GetEnvironment()
		{
			return Environment.GetEnvironmentVariable(ENVAR_ENVIRONMENT) ?? GetCMDArgument(ARG_ENVIRONMENT) ?? "development";
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
	}
}