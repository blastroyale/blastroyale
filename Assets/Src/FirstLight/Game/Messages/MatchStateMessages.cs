using FirstLight.Services;

namespace FirstLight.Game.Messages
{
	public struct MatchSimulationStartedMessage : IMessage { }
	public struct MatchSimulationEndedMessage : IMessage { }
	public struct MatchReadyMessage : IMessage { }
	public struct MatchStartedMessage : IMessage { }
	public struct MatchEndedMessage : IMessage { }
	public struct MatchReadyForResyncMessage : IMessage { }
	public struct CoreMatchAssetsLoadedMessage : IMessage { }
	public struct AllMatchAssetsLoadedMessage : IMessage { }
	public struct StartedFinalPreloadMessage : IMessage { }
	public struct SpectateKillerMessage : IMessage { }
}