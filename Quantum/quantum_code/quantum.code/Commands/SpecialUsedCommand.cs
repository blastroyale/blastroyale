using Photon.Deterministic;
using Quantum.Systems;

namespace Quantum.Commands
{
	/// <summary>
	/// This command tries to use a special with the SpecialIndex index
	/// </summary>
	public unsafe class SpecialUsedCommand : CommandBase
	{
		public FPVector2 AimInput;
		public int SpecialIndex;
		
		/// <inheritdoc />
		public override void Serialize(BitStream stream)
		{
			stream.Serialize(ref AimInput);
			stream.Serialize(ref SpecialIndex);
		}

		/// <inheritdoc />
		internal override void Execute(Frame f, PlayerRef playerRef)
		{
			var characterEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(characterEntity);
			var special = playerCharacter->Specials.GetPointer(SpecialIndex);
			var currentWeaponSlot = playerCharacter->CurrentWeaponSlot;
			
			if (special->IsValid || !special->IsSpecialAvailable(f) || HasCharge(playerCharacter))
			{
				if (special->TryActivate(f, characterEntity, AimInput, SpecialIndex))
				{
					playerCharacter->WeaponSlots[playerCharacter->CurrentWeaponSlot].SpecialsCharges[SpecialIndex].Charges--;
				}
			}
		}

		/// <summary>
		/// Tests if the current special has enough charge to be triggered
		/// </summary>
		private bool HasCharge(PlayerCharacter* playerCharacter)
		{
			return playerCharacter->WeaponSlots[playerCharacter->CurrentWeaponSlot].SpecialsCharges[SpecialIndex].Charges > 0;
		}
	}
}