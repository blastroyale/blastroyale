using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace FirstLight.Editor.Build
{
	/// <summary>
	/// Collection of build calls for specific build configurations
	/// </summary>
	public static class FirstLightBuildJobs
	{
		private const string _apkExtension = ".apk";
		
		public static BuildReport BuildAndroidLocal(string outputPath)
		{
			var buildConfig = FirstLightBuildConfig.ConfigureAndroidLocalBuild();
			outputPath = Path.ChangeExtension(outputPath, _apkExtension);
			buildConfig.locationPathName = outputPath;
			return BuildPipeline.BuildPlayer(buildConfig);
		}
		
		public static BuildReport BuildAndroidDevelopment(string outputPath)
		{
			var buildConfig = FirstLightBuildConfig.ConfigureAndroidDevelopmentBuild();
			outputPath = Path.ChangeExtension(outputPath, _apkExtension);
			buildConfig.locationPathName = outputPath;
			return BuildPipeline.BuildPlayer(buildConfig);
		}
		
		public static BuildReport BuildAndroidRelease(string outputPath)
		{
			var buildConfig = FirstLightBuildConfig.ConfigureAndroidReleaseBuild();
			outputPath = Path.ChangeExtension(outputPath, _apkExtension);
			buildConfig.locationPathName = outputPath;
			return BuildPipeline.BuildPlayer(buildConfig);
		}
		
		public static BuildReport BuildAndroidStore(string outputPath)
		{
			var buildConfig = FirstLightBuildConfig.ConfigureAndroidStoreBuild();
			outputPath = Path.ChangeExtension(outputPath, _apkExtension);
			buildConfig.locationPathName = outputPath;
			return BuildPipeline.BuildPlayer(buildConfig);
		}
		
		public static BuildReport BuildIosLocal(string outputPath)
		{
			var buildConfig = FirstLightBuildConfig.ConfigureIosLocalBuild();
			buildConfig.locationPathName = outputPath;
			return BuildPipeline.BuildPlayer(buildConfig);
		}
		
		public static BuildReport BuildIosDevelopment(string outputPath)
		{
			var buildConfig = FirstLightBuildConfig.ConfigureIosDevelopmentBuild();
			buildConfig.locationPathName = outputPath;
			return BuildPipeline.BuildPlayer(buildConfig);
		}
		
		public static BuildReport BuildIosRelease(string outputPath)
		{
			var buildConfig = FirstLightBuildConfig.ConfigureIosReleaseBuild();
			buildConfig.locationPathName = outputPath;
			return BuildPipeline.BuildPlayer(buildConfig);
		}
		
		public static BuildReport BuildIosStore(string outputPath)
		{
			var buildConfig = FirstLightBuildConfig.ConfigureIosStoreBuild();
			buildConfig.locationPathName = outputPath;
			return BuildPipeline.BuildPlayer(buildConfig);
		}
	}
}