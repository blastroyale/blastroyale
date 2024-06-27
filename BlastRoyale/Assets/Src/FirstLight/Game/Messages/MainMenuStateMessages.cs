using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using Photon.Realtime;
using Quantum;
using FirstLight.SDK.Services;

namespace FirstLight.Game.Messages
{
	public struct MainMenuShouldReloadMessage : IMessage { }
	public struct MainMenuOpenedMessage : IMessage { }
	public struct PlayScreenOpenedMessage : IMessage { }
	public struct ShopScreenOpenedMessage : IMessage { }
	public struct OnViewingRewardsFinished : IMessage { }
	public struct SelectedEquipmentItemMessage : IMessage
	{
		public UniqueId ItemID;
	}
	public struct EquippedItemMessage : IMessage
	{
		public UniqueId ItemID;
	}
	public struct EquipmentSlotOpenedMessage : IMessage
	{
		public GameIdGroup Slot;
	}
	public struct RoomLeaveClickedMessage : IMessage { }
	public struct RoomLockClickedMessage : IMessage
	{
		public bool AddBots;
	}
	public struct PlayJoinRoomClickedMessage : IMessage { public string RoomName; }
	public struct LocalPlayerClickedPlayMessage : IMessage { }
	public struct MatchmakingCancelMessage : IMessage { }
	public struct PlayMapClickedMessage : IMessage
	{
		public int MapId;
	}
	
	public struct PlayCreateRoomClickedMessage : IMessage
	{
		public string RoomName;
		public QuantumGameModeConfig GameModeConfig;
		public QuantumMapConfig MapConfig;
		public CustomMatchSettings CustomGameOptions;
		public bool JoinIfExists;
	}

	public struct ReinitializeMenuViewsMessage : IMessage
	{
		
	}
	
	public struct MapDropPointSelectedMessage : IMessage { }
}
