using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Handles player movement while parachuting in BR mode.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public unsafe class ParachuteDropAction : AIAction
	{
		public override void Update(Frame f, EntityRef e)
		{
			var kcc = f.Unsafe.GetPointer<CharacterController3D>(e);
			kcc->Move(f, e, FPVector3.Zero);
		}
	}
}