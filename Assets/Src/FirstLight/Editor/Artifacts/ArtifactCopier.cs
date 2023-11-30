using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FirstLight.Editor.Artifacts
{
	public static class ArtifactCopier
	{
		public static DllArtifacts QuantumDlls = new()
		{
			SourceDir = $"{Application.dataPath}/../Assets/Libs/Photon/Quantum/Assemblies/",
			Dlls = new[]
			{
				"quantum.code.dll",
				"quantum.core.dll",
				"PhotonDeterministic.dll"
			}
		};


		public static DllArtifacts GameDlls = new()
		{
			SourceDir = $"{Application.dataPath}/../Library/ScriptAssemblies/",
			Dlls = new[]
			{
				"FirstLight.DataExtensions.dll",
				"FirstLight.Game.Server.dll",
				"FirstLight.Game.dll",
				"FirstLight.Services.dll",
				"FirstLight.Models.dll",
				"PhotonQuantum.dll"
			}
		};

		public static DllArtifacts ServerSdk = new()
		{
			SourceDir = $"{Application.dataPath}/../Assets/Src/FirstLight/Server/Plugin/net48/",
			Dlls = new[]
			{
				"FirstLightServerSDK.dll"
			}
		};

		public static DllArtifacts Odin = new()
		{
			SourceDir = $"{Application.dataPath}/../Assets/Libs/Odin/Plugins/Sirenix/Assemblies/",
			Dlls = new[]
			{
				"Sirenix.OdinInspector.Attributes.dll"
			}
		};

		public static GameConfigArtifact GameConfigs = new();

		public static GameTranslationArtifact GameTranslations = new();

		public static QuantumAssetDBArtifact QuantumAssetDBArtifact = new();


		public static IArtifact[] All =
		{
			GameConfigs,
			GameTranslations,
			GameDlls,
			QuantumDlls,
			QuantumAssetDBArtifact,
			ServerSdk,
			Odin
		};

		public static async UniTask Copy(string target, params IArtifact[] artifacts)
		{
			AssureDirectoryExistence(target);

			await UniTask.WhenAll(artifacts.Select(a => a.CopyTo(target)));
		}
		
		private static void AssureDirectoryExistence(string directory)
		{
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
		}
	}
}