using System;

namespace Quantum
{
	/// <summary>
	/// This decision checks if the actor's <see cref="Weapon"/> is empty
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public class IsWeaponEmpty : HFSMDecision
	{
		/// <inheritdoc />
		public override bool Decide(Frame f, EntityRef e)
		{
			return !f.TryGet<Weapon>(e, out var weapon) || weapon.Ammo == 0;
		}
	}
}