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
#if DEBUG
			var characterEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;
			var pc = f.Unsafe.GetPointer<PlayerCharacter>(characterEntity);
			var pi = f.Unsafe.GetPointer<PlayerInventory>(characterEntity);
			var stats = f.Unsafe.GetPointer<Stats>(characterEntity);

			// Replenish Special's charges
			for (var i = 0; i < pc->WeaponSlots.Length; i++)
			{
				pi->Specials[0].Charges = 1;
				pi->Specials[0].AvailableTime = f.Time;
				pi->Specials[1].Charges = 1;
				pi->Specials[1].AvailableTime = f.Time;
			}

			stats->GainAmmoPercent(f, characterEntity, FP._1);
			pc->EquipSlotWeapon(f, characterEntity, pc->CurrentWeaponSlot);
#else
			Log.Error($"Trying to use Cheat command {this.GetType().Name} in Release build of Quantum Code");
#endif
		}
	}
}