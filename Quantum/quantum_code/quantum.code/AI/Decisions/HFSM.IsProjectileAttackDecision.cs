using System;
using Photon.Deterministic;
using Quantum.Systems;

namespace Quantum
{
	/// <summary>
	/// This decision checks if the player weapon attack is with a projectile
	/// </summary>
	/// TODO: Remove and save the data in the circuit blackboard
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
		GenerateAssetResetMethod = false)]
	public class IsProjectileAttackDecision : HFSMDecision
	{
		/// <inheritdoc />
		public override bool Decide(Frame f, EntityRef e, ref AIContext aiContext)
		{
			return true;
		}
	}
}