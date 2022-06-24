using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// This command gives the player a huge amount of HP executing the command
	/// </summary>
	public unsafe class CheatMakeLocalPlayerSuperToughCommand : CommandBase
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
			var healthMultiplier = FP._1000;
			
			var healthModifier = new Modifier
			{
				Id = modifierId,
				Type = StatType.Health,
				Power = healthMultiplier,
				Duration = FP.MaxValue,
				StartTime = FP._0,
				IsNegative = false
			};
			
			stats->AddModifier(f, healthModifier);
			stats->SetCurrentHealthPercentage(f, characterEntity, characterEntity, FP._1);
		}
	}
}