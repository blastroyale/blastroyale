using System.Collections.Generic;
using FirstLight.Game.MonoComponent.EntityPrototypes;
using FirstLight.Game.Services;
using Quantum;
using FirstLight.SDK.Services;
using Photon.Realtime;
using UnityEngine;

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

	public struct EntityViewLoaded : IMessage
	{
		public EntityView View;
		public EntityBase Entity;
	}
	
	public struct PlayerCharacterInstantiated : IMessage
	{
		public PlayerCharacterMonoComponent Character;
	}

	public struct LocalPlayerEntityVisibilityUpdate : IMessage
	{
		public EntityRef Entity;
		public bool CanSee;
	}
	
	public struct CoreMatchAssetsLoadedMessage : IMessage { }
	public struct WaitingMandatoryMatchAssetsMessage : IMessage { }

	public struct PlayerUpdateLoadoutMessage : IMessage
	{
		public Player Player;
		public List<GameId> Loadout;
	}
	public struct SpectateStartedMessage : IMessage { }
	public struct SpectateSetCameraMessage : IMessage { public int CameraId; }

	public struct WinnerSetCameraMessage : IMessage
	{
		public Transform WinnerTrasform;
	}
	
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