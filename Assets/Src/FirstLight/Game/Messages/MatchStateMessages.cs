using FirstLight.Game.Ids;
using FirstLight.Services;
using Photon.Realtime;
using UnityEngine.UI;

namespace FirstLight.Game.Messages
{
	public struct MatchConnectedMessage : IMessage { }
	public struct MatchDisconnectedMessage : IMessage { public DisconnectCause Cause; }
	public struct MatchJoinedRoomMessage : IMessage { }
	public struct PlayerJoinedMatchMessage : IMessage { public Player player; }
	public struct PlayerLeftMatchMessage : IMessage { public Player player; }
	public struct MatchSimulationStartedMessage : IMessage { }
	public struct MatchSimulationEndedMessage : IMessage { }
	public struct MatchReadyMessage : IMessage { }
	public struct MatchStartedMessage : IMessage { }
	public struct MatchEndedMessage : IMessage { }
}