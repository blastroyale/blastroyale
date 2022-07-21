namespace ServerSDK.Events;

/// <summary>
/// Event called before server data is loaded so operations that update this data can be called.
/// </summary>
public class PlayerDataLoadEvent : GameServerEvent
{
	private readonly string _playerId;

	public PlayerDataLoadEvent(string playerId)
	{
		_playerId = playerId;
	}
	
	public string PlayerId => _playerId;
}