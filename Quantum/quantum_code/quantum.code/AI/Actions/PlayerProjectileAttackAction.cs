using System;
using Photon.Deterministic;
using Quantum.Systems;
using Quantum.Systems.Bots;

namespace Quantum
{
	/// <summary>
	/// This action attacks at <see cref="PlayerCharacter"/> aiming direction based on it's <see cref="Weapon"/> data
	/// and it's an projectile speed based attack
	/// </summary>
	/// <remarks>
	/// Use <see cref="PlayerRaycastAttackAction"/> if is not a projectile speed base attack
	/// </remarks>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public unsafe class PlayerProjectileAttackAction : AIAction
	{
		/// <inheritdoc />
		public override void Update(Frame f, EntityRef e, ref AIContext aiContext)
		{
			ProjectileSystem.Shoot(f, e);
		}
	}
}