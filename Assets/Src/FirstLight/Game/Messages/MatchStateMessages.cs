using FirstLight.Game.Services;
using Quantum;
using FirstLight.SDK.Services;
using Photon.Realtime;

namespace FirstLight.Game.Messages
{
	public enum SimulationEndReason
	{
		Finished, Disconnected
	}
	
	/// <summary>
	/// When simulation initializes. Not necessarily when its running but when initialized the runner.
	/// </summary>
	public struct MatchSimulationStartedMessage : IMessage { }

	public struct SimulationEndedMessage : IMessage
	{
		public SimulationEndReason Reason;
		public QuantumGame Game;
	}
	public struct MatchStartedMessage : IMessage
	{
		public QuantumGame Game;
		public bool IsResync;
	}

	public struct MatchEndedMessage : IMessage
	{
		public QuantumGame Game;
		public bool IsDisconnected;
		public bool IsPlayerQuit;
	}
	public struct CoreMatchAssetsLoadedMessage : IMessage { }
	public struct AllMatchAssetsLoadedMessage : IMessage { }
	public struct StartedFinalPreloadMessage : IMessage { }
	public struct AssetReloadRequiredMessage : IMessage { }
	public struct SpectateStartedMessage : IMessage { }
	public struct SpectateSetCameraMessage : IMessage { public int CameraId; }
	public struct LeftBeforeMatchFinishedMessage : IMessage { }
	public struct MatchCountdownStartedMessage : IMessage { }

	public struct PlayerEnteredMessageVolume : IMessage
	{
		public string VolumeId;
	}

	public struct PlayerUsedMovementJoystick : IMessage { }
	
	public struct PlayerEnteredAmbienceMessage : IMessage
	{
		public AmbienceType Ambience;
	}
	
	public struct PlayerLeftAmbienceMessage : IMessage
	{
		public AmbienceType Ambience;
	}
}