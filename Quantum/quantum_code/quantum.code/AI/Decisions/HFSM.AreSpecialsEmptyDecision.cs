using System;

namespace Quantum
{
	/// <summary>
	/// This decision checks if the actor's <see cref="Weapon"/> specials are all empty
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public class AreSpecialsEmptyDecision : HFSMDecision
	{
		/// <inheritdoc />
		public override bool Decide(Frame f, EntityRef e)
		{
			var playerCharacter = f.Get<PlayerCharacter>(e);
			var currentWeaponSlot = playerCharacter.WeaponSlots[playerCharacter.CurrentWeaponSlot];

			return currentWeaponSlot.Special1AvailableTime > f.Time
			       || currentWeaponSlot.Special2AvailableTime > f.Time;
		}
	}
}