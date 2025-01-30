using Cysharp.Threading.Tasks;

namespace FirstLight.Server.SDK.Modules.Commands
{
	/// <summary>
	/// Represents a command that can be executed on client and also be sent to ran on server.
	/// Should be deterministic given the same command execution context.
	/// </summary>
	public interface IGameCommand
	{
		/// <summary>
		/// Define necessary permissions to run a given command on server.
		/// On development servers, everyone is admin.
		/// </summary>
		CommandAccessLevel AccessLevel();

		/// <summary>
		/// Defines the execution mode of the given command.
		/// </summary>
		CommandExecutionMode ExecutionMode();

		/// <summary>
		/// Executes the command logic
		/// </summary>
		UniTask Execute(CommandExecutionContext ctx);
	}

	public interface IGameCommandWithResult<out T> : IGameCommand
	{
		T GetResult();
	}
}