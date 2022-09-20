using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Services;
using Photon.Realtime;
using Quantum;

namespace FirstLight.Game.Messages
{
	public struct PlayScreenOpenedMessage : IMessage { }
	public struct RoomLeaveClickedMessage : IMessage { }
	public struct RoomLockClickedMessage : IMessage
	{
		public bool AddBots;
	}
	public struct SpectatorModeToggledMessage : IMessage
	{
		public bool IsSpectator;
	}
	public struct RequestKickPlayerMessage : IMessage
	{
		public Player Player;
	}
	public struct PlayJoinRoomClickedMessage : IMessage { public string RoomName; }
	public struct PlayMatchmakingReadyMessage : IMessage { }
	public struct SelectedGameModeMessage : IMessage { }
	public struct PlayMapClickedMessage : IMessage
	{
		public int MapId;
	}
	public struct PlayCreateRoomClickedMessage : IMessage
	{
		public string RoomName;
		public QuantumGameModeConfig GameModeConfig;
		public QuantumMapConfig MapConfig;
		public string Mutators;
		public bool JoinIfExists;
	}
}
