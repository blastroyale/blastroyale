using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using I2.Loc;
using Quantum.Editor;
using UnityEngine;

namespace FirstLight.Editor.Artifacts
{
	public class QuantumAssetDBArtifact : IArtifact
	{
		public UniTask CopyTo(string directory)
		{
			AssetDBGeneration.Export(directory + "assetDatabase.json");
			Debug.Log("Exported Quantum asset database");
			return UniTask.CompletedTask;
		}
	}
}