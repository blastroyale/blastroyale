using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Handles player movement and aiming direction.
	/// </summary>
	[Obsolete]
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public unsafe class PlayerMoveAction : AIAction
	{
		public AIParamFP MaxSpeedModifier;
		public AIParamFPVector3 VelocityModifier;

		public override void Update(Frame f, EntityRef e, ref AIContext aiContext)
		{
			// DEPRECATED, DID NOT DELETE BECAUSE IM A NOOB AND HATE CIRCUIT
			// Kind Regards, Gabriel
		}
	}
}