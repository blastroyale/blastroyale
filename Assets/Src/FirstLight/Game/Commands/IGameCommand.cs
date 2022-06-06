using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Services;

namespace FirstLight.Game.Commands
{
	/// <inheritdoc cref="IGameCommand{T}"/>
	public interface IGameCommand
	{
		/// <summary>
		/// Define necessary permissions to run a given command on server.
		/// On development servers, everyone is admin.
		/// </summary>
		CommandAccessLevel AccessLevel => CommandAccessLevel.Player;
		
		/// <summary>
		/// Executes the command logic
		/// </summary>
		void Execute(IGameLogic gameLogic, IDataProvider dataProvider);
	}
}