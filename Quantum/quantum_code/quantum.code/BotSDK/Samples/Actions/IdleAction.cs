using System;
using System.Runtime.InteropServices;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public partial class IdleAction : AIAction
	{
		public override unsafe void Update(Frame frame, EntityRef entity)
		{
		}
	}
}
