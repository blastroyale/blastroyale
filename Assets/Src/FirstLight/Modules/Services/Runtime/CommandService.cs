// ReSharper disable once CheckNamespace

namespace FirstLight.Services
{
	/// <summary>
	/// Tags the interface as a <see cref="IGameCommand{TGameLogic}"/>
	/// </summary>
	public interface IGameCommandBase {}
	
	/// <summary>
	/// Interface representing the command to be executed in the <see cref="ICommandService{TGameLogic}"/>.
	/// Implement this interface with the proper command logic
	/// </summary>
	/// <remarks>
	/// Follows the Command pattern <see cref="https://en.wikipedia.org/wiki/Command_pattern"/>
	/// </remarks>
	public interface IGameCommand<in TGameLogic> : IGameCommandBase where TGameLogic : class
	{
		/// <summary>
		/// Executes the command logic
		/// </summary>
		void Execute(TGameLogic gameLogic);
	}
	
	/// <summary>
	/// This service provides the possibility to execute a <see cref="IGameCommand{TGameLogic}"/>.
	/// It allows to create an seamless abstraction layer of execution between the game logic and any other part of the code 
	/// </summary>
	public interface ICommandService<out TGameLogic> where TGameLogic : class
	{
		/// <summary>
		/// Executes the given <paramref name="command"/>.
		/// The command execution is done atomically
		/// </summary>
		void ExecuteCommand<TCommand>(TCommand command) where TCommand : struct, IGameCommand<TGameLogic>;
	}
	
	/// <inheritdoc />
	public class CommandService<TGameLogic> : ICommandService<TGameLogic> where TGameLogic : class
	{
		private readonly TGameLogic _gameLogic;
		
		public CommandService(TGameLogic gameLogic)
		{
			_gameLogic = gameLogic;
		}
		
		/// <inheritdoc />
		public void ExecuteCommand<TCommand>(TCommand command) where TCommand : struct, IGameCommand<TGameLogic>
		{
			command.Execute(_gameLogic);
		}
	}
}