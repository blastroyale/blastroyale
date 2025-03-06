using FirstLight.Editor.Build.Utils;
using FirstLight.Game.Utils;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;

namespace FirstLight.Editor.Build
{
	public static class BuilderLocalHelper
	{
		[MenuItem("FLG/Build/Local/Addressables/No Debug", false, 10)]
		public static void BuildAddressablesNoDebug()
		{
			Builder.BuildAddressables(false);
		}

		[MenuItem("FLG/Build/Local/Addressables/Debug", false, 11)]
		public static void BuildAddressablesYesDebug()
		{
			Builder.BuildAddressables(true);
		}

		[MenuItem("FLG/Build/Local/Develop", false, 40)]
		public static void DefaultDevelopBuild()
		{
			BaseDevelopBuild();
		}

		[MenuItem("FLG/Build/Local/Develop - Faster Build", false, 41)]
		public static void DevelopBuildFastIL2CPP()
		{
			var target = BuildUtils.GetBuildTarget();
			var targetGroup = BuildPipeline.GetBuildTargetGroup(target);
			var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
			PlayerSettings.SetIl2CppCodeGeneration(namedBuildTarget, Il2CppCodeGeneration.OptimizeSpeed);
			BaseDevelopBuild();
		}

		[MenuItem("FLG/Build/Local/Develop - Clean Build", false, 42)]
		public static void DevelopBuildCleanBuild()
		{
			AddressableAssetSettings.CleanPlayerContent();
			BaseDevelopBuild(BuildOptions.CleanBuildCache);
		}

		[MenuItem("FLG/Build/Local/Staging - Store", false, 55)]
		public static void StoreBuild()
		{
			BuildUtils.OverwriteEnvironment = FLEnvironment.STAGING;
			EditorUserBuildSettings.buildAppBundle = true;
			PlayerSettings.Android.splitApplicationBinary = true;
			AddressableAssetSettings.CleanPlayerContent();
			Builder.Build(new Builder.BuildParameters()
			{
				BuildNumber = 1,
				BuildTarget = BuildUtils.GetBuildTarget(),
				DevelopmentBuild = false,
				RemoteAddressables = true,
				UploadSymbolsToUnity = true,
			});
		}

		[MenuItem("FLG/Build/Local/Development - Store", false, 54)]
		public static void StoreBuildDevelopment()
		{
			BuildUtils.OverwriteEnvironment = FLEnvironment.DEVELOPMENT;
			EditorUserBuildSettings.buildAppBundle = true;
			PlayerSettings.Android.splitApplicationBinary = true;
			AddressableAssetSettings.CleanPlayerContent();
			Builder.Build(new Builder.BuildParameters()
			{
				BuildNumber = 1,
				BuildTarget = BuildUtils.GetBuildTarget(),
				DevelopmentBuild = false,
				RemoteAddressables = true,
				UploadSymbolsToUnity = true,
			});
		}

		public static void BaseDevelopBuild(BuildOptions additionalFlags = BuildOptions.None)
		{
			Builder.Build(new Builder.BuildParameters()
			{
				BuildNumber = 1,
				BuildTarget = BuildUtils.GetBuildTarget(),
				DevelopmentBuild = true,
				RemoteAddressables = true,
				AdditionalOptions = BuildOptions.ConnectWithProfiler | BuildOptions.AutoRunPlayer | BuildOptions.WaitForPlayerConnection |
					additionalFlags,
				UploadSymbolsToUnity = false,
			});
		}
	}
}