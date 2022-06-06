using UnityEditor;

namespace FirstLight.Editor.Build
{
	/// <summary>
	/// Adds editor menu options to start specific build configurations.
	/// </summary>
	public static class FirstLightBuildMenu
	{
		private const string _defaultAppName = "blast_royale";
		private const string _apkExtension = "apk";
		
		[MenuItem("First Light Games/Build/Android/Local Build")]
		private static void BuildAndroidLocal()
		{
			var outputPath = GetAndroidOutputPath();
			
			if (string.IsNullOrWhiteSpace(outputPath))
			{
				return;
			}

			var options = FirstLightBuildConfig.GetBuildPlayerOptions(BuildTarget.Android, outputPath, true, true);
			
			FirstLightBuildConfig.SetupDevelopmentConfig();
			BuildPipeline.BuildPlayer(options);
		}
		
		[MenuItem("First Light Games/Build/Android/Development Build")]
		private static void BuildAndroidDevelopment()
		{
			var outputPath = GetAndroidOutputPath();
			if (string.IsNullOrWhiteSpace(outputPath))
			{
				return;
			}
			
			var options = FirstLightBuildConfig.GetBuildPlayerOptions(BuildTarget.Android, outputPath, true);
			
			FirstLightBuildConfig.SetupDevelopmentConfig();
			BuildPipeline.BuildPlayer(options);
		}
		
		[MenuItem("First Light Games/Build/Android/Release Build")]
		public static void BuildAndroidRelease()
		{
			var outputPath = GetAndroidOutputPath();
			if (string.IsNullOrWhiteSpace(outputPath))
			{
				return;
			}
			
			var options = FirstLightBuildConfig.GetBuildPlayerOptions(BuildTarget.Android, outputPath, false);
			
			FirstLightBuildConfig.SetupReleaseConfig();
			BuildPipeline.BuildPlayer(options);
		}
		
		[MenuItem("First Light Games/Build/Android/Store Build")]
		public static void BuildAndroidStore()
		{
			var outputPath = GetAndroidOutputPath();
			
			if (string.IsNullOrWhiteSpace(outputPath))
			{
				return;
			}
			
			var options = FirstLightBuildConfig.GetBuildPlayerOptions(BuildTarget.Android, outputPath, false);
			
			FirstLightBuildConfig.SetupStoreConfig();
			BuildPipeline.BuildPlayer(options);
		}
		
		[MenuItem("First Light Games/Build/iOS/Local Build")]
		private static void BuildIosLocal()
		{
			var outputPath = GetIosOutputPath();
			
			if (string.IsNullOrWhiteSpace(outputPath))
			{
				return;
			}

			var options = FirstLightBuildConfig.GetBuildPlayerOptions(BuildTarget.iOS, outputPath, true, true);
			
			FirstLightBuildConfig.SetupDevelopmentConfig();
			BuildPipeline.BuildPlayer(options);
		}
		
		[MenuItem("First Light Games/Build/iOS/Development Build")]
		private static void BuildIosDevelopment()
		{
			var outputPath = GetIosOutputPath();
			
			if (string.IsNullOrWhiteSpace(outputPath))
			{
				return;
			}

			var options = FirstLightBuildConfig.GetBuildPlayerOptions(BuildTarget.iOS, outputPath, true);
			
			FirstLightBuildConfig.SetupDevelopmentConfig();
			BuildPipeline.BuildPlayer(options);
		}
		
		[MenuItem("First Light Games/Build/iOS/Release Build")]
		public static void BuildIosRelease()
		{
			var outputPath = GetIosOutputPath();
			
			if (string.IsNullOrWhiteSpace(outputPath))
			{
				return;
			}
			
			var options = FirstLightBuildConfig.GetBuildPlayerOptions(BuildTarget.iOS, outputPath, false);
			
			FirstLightBuildConfig.SetupReleaseConfig();
			BuildPipeline.BuildPlayer(options);
		}
		
		[MenuItem("First Light Games/Build/iOS/Store Build")]
		public static void BuildIosStore()
		{
			var outputPath = GetIosOutputPath();
			
			if (string.IsNullOrWhiteSpace(outputPath))
			{
				return;
			}
			
			var options = FirstLightBuildConfig.GetBuildPlayerOptions(BuildTarget.iOS, outputPath, false);
			
			FirstLightBuildConfig.SetupStoreConfig();
			BuildPipeline.BuildPlayer(options);
		}

		private static string GetAndroidOutputPath()
		{
			return EditorUtility.SaveFilePanel(string.Empty, string.Empty, _defaultAppName, _apkExtension);
		}
		
		private static string GetIosOutputPath()
		{
			return EditorUtility.SaveFilePanel(string.Empty, string.Empty, _defaultAppName, string.Empty);
		}
	}
}