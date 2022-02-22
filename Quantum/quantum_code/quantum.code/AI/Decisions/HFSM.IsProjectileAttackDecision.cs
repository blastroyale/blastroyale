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
		/// <inheritdoc />
		public override unsafe bool Decide(Frame f, EntityRef e)
		{
			var weaponId = f.Get<PlayerCharacter>(e).GetCurrentWeapon().GameId;
			
			return f.WeaponConfigs.GetConfig(weaponId).ProjectileSpeed > FP._0;
		}
	}
}