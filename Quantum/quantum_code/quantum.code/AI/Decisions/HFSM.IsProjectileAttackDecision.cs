using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This decision checks if the player weapon attack is with a projectile
	/// </summary>
	/// TODO: Remove and save the data in the circuit blackboard
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public partial class IsProjectileAttackDecision : HFSMDecision
	{
		/// <inheritdoc />
		public override unsafe bool Decide(Frame f, EntityRef e)
		{
			var weaponId = f.Unsafe.GetPointer<PlayerCharacter>(e)->CurrentWeapon.GameId;
			
			return f.WeaponConfigs.GetConfig(weaponId).IsProjectile;
		}
	}
}