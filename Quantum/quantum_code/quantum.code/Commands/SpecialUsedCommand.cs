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

			if (HasCharge(playerCharacter) && special->TryActivate(f, characterEntity, AimInput, SpecialIndex))
			{
				switch (SpecialIndex)
				{
					case 0 : playerCharacter->WeaponSlots[playerCharacter->CurrentWeaponSlot].Special1Charges--;
						break;
					case 1 : playerCharacter->WeaponSlots[playerCharacter->CurrentWeaponSlot].Special2Charges--;
						break;
				}
			}
		}

		/// <summary>
		/// Tests if the current special has enough charge to be triggered
		/// </summary>
		private bool HasCharge(PlayerCharacter* playerCharacter)
		{
			return GetSpecialChargesByIndex(SpecialIndex, playerCharacter) > 0;
		}

		/// <summary>
		/// Gets the number of charges of an Special by its index
		/// </summary>
		private int GetSpecialChargesByIndex(int specialIndex, PlayerCharacter* playerCharacter)
		{
			return specialIndex switch
			{ 
				0 => playerCharacter->WeaponSlots[playerCharacter->CurrentWeaponSlot].Special1Charges,
				1 => playerCharacter->WeaponSlots[playerCharacter->CurrentWeaponSlot].Special2Charges,
				_ => 0
			};
		}
	}
}