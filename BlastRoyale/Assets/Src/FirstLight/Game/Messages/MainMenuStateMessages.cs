using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using Photon.Realtime;
using Quantum;
using FirstLight.SDK.Services;

namespace FirstLight.Game.Messages
{
	public struct MainMenuShouldReloadMessage : IMessage
	{
	}

	public struct MainMenuLoadedMessage : IMessage
	{
	}

	public struct MainMenuOpenedMessage : IMessage
	{
	}

	public struct ShopScreenOpenedMessage : IMessage
	{
	}

	public struct OnViewingRewardsFinished : IMessage
	{
	}

	public struct EquippedItemMessage : IMessage
	{
		public UniqueId ItemID;
	}

	public struct RoomLeaveClickedMessage : IMessage
	{
	}

	public struct JoinRoomMessage : IMessage
	{
	}

	public struct LocalPlayerClickedPlayMessage : IMessage
	{
	}

	public struct MatchmakingCancelMessage : IMessage
	{
	}

	public struct ReinitializeMenuViewsMessage : IMessage
	{
	}

	/// <summary>
	///  This should be temporary, its a workaround to reset joinsource, because presenters are handling join room logic
	/// </summary>
	public struct StartedCustomMatch : IMessage
	{
		public CustomMatchSettings Settings;
	}

	public struct MapDropPointSelectedMessage : IMessage
	{
	}
}