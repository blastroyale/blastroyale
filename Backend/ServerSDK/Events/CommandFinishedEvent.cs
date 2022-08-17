using ServerSDK.Models;

namespace ServerSDK.Events;

/// <summary>
/// Event called after any command ran.
/// Can be used to manipulate player state.
/// </summary>
public class CommandFinishedEvent : GameServerEvent
{
	private readonly string _playerId;
	private readonly object _command; // TODO: Change to IGameCommand when moving IGameCommand to SDK
	private readonly ServerState _userState;
	private string _commandData;
	
	public string PlayerId => _playerId;
	public object Command => _command;
	public ServerState PlayerState => _userState;
	public string CommandData => _commandData;
	
	public CommandFinishedEvent(string playerId, object command, ServerState finalState, string commandData)
	{
		_playerId = playerId;
		_command = command;
		_userState = finalState;
		_commandData = commandData;
	}
}
