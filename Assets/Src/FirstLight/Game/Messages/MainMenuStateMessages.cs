using System.Collections.Generic;
using Photon.Realtime;
using Quantum;
using FirstLight.SDK.Services;

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
		public List<string> Mutators;
		public bool JoinIfExists;
	}
}
