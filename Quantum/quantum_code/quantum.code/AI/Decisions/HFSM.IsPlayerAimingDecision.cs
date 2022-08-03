using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This decision checks if the player has aiming at something
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public partial class IsPlayerAimingDecision : HFSMDecision
	{
		/// <inheritdoc />
		public override unsafe bool Decide(Frame f, EntityRef e)
		{
			return f.Get<AIBlackboardComponent>(e).GetBoolean(f, Constants.IsAimingKey);
		}
	}
}