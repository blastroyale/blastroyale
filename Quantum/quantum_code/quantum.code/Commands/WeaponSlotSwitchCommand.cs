using Photon.Deterministic;
using Quantum.Systems;

namespace Quantum.Commands
{
	/// <summary>
	/// This command tries to switch to a weapon in a specified WeaponSlotIndex slot
	/// </summary>
	public unsafe class WeaponSlotSwitchCommand : CommandBase
	{
		public int WeaponSlotIndex;
		
		/// <inheritdoc />
		public override void Serialize(BitStream stream)
		{
			stream.Serialize(ref WeaponSlotIndex);
		}

		/// <inheritdoc />
		internal override void Execute(Frame f, PlayerRef playerRef)
		{
			var characterEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;
			
			// Between sending the command and receiving it, the player might have died due to the frame delay between Unity & Quantum
			if (!f.Unsafe.TryGetPointer<PlayerCharacter>(characterEntity, out var pc) ||
			    !pc->WeaponSlots[WeaponSlotIndex].Weapon.IsValid() || WeaponSlotIndex == pc->CurrentWeaponSlot)
			{
				return;
			}
			
			pc->EquipSlotWeapon(f, characterEntity, WeaponSlotIndex);
		}
	}
}