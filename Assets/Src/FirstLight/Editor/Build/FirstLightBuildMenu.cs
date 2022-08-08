using UnityEditor;

namespace FirstLight.Editor.Build
{
	/// <summary>
	/// Adds editor menu options to start specific build configurations.
	/// </summary>
	public static class FirstLightBuildMenu
	{
		private const string _defaultAppName = "blast_royale";
		
		[MenuItem("FLG/Build/Android/Local Build")]
		private static void BuildAndroidLocal()
		{
			var outputPath = EditorUtility.SaveFilePanel(string.Empty, string.Empty, _defaultAppName, string.Empty);
			
			if (string.IsNullOrWhiteSpace(outputPath))
			{
				return;
			}
			
			FirstLightBuildConfig.SetupDevelopmentConfig();
			FirstLightBuildConfig.SetScriptingDefineSymbols(BuildTargetGroup.Android, FirstLightBuildConfig.DevelopmentSymbol);

			var options = FirstLightBuildConfig.GetBuildPlayerOptions(BuildTarget.Android, outputPath, FirstLightBuildConfig.LocalSymbol);
			
			BuildPipeline.BuildPlayer(options);
		}
		
		[MenuItem("FLG/Build/Android/Development Build")]
		private static void BuildAndroidDevelopment()
		{
			var outputPath = EditorUtility.SaveFilePanel(string.Empty, string.Empty, _defaultAppName, string.Empty);
			
			if (string.IsNullOrWhiteSpace(outputPath))
			{
				return;
			}
			
			FirstLightBuildConfig.SetupDevelopmentConfig();
			FirstLightBuildConfig.SetScriptingDefineSymbols(BuildTargetGroup.Android, FirstLightBuildConfig.DevelopmentSymbol);
			
			var options = FirstLightBuildConfig.GetBuildPlayerOptions(BuildTarget.Android, outputPath, FirstLightBuildConfig.DevelopmentSymbol);
			
			BuildPipeline.BuildPlayer(options);
		}
		
		[MenuItem("FLG/Build/Android/Release Build")]
		public static void BuildRelease()
		{
			var outputPath = EditorUtility.SaveFilePanel(string.Empty, string.Empty, _defaultAppName, string.Empty);
			
			if (string.IsNullOrWhiteSpace(outputPath))
			{
				return;
			}
			
			FirstLightBuildConfig.SetupReleaseConfig();
			FirstLightBuildConfig.SetScriptingDefineSymbols(BuildTargetGroup.Android, FirstLightBuildConfig.ReleaseSymbol);
			
			var options = FirstLightBuildConfig.GetBuildPlayerOptions(BuildTarget.Android, outputPath, FirstLightBuildConfig.ReleaseSymbol);
			
			BuildPipeline.BuildPlayer(options);
		}
		
		[MenuItem("FLG/Build/Android/Store Build")]
		public static void BuildAndroidStore()
		{
			var outputPath = EditorUtility.SaveFilePanel(string.Empty, string.Empty, _defaultAppName, string.Empty);
			
			if (string.IsNullOrWhiteSpace(outputPath))
			{
				return;
			}
			
			FirstLightBuildConfig.SetupStoreConfig();
			FirstLightBuildConfig.SetScriptingDefineSymbols(BuildTargetGroup.Android, FirstLightBuildConfig.StoreSymbol);
			
			var options = FirstLightBuildConfig.GetBuildPlayerOptions(BuildTarget.Android, outputPath, FirstLightBuildConfig.StoreSymbol);
			
			BuildPipeline.BuildPlayer(options);
		}
		
		[MenuItem("FLG/Build/iOS/Local Build")]
		private static void BuildIosLocal()
		{
			var outputPath = EditorUtility.SaveFilePanel(string.Empty, string.Empty, _defaultAppName, string.Empty);
			
			if (string.IsNullOrWhiteSpace(outputPath))
			{
				return;
			}
			
			FirstLightBuildConfig.SetupDevelopmentConfig();
			FirstLightBuildConfig.SetScriptingDefineSymbols(BuildTargetGroup.iOS, FirstLightBuildConfig.DevelopmentSymbol);

			var options = FirstLightBuildConfig.GetBuildPlayerOptions(BuildTarget.iOS, outputPath, FirstLightBuildConfig.LocalSymbol);

			PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Automatic;
			PlayerSettings.iOS.appleEnableAutomaticSigning = true;
			
			BuildPipeline.BuildPlayer(options);
		}
		
		[MenuItem("FLG/Build/iOS/Development Build")]
		private static void BuildIosDevelopment()
		{
			var outputPath = EditorUtility.SaveFilePanel(string.Empty, string.Empty, _defaultAppName, string.Empty);
			
			if (string.IsNullOrWhiteSpace(outputPath))
			{
				return;
			}

			FirstLightBuildConfig.SetupDevelopmentConfig();
			FirstLightBuildConfig.SetScriptingDefineSymbols(BuildTargetGroup.iOS, FirstLightBuildConfig.DevelopmentSymbol);
			
			var options = FirstLightBuildConfig.GetBuildPlayerOptions(BuildTarget.iOS, outputPath, FirstLightBuildConfig.DevelopmentSymbol);
			
			BuildPipeline.BuildPlayer(options);
		}
		
		[MenuItem("FLG/Build/iOS/Release Build")]
		public static void BuildIosRelease()
		{
			var outputPath = EditorUtility.SaveFilePanel(string.Empty, string.Empty, _defaultAppName, string.Empty);
			
			if (string.IsNullOrWhiteSpace(outputPath))
			{
				return;
			}
			
			FirstLightBuildConfig.SetupReleaseConfig();
			FirstLightBuildConfig.SetScriptingDefineSymbols(BuildTargetGroup.iOS, FirstLightBuildConfig.ReleaseSymbol);
			
			var options = FirstLightBuildConfig.GetBuildPlayerOptions(BuildTarget.iOS, outputPath, FirstLightBuildConfig.ReleaseSymbol);
			
			BuildPipeline.BuildPlayer(options);
		}
		
		[MenuItem("FLG/Build/iOS/Store Build")]
		public static void BuildIosStore()
		{
			var outputPath = EditorUtility.SaveFilePanel(string.Empty, string.Empty, _defaultAppName, string.Empty);
			
			if (string.IsNullOrWhiteSpace(outputPath))
			{
				return;
			}
			
			FirstLightBuildConfig.SetupStoreConfig();
			FirstLightBuildConfig.SetScriptingDefineSymbols(BuildTargetGroup.iOS, FirstLightBuildConfig.StoreSymbol);
			
			var options = FirstLightBuildConfig.GetBuildPlayerOptions(BuildTarget.iOS, outputPath, FirstLightBuildConfig.StoreSymbol);
			
			BuildPipeline.BuildPlayer(options);
		}
	}
}