using FirstLight.Game.Commands;
using Quantum;


namespace Assets.Src.FirstLight.Game.Commands.QuantumLogicCommands
{
	/// <summary>
	/// Transforms quantum logic command events to Game Logic Service command instances.
	/// The command instance must inheric IQuantumCommand to be enriched from a deterministic frame.
	/// </summary>
	public class QuantumLogicCommandFactory
	{
		public static IQuantumCommand BuildFromEvent(EventFireQuantumServerCommand ev)
		{
			if (ev.CommandType == QuantumServerCommand.EndOfGameRewards)
			{
				return new EndOfGameCalculationsCommand();
			}
			return null;
		}
	}
}
