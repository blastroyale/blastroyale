using FirstLight.Game.Ids;
using FirstLight.Services;
using Photon.Realtime;
using UnityEngine.UI;

namespace FirstLight.Game.Messages
{
	public struct MatchSimulationStartedMessage : IMessage { }
	public struct MatchSimulationEndedMessage : IMessage { }
	public struct MatchReadyMessage : IMessage { }
	public struct MatchStartedMessage : IMessage { }
	public struct MatchEndedMessage : IMessage { }
	public struct MatchAssetsLoadedMessage : IMessage { }
	public struct EnteredMatchmakingStateMessage : IMessage { }
}