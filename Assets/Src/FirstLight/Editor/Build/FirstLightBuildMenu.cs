using UnityEditor;

namespace FirstLight.Editor.Build
{
    /// <summary>
    /// Adds editor menu options to start specific build configurations.
    /// </summary>
    public static class FirstLightBuildMenu
    {
        private const string _defaultAppName = "phoenix";
        private const string _apkExtension = "apk";
        
        [MenuItem("First Light Games/Build/Android/Local Build")]
        private static void BuildAndroidLocal()
        {
            var outputPath = GetAndroidOutputPath();
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return;
            }
            
            FirstLightBuildConfig.PrepareFirebase(FirstLightBuildConfig.DevelopmentSymbol);
		
            FirstLightBuildConfig.SetScriptingDefineSymbols(FirstLightBuildConfig.DevelopmentSymbol, BuildTargetGroup.iOS);
            
            FirstLightBuildJobs.BuildAndroidDevelopment(outputPath);
        }
        
        [MenuItem("First Light Games/Build/Android/Development Build")]
        private static void BuildAndroidDevelopment()
        {
            var outputPath = GetAndroidOutputPath();
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return;
            }
            
            FirstLightBuildConfig.PrepareFirebase(FirstLightBuildConfig.DevelopmentSymbol);
		
            FirstLightBuildConfig.SetScriptingDefineSymbols(FirstLightBuildConfig.DevelopmentSymbol, BuildTargetGroup.iOS);
            
            FirstLightBuildJobs.BuildAndroidDevelopment(outputPath);
        }
        
        [MenuItem("First Light Games/Build/Android/Release Build")]
        public static void BuildAndroidRelease()
        {
            var outputPath = GetAndroidOutputPath();
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return;
            }
            
            FirstLightBuildConfig.PrepareFirebase(FirstLightBuildConfig.ReleaseSymbol);
		
            FirstLightBuildConfig.SetScriptingDefineSymbols(FirstLightBuildConfig.ReleaseSymbol, BuildTargetGroup.iOS);
            
            FirstLightBuildJobs.BuildAndroidRelease(outputPath);
        }
        
        [MenuItem("First Light Games/Build/iOS/Local Build")]
        private static void BuildIosLocal()
        {
            var outputPath = GetIosOutputPath();
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return;
            }

            FirstLightBuildJobs.BuildIosLocal(outputPath);
        }
        
        [MenuItem("First Light Games/Build/iOS/Development Build")]
        private static void BuildIosDevelopment()
        {
            var outputPath = GetIosOutputPath();
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return;
            }

            FirstLightBuildConfig.PrepareFirebase(FirstLightBuildConfig.DevelopmentSymbol);
		
            FirstLightBuildConfig.SetScriptingDefineSymbols(FirstLightBuildConfig.DevelopmentSymbol, BuildTargetGroup.iOS);
            
            FirstLightBuildJobs.BuildIosDevelopment(outputPath);
        }
        
        [MenuItem("First Light Games/Build/iOS/Release Build")]
        public static void BuildIosRelease()
        {
            var outputPath = GetIosOutputPath();
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return;
            }
            
            FirstLightBuildConfig.PrepareFirebase(FirstLightBuildConfig.ReleaseSymbol);
		
            FirstLightBuildConfig.SetScriptingDefineSymbols(FirstLightBuildConfig.ReleaseSymbol, BuildTargetGroup.iOS);
            
            FirstLightBuildJobs.BuildIosRelease(outputPath);
        }

        private static string GetAndroidOutputPath()
        {
            return EditorUtility.SaveFilePanel(string.Empty,
                string.Empty,
                _defaultAppName,
                _apkExtension);
        }
        
        private static string GetIosOutputPath()
        {
            return EditorUtility.SaveFilePanel(string.Empty,
                string.Empty,
                _defaultAppName,
                string.Empty);
        }
    }
}