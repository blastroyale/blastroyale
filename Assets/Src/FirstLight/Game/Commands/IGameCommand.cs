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
		/// Marks this command to be executed only on the client or also on the server.
		/// By default <see cref="IGameCommand"/> always runs on the server. To only run on the client, please mark on
		/// the interface implementation as false.
		/// </summary>
		/// <remarks>
		/// Use this check with care and guarantee that will not create de-syncs between the local and server state
		/// </remarks>
		bool ExecuteServer => true;
		
		/// <summary>
		/// Executes the command logic
		/// </summary>
		void Execute(IGameLogic gameLogic, IDataProvider dataProvider);
	}
}