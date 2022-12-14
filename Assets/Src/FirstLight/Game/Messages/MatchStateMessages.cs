using Quantum;
using FirstLight.SDK.Services;

namespace FirstLight.Game.Messages
{
	public struct MatchSimulationStartedMessage : IMessage { }
	public struct MatchSimulationEndedMessage : IMessage { }
	public struct MatchStartedMessage : IMessage
	{
		public QuantumGame Game;
		public bool IsResync;
	}
	public struct MatchEndedMessage : IMessage { public QuantumGame Game; }
	public struct CoreMatchAssetsLoadedMessage : IMessage { }
	public struct AllMatchAssetsLoadedMessage : IMessage { }
	public struct StartedFinalPreloadMessage : IMessage { }
	public struct AssetReloadRequiredMessage : IMessage { }
	public struct SpectateStartedMessage : IMessage { }
	public struct SpectateSetCameraMessage : IMessage { public int CameraId; }
	public struct LeftMatchFromSpectateMessage : IMessage { }
	public struct MatchCountdownStartedMessage : IMessage { }
}