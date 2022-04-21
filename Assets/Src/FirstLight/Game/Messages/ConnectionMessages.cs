using System.Collections.Generic;
using ExitGames.Client.Photon;
using FirstLight.Services;
using Photon.Realtime;

namespace FirstLight.Game.Messages
{
	public struct PhotonBaseConnectedMessage : IMessage { }
	public struct PhotonMasterConnectedMessage : IMessage { }
	public struct PhotonDisconnectedMessage : IMessage { public DisconnectCause Cause; }
	public struct CreatedRoomMessage : IMessage { }
	public struct CreateRoomFailedMessage : IMessage { }
	public struct JoinedRoomMessage : IMessage { }
	public struct JoinRoomFailedMessage : IMessage { public short ReturnCode; public string Message; }
	public struct LeftRoomMessage : IMessage { }
	public struct PlayerJoinedRoomMessage : IMessage { public Player Player; }
	public struct PlayerLeftRoomMessage : IMessage { public Player Player;  }
	public struct MasterClientSwichedMessage : IMessage { public Player NewMaster; }
	public struct RoomPropertiesUpdatedMessage : IMessage { public Hashtable ChangedProps; }
	public struct RoomClosedMessage : IMessage {}
	public struct PlayerPropertiesUpdatedMessage : IMessage { public Player Player; public Hashtable ChangedProps; }
	public struct PlayerLoadedMatchMessage : IMessage { public Player Player; }
	public struct PlayerLoadedEquipmentMessage : IMessage { public Player Player; }
	public struct AllPlayersLoadedMatchMessage : IMessage {}
	public struct AllPlayersLoadedEquipmentMessage : IMessage {}
	public struct RegionListReceivedMessage : IMessage { public RegionHandler RegionHandler; }
	public struct CustomAuthResponseMessage : IMessage { public Dictionary<string, object> Data; }
	public struct CustomAuthFailedMessage : IMessage { public string Message; }
	public struct FriendListUpdateMessage : IMessage { public List<FriendInfo> FriendList; }
}