using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Server.SDK.Events
{
	/// <summary>
	/// Event called after any command ran.
	/// Can be used to manipulate player state.
	/// </summary>
	public class CommandFinishedEvent : GameServerEvent
	{
		private readonly IGameCommand _command; 
		private readonly ServerState _userState;
		private readonly ServerState _userStateBeforeCommand;
		private string _commandData;
		
		public IGameCommand Command => _command;
		public ServerState PlayerState => _userState;
		public ServerState PlayerStateBeforeCommand => _userStateBeforeCommand;
		public string CommandData => _commandData;
	
		public CommandFinishedEvent(string playerId, IGameCommand command, ServerState finalState, ServerState stateBeforeCommand, string commandData) : base(playerId)
		{
			_command = command;
			_userState = finalState;
			_commandData = commandData;
			_userStateBeforeCommand = stateBeforeCommand;
		}
	}

}

