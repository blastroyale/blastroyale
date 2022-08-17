using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// This command gives the player a huge damage executing the command
	/// </summary>
	public unsafe class CheatRefillAmmoAndSpecials : CommandBase
	{
		/// <inheritdoc />
		public override void Serialize(BitStream stream)
		{
		}

		/// <inheritdoc />
		internal override void Execute(Frame f, PlayerRef playerRef)
		{
			var characterEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;
			var pc = f.Unsafe.GetPointer<PlayerCharacter>(characterEntity);

			pc->GainAmmo(f, characterEntity, FP._1);
			pc->EquipSlotWeapon(f, characterEntity, pc->CurrentWeaponSlot);
		}
	}
}