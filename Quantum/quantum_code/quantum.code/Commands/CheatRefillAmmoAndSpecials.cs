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

			// Replenish Special's charges
			for (var i = 0; i < pc->WeaponSlots.Length; i++)
			{
				pc->WeaponSlots[i].Specials[0].Charges = 1;
				pc->WeaponSlots[i].Specials[0].AvailableTime = f.Time;
				pc->WeaponSlots[i].Specials[1].Charges = 1;
				pc->WeaponSlots[i].Specials[1].AvailableTime = f.Time;
			}

			pc->GainAmmo(f, characterEntity, FP._1);
			pc->EquipSlotWeapon(f, characterEntity, pc->CurrentWeaponSlot);
		}
	}
}