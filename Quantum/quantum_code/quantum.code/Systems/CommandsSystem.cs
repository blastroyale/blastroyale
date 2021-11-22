using Quantum.Commands;

namespace Quantum.Systems
{
	/// <summary>
	/// Handles execution of all player commands.
	/// </summary>
	public class CommandsSystem : SystemMainThread
	{
		/// <inheritdoc />
		public override void Update(Frame f)
		{
			for (var i = 0; i < f.PlayerCount; i++)
			{
				var command = f.GetPlayerCommand(i) as CommandBase;
				
				command?.Execute(f, i);
			}
		}
	}
}