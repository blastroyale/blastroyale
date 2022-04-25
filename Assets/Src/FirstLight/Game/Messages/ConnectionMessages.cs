using System.Collections.Generic;
using ExitGames.Client.Photon;
using FirstLight.Services;
using Photon.Realtime;

namespace FirstLight.Game.Messages
{
	public struct PlayerJoinedRoomMessage : IMessage { public Player Player; }
	public struct PlayerLeftRoomMessage : IMessage { public Player Player;  }
	public struct AllPlayersLoadedMatchMessage : IMessage {}
	public struct AllPlayersLoadedEquipmentMessage : IMessage {}
	public struct PlayerPropertiesUpdatedMessage : IMessage { public Player Player; public Hashtable ChangedProps; }
	public struct PlayerLoadedMatchMessage : IMessage { public Player Player; }
	public struct PlayerLoadedEquipmentMessage : IMessage { public Player Player; }

}