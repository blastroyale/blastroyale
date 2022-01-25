using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// This command gives the player a huge damage executing the command
	/// </summary>
	public unsafe class CheatMakeLocalPlayerBigDamagerCommand : CommandBase
	{
		/// <inheritdoc />
		public override void Serialize(BitStream stream)
		{
		}

		/// <inheritdoc />
		internal override void Execute(Frame f, PlayerRef playerRef)
		{
			var characterEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;
			var stats = f.Unsafe.GetPointer<Stats>(characterEntity);
			
			var modifierId = ++f.Global->ModifierIdCount;
			
			var powerModifier = new Modifier
			{
				Id = modifierId,
				Type = StatType.Power,
				Power = FP._1000,
				Duration = FP.MaxValue,
				EndTime = FP.MaxValue,
				IsNegative = false
			};
			
			stats->AddModifier(f, powerModifier);
			
			f.Unsafe.GetPointer<Weapon>(characterEntity)->GainAmmo(100);
		}
	}
}