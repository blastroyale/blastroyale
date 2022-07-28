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
	
	public string PlayerId => _playerId;
	public object Command => _command;
	public ServerState PlayerState => _userState;
	
	public CommandFinishedEvent(string playerId, object command, ServerState finalState)
	{
		_playerId = playerId;
		_command = command;
		_userState = finalState;
	}
}
