using Quantum.Editor;
using UnityEngine;

namespace FirstLight.Editor.Artifacts
{
	public class QuantumAssetDBArtifact : IArtifact
	{
		public void CopyTo(string directory)
		{
			AssetDBGeneration.Export(directory + "assetDatabase.json");
			Debug.Log("Exported Quantum asset database");
		}
	}
}