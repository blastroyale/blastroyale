using System;
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
#if DEBUG
			var entity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;
			var stats = f.Unsafe.GetPointer<Stats>(entity);
			var healthModifier = new Modifier
			{
				Id = ++f.Global->ModifierIdCount,
				Type = StatType.Health,
				OpType = OperationType.Add,
				Power = FP._1000,
				Duration = FP.MaxValue,
				StartTime = FP._0,
				IsNegative = false
			};
			
			stats->AddModifier(f, entity, healthModifier);
			stats->GainShield(f, entity, 2000);
			var spell = new Spell {PowerAmount = uint.MaxValue};
			stats->GainHealth(f, entity, &spell);
#else
		Log.Error($"Trying to use Cheat command {this.GetType().Name} in Release build of Quantum Code");
#endif
		}
	}
}