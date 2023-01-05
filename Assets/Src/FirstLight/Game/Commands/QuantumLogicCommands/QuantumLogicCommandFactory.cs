using FirstLight.Game.Commands;
using Quantum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Src.FirstLight.Game.Commands.QuantumLogicCommands
{
	/// <summary>
	/// Transforms quantum logic command events to Game Logic Service command instances.
	/// The command instance must inheric IQuantumCommand to be enriched from a deterministic frame.
	/// </summary>
	public class QuantumLogicCommandFactory
	{
		private readonly static Dictionary<QuantumServerCommand, Type> _registry = new Dictionary<QuantumServerCommand, Type>()
		{
			{ QuantumServerCommand.EndOfGameRewards,typeof(EndOfGameCalculationsCommand) }
		};

		public static IQuantumCommand BuildFromEvent(EventFireQuantumServerCommand ev)
		{
			if(!_registry.TryGetValue(ev.CommandType, out var commandType))
			{
				throw new Exception($"Invalid quantum server command from simulation {ev.CommandType.ToString()}");
			}
			return Activator.CreateInstance(commandType) as IQuantumCommand;
		}
	}
}
