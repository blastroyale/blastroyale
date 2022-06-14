using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Messages
{
	public struct PlayScreenOpenedMessage : IMessage { }
	public struct ShopScreenOpenedMessage : IMessage { }
	public struct LootScreenOpenedMessage : IMessage { }
	public struct LootScreenClosedMessage : IMessage { }
	public struct SocialScreenOpenedMessage : IMessage { }
	public struct RoomLeaveClickedMessage : IMessage { }
	public struct RoomLockClickedMessage : IMessage { }
	public struct PlayJoinRoomClickedMessage : IMessage { public string RoomName; }
	public struct PlayRandomClickedMessage : IMessage { }
	public struct SelectedGameModeMessage : IMessage { }
	public struct PlayMapClickedMessage : IMessage
	{
		public int MapId;
	}
	public struct PlayCreateRoomClickedMessage : IMessage
	{
		public string RoomName; 
		public QuantumMapConfig MapConfig;
	}
}
