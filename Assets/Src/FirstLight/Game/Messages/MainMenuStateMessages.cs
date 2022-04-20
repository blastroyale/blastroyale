using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Services;

namespace FirstLight.Game.Messages
{
	public struct PlayScreenOpenedMessage : IMessage { }
	public struct ShopScreenOpenedMessage : IMessage { }
	public struct LootScreenOpenedMessage : IMessage { }
	public struct CratesScreenOpenedMessage : IMessage { }
	public struct LootScreenClosedMessage : IMessage { }
	public struct CratesScreenClosedMessage : IMessage { }
	public struct SocialScreenOpenedMessage : IMessage { }
	
	/// <summary>
	/// Requests the Crates Screen to open. Triggered when tapping on a 3D Loot Box instead of a UI Element.
	/// </summary>
	public struct MenuWorldLootBoxClickedMessage : IMessage { }

	public struct LootBoxReadyToBeOpenedMessage : IMessage { public  List<UniqueId> ListToOpen; }
	public struct FuseSequenceReadyMessage : IMessage { public List<UniqueId> FusionList; }
	public struct FuseCompletedMessage : IMessage {  }
	public struct EnhanceSequenceReadyMessage : IMessage { public List<UniqueId> EnhanceList; }
	public struct EnhanceCompletedMessage : IMessage {  }
	public struct CrateClickedMessage : IMessage { public UniqueId LootBoxId; }
	public struct RoomRandomClickedMessage : IMessage { }
	public struct RoomJoinCreateClickedMessage : IMessage { }
	public struct RoomJoinClickedMessage : IMessage { public string RoomName; }
	public struct RoomCreateClickedMessage : IMessage { public string RoomName; }
	public struct RoomLeaveClickedMessage : IMessage { }
}