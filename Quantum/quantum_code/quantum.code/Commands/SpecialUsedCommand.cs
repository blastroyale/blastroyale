using System.Diagnostics;
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
			var special = playerCharacter->GetSpecial(SpecialIndex);

			if (playerCharacter->GetSpecialCharges(SpecialIndex) > 0 && 
			    f.Time >= playerCharacter->GetSpecialAvailableTime(SpecialIndex) &&
			    special.TryActivate(f, characterEntity, AimInput, SpecialIndex))
			{
				var weaponSlot = playerCharacter->WeaponSlot;

				switch (SpecialIndex)
				{
					case 0:
					{
						weaponSlot.Special1AvailableTime = f.Time + special.Cooldown;
						break;
					}
					case 1:
					{
						weaponSlot.Special2AvailableTime = f.Time + special.Cooldown;
						break;
					}
				}

				playerCharacter->WeaponSlots[playerCharacter->CurrentWeaponSlot] = weaponSlot;
			}
		}
	}
}