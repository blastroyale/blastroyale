using System;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.Build.Utils
{
	/// <summary>
	/// Static utility methods for use in build scripts.
	/// </summary>
	public static class BuildUtils
	{
		public const string ENV_DEV = "development";
		public const string ENV_STAGING = "staging";
		public const string ENV_PROD = "production";
		
		private const string ENVAR_BUILD_NUMBER = "FL_BUILD_NUMBER";
		private const string ENVAR_ENVIRONMENT = "FL_ENVIRONMENT";
		private const string ENVAR_DEVELOPMENT_BUILD = "FL_DEVELOPMENT_BUILD";
		
		private const string ARG_ENVIRONMENT = "-FLEnvironment";
		private const string ARG_BUILD_NUMBER = "-FLBuildNumber";
		private const string ARG_DEV_BUILD = "-FLDevelopmentBuild";

		public static string GetEnvironment()
		{
			return Environment.GetEnvironmentVariable(ENVAR_ENVIRONMENT) ?? GetCMDArgument(ARG_ENVIRONMENT) ?? "development";
		}

		public static int GetBuildNumber()
		{
			return int.Parse(Environment.GetEnvironmentVariable(ENVAR_BUILD_NUMBER) ?? GetCMDArgument(ARG_BUILD_NUMBER) ?? "0");
		}

		public static bool GetIsDevelopmentBuild()
		{
			return bool.Parse(Environment.GetEnvironmentVariable(ENVAR_DEVELOPMENT_BUILD) ?? GetCMDArgument(ARG_DEV_BUILD) ?? "true");
		}

		public static BuildTarget GetBuildTarget()
		{
			var buildTarget = BuildTarget.NoTarget;
			Debug.Log("PACO BUILD TARGET NONE");

#if UNITY_ANDROID
			buildTarget = BuildTarget.Android;
			Debug.Log("PACO BUILD TARGET ANDROID");
#elif UNITY_IOS
			Debug.Log("PACO BUILD TARGET iOS");
			buildTarget = BuildTarget.iOS;
#endif

			return buildTarget;
		}
		
		private static string GetCMDArgument(string argumentName)
		{
			string[] args = Environment.GetCommandLineArgs();
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i] == argumentName && args.Length > i + 1)
				{
					return args[i + 1];
				}
			}
			return null;
		}
	}
	
	
}