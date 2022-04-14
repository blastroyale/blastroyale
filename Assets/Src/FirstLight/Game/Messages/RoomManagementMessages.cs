using System.Collections.Generic;
using ExitGames.Client.Photon;
using FirstLight.Services;
using Photon.Realtime;

namespace FirstLight.Game.Messages
{
	public struct RoomRandomClickedMessage : IMessage { }
	public struct RoomJoinCreateClickedMessage : IMessage { }
	public struct RoomJoinClickedMessage : IMessage { }
	public struct RoomCreateClickedMessage : IMessage { }

}