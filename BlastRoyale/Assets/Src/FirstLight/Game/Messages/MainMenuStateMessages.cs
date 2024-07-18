using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using Photon.Realtime;
using Quantum;
using FirstLight.SDK.Services;

namespace FirstLight.Game.Messages
{
	public struct MainMenuShouldReloadMessage : IMessage { }
	public struct MainMenuLoadedMessage : IMessage { }
	public struct MainMenuOpenedMessage : IMessage { }
	public struct ShopScreenOpenedMessage : IMessage { }
	public struct OnViewingRewardsFinished : IMessage { }
	public struct EquippedItemMessage : IMessage
	{
		public UniqueId ItemID;
	}
	public struct RoomLeaveClickedMessage : IMessage { }

	public struct JoinRoomMessage : IMessage { }

	public struct LocalPlayerClickedPlayMessage : IMessage { }
	public struct MatchmakingCancelMessage : IMessage { }
	
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
