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
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(characterEntity);
			
			if (!playerCharacter->Weapons[WeaponSlotIndex].IsValid)
			{
				return;
			}
			
			playerCharacter->EquipSlotWeapon(f, characterEntity, WeaponSlotIndex);
		}
	}
}