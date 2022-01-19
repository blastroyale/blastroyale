using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This decision checks if the player weapon attack is with a projectile
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public partial class IsProjectileAttackDecision : HFSMDecision
	{
		public AIBlackboardValueKey TimeToTrueState;
		
		/// <inheritdoc />
		public override unsafe bool Decide(Frame f, EntityRef e)
		{
			return f.Get<Weapon>(e).ProjectileSpeed > FP._0;
		}
	}
}