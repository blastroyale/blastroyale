using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Messages
{
	public struct MatchSimulationStartedMessage : IMessage { }
	public struct MatchSimulationEndedMessage : IMessage { }
	public struct MatchReadyMessage : IMessage { }
	public struct MatchStartedMessage : IMessage 
	{ 
		public bool IsResync;
	}
	public struct MatchEndedMessage : IMessage { }
	public struct CoreMatchAssetsLoadedMessage : IMessage { }
	public struct AllMatchAssetsLoadedMessage : IMessage { }
	public struct StartedFinalPreloadMessage : IMessage { }
	public struct AssetReloadRequiredMessage : IMessage { }
	public struct SpectateStartedMessage : IMessage { }

	public struct SpectateNextPlayerMessage : IMessage { }

	public struct SpectatePreviousPlayerMessage : IMessage { }

	public struct SpectateTargetSwitchedMessage : IMessage
	{
		public EntityRef EntitySpectated;
		public PlayerRef PlayerSpectated;
	}

	public struct SpectateSetCameraMessage : IMessage { public int CameraId; }

}