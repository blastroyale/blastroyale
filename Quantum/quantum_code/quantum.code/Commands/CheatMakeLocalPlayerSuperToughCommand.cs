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
			var entity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;
			var stats = f.Unsafe.GetPointer<Stats>(entity);
			var healthModifier = new Modifier
			{
				Id = ++f.Global->ModifierIdCount,
				Type = StatType.Health,
				Power = FP._1000,
				Duration = FP.MaxValue,
				StartTime = FP._0,
				IsNegative = false
			};
			
			stats->AddModifier(f, entity, healthModifier);
			stats->GainShield(f, entity, int.MaxValue);
			stats->GainHealth(f, entity, new Spell {PowerAmount = uint.MaxValue});
		}
	}
}