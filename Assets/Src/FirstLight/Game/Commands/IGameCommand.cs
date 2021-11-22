using FirstLight.Game.Logic;
using FirstLight.Services;

namespace FirstLight.Game.Commands
{
	/// <inheritdoc cref="IGameCommand{T}"/>
	public interface IGameCommand
	{
		/// <summary>
		/// Executes the command logic
		/// </summary>
		void Execute(IGameLogic gameLogic, IDataProvider dataProvider);
	}
}