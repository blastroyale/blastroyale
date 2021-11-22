using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// This abstract command creates the basic execution contract of the game's command
	/// </summary>
	public abstract class CommandBase : DeterministicCommand
	{
		/// <summary>
		/// Executes the command in the given <paramref name="f"/> frame
		/// </summary>
		internal abstract void Execute(Frame f, PlayerRef playerRef);
	}
}