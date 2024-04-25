using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using FirstLight.UIService;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// State to reconnect to a previously disconnected match. This works across crashes/game closes.
	/// Will attempt a few different ways:
	/// 1-) Re-join a game that was saved by a game snapshot.
	///    - Will go to main manu if failed without messages.
	///    - If can create a room from snapshot (offline game, custom or dev env) will attempt to re-create the game
	///      from that snapshot from the frame it stopped.
	///
	///    If it matches and joins a room, game will load instantly, else would go to main menu without user noticing.
	/// </summary>
	public class ReconnectionState
	{
		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly IInternalGameNetworkService _networkService;
		private readonly UIService.UIService _uiService;
		private readonly MatchState _matchState;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		public static readonly IStatechartEvent ReconnectToRoomEvent = new StatechartEvent("Reconnect To Snapshot");

		private Coroutine _csPoolTimerCoroutine;

		public ReconnectionState(IGameServices services, IGameDataProvider dataProvider, IInternalGameNetworkService networkService,
								 Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_dataProvider = dataProvider;
			_networkService = networkService;
			_uiService = services.UIService;
			_statechartTrigger = statechartTrigger;
		}

		/// <summary>
		/// Setups the Adventure gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var firstMatchCheck = stateFactory.Choice("Check for Reconnection");
			var createRoomFromSnapshot = stateFactory.State("Create room from snapshot");
			var checkCreateNewRoom = stateFactory.Choice("check if can create room");
			var joinPendingMatch = stateFactory.State("Join Pending Match");

			initial.Transition().Target(firstMatchCheck);

			firstMatchCheck.Transition().Condition(HasPendingMatch).Target(joinPendingMatch);
			firstMatchCheck.Transition().Target(final);

			joinPendingMatch.OnEnter(JoinPendingMatch);
			joinPendingMatch.Event(NetworkState.JoinedRoomEvent).Target(final);
			joinPendingMatch.Event(NetworkState.GameDoesNotExists).Target(checkCreateNewRoom);
			joinPendingMatch.Event(NetworkState.JoinRoomFailedEvent).OnTransition(ClearSnapshot).Target(final);

			checkCreateNewRoom.Transition().Condition(CanCreateRoomFromSnapshot).Target(createRoomFromSnapshot);
			checkCreateNewRoom.Transition().OnTransition(ClearSnapshot).Target(final);

			createRoomFromSnapshot.OnEnter(CreateRoomFromSnapshot);
			createRoomFromSnapshot.Event(NetworkState.JoinedRoomEvent).OnTransition(FireReconnect)
				.OnTransition(SetupSnapshotRoom).Target(final);
			createRoomFromSnapshot.Event(NetworkState.CreateRoomFailedEvent).OnTransition(ClearSnapshot).Target(final);
		}

		private void FireReconnect()
		{
			_statechartTrigger(ReconnectToRoomEvent);
		}

		private void ClearSnapshot()
		{
			FLog.Verbose("Clearing Snapshot");
			_dataProvider.AppDataProvider.LastFrameSnapshot.Value = default;
			if (_dataProvider.AppDataProvider.IsPlayerLoggedIn)
			{
				_services.DataSaver.SaveData<AppData>();
			}
		}

		private void SetupSnapshotRoom()
		{
			if (_networkService.JoinSource.Value == JoinRoomSource.RecreateFrameSnapshot)
			{
				_networkService.QuantumClient.CurrentRoom.PlayerTtl = 0;
			}
		}

		private bool CanCreateRoomFromSnapshot()
		{
			var snapshot = _dataProvider.AppDataProvider.LastFrameSnapshot.Value;
			return snapshot.CanBeRestoredWithLocalSnapshot();
		}

		private void CreateRoomFromSnapshot()
		{
			FLog.Verbose("Creating new room From Snapshot");
			var snapshot = _dataProvider.AppDataProvider.LastFrameSnapshot.Value;
			snapshot.Setup.RoomIdentifier ??= Guid.NewGuid().ToString();
			_networkService.JoinSource.Value = JoinRoomSource.RecreateFrameSnapshot;
			_services.RoomService.CreateRoom(snapshot.Setup, snapshot.Offline);
		}

		private bool HasPendingMatch()
		{
#if UNITY_EDITOR
			if (FeatureFlags.GetLocalConfiguration().DisableReconnection)
			{
				ClearSnapshot();
				return false;
			}
#endif
			var snapShot = _dataProvider.AppDataProvider.LastFrameSnapshot.Value;
			var isTutorial = snapShot.Setup is {GameModeId: GameConstants.Tutorial.FIRST_TUTORIAL_GAME_MODE_ID};
			var canRestoreFromSnapshot = _services.GameBackendService.RunsSimulationOnServer() || snapShot.Offline || snapShot.AmtPlayers > 1;

			// Tutorial does not support reconnecting mid-way if app was closed due to keeping track of internal states in view/state machines
			// and not simulation
			if (isTutorial)
			{
				ClearSnapshot();
				return false;
			}

			if (canRestoreFromSnapshot && !snapShot.Expired())
			{
				return true;
			}

			FLog.Verbose($"Snapshot expired ({new DateTime(snapShot.ExpiresAt)}) or tutorial({isTutorial})");
			_dataProvider.AppDataProvider.LastFrameSnapshot.Value = default;
			return false;
		}

		private async UniTaskVoid MatchTransition()
		{
			await _uiService.OpenScreen<SwipeTransitionScreenPresenter>();
		}

		private void JoinPendingMatch()
		{
			MatchTransition().Forget();
			var snapShot = _dataProvider.AppDataProvider.LastFrameSnapshot.Value;
			if (snapShot.Offline)
			{
				FLog.Verbose("Creating offline room from snapshot");
				_networkService.JoinSource.Value = JoinRoomSource.RecreateFrameSnapshot;
				_services.RoomService.CreateRoom(snapShot.Setup, true);
			}
			else
			{
				FLog.Verbose($"Rejoining room from {snapShot.RoomName} snapshot");
				_networkService.JoinSource.Value = JoinRoomSource.ReconnectFrameSnapshot;
				_services.RoomService.RejoinRoom(snapShot.RoomName);
			}
		}
	}
}