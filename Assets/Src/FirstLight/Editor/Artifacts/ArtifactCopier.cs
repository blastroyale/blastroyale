using System.IO;
using UnityEngine;

namespace FirstLight.Editor.Artifacts
{
	public static class ArtifactCopier
	{
		public static readonly DllArtifacts QuantumDlls = new ()
		{
			SourceDir = $"{Application.dataPath}/../Assets/Libs/Photon/Quantum/Assemblies/",
			Dlls = new[]
			{
				"quantum.code.dll",
				"quantum.core.dll",
				"PhotonDeterministic.dll"
			}
		};

		public static readonly DllArtifacts GameDlls = new ()
		{
			SourceDir = $"{Application.dataPath}/../Library/ScriptAssemblies/",
			Dlls = new[]
			{
				"FirstLight.DataExtensions.dll",
				"FirstLight.Game.Server.dll",
				"FirstLight.Game.dll",
				"FirstLight.Services.dll",
				"FirstLight.Models.dll",
				"UniTask.dll",
				"PhotonQuantum.dll"
			}
		};

		public static readonly DllArtifacts ServerSdk = new ()
		{
			SourceDir = $"{Application.dataPath}/../Assets/Src/FirstLight/Server/Plugin/net48/",
			Dlls = new[]
			{
				"FirstLightServerSDK.dll"
			}
		};

		public static readonly DllArtifacts Odin = new ()
		{
			SourceDir = $"{Application.dataPath}/../Assets/Libs/Odin/Plugins/Sirenix/Assemblies/",
			Dlls = new[]
			{
				"Sirenix.OdinInspector.Attributes.dll"
			}
		};

		public static readonly GameConfigArtifact GameConfigs = new ();

		public static readonly GameTranslationArtifact GameTranslations = new ();

		public static readonly QuantumAssetDBArtifact QuantumAssetDBArtifact = new ();


		public static readonly IArtifact[] All =
		{
			GameConfigs,
			GameTranslations,
			GameDlls,
			QuantumDlls,
			QuantumAssetDBArtifact,
			ServerSdk,
			Odin
		};

		public static void Copy(string target, params IArtifact[] artifacts)
		{
			AssureDirectoryExistence(target);

			foreach (var artifact in artifacts)
			{
				artifact.CopyTo(target);
			}
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