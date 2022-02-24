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
			
			if (QuantumHelpers.IsDestroyed(f, characterEntity) || !f.Has<Targetable>(characterEntity) || 
			    !f.Unsafe.TryGetPointer<PlayerCharacter>(characterEntity, out var playerCharacter) ||
			    !playerCharacter->Weapons[WeaponSlotIndex].IsValid)
			{
				return;
			}
			
			playerCharacter->CurrentWeaponSlot = (ushort) WeaponSlotIndex;
			playerCharacter->EquipCurrentSlotWeapon(f, characterEntity);
		}
	}
}