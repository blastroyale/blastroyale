using System;

namespace Quantum
{
	/// <summary>
	/// This decision checks if the actor's <see cref="Weapon"/> specials are all empty
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public unsafe class AreSpecialsEmptyDecision : HFSMDecision
	{
		/// <inheritdoc />
		public override bool Decide(Frame f, EntityRef e)
		{
			var specials = f.Unsafe.GetPointer<PlayerCharacter>(e)->WeaponSlot->Specials;

			for(var i = 0; i < specials.Length; i++)
			{
				if (f.Time > specials[i].AvailableTime)
				{
					return false;
				}
			}

			return true;
		}
	}
}