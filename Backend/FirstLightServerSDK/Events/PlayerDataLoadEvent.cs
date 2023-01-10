using System;
using FirstLight.Server.SDK.Models;

namespace FirstLight.Server.SDK.Events
{
	/// <summary>
	/// Event called before server data is loaded so operations that update this data can be called.
	/// </summary>
	public class PlayerDataLoadEvent : GameServerEvent
	{
		public readonly string PlayerId;
		public readonly ServerState PlayerState;

		public PlayerDataLoadEvent(string playerId, ServerState currentState)
		{
			PlayerId = playerId;
			PlayerState = currentState;
		}
		
	}
}

