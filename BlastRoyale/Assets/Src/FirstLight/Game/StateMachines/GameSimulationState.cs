using System;
using System.Diagnostics;
using System.Linq;
using FirstLight.FLogger;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes.Helpers;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Authentication;
using FirstLight.Game.TestCases;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.Server.SDK.Modules;
using FirstLight.Statechart;
using I2.Loc;
using Photon.Deterministic;
using PlayFab;
using Quantum;
using Quantum.Commands;
using Quantum.Systems;
using Unity.Services.Authentication;
using Assert = UnityEngine.Assertions.Assert;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Adventure Game Simulation State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class GameSimulationState
	{
		public static readonly IStatechartEvent SimulationStartedEvent = new StatechartEvent("Simulation Ready Event");
		public static readonly IStatechartEvent SimulationDestroyedEvent = new StatechartEvent("Simulation Destroyed Event");
		public static readonly IStatechartEvent LocalPlayerExitEvent = new StatechartEvent("Local Player Exit");
		private readonly BattleRoyaleState _battleRoyaleState;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IGameServices _services;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly IGameNetworkService _network;
		private readonly IInternalGameNetworkService _networkService;

		private IMatchServices _matchServices;

		public GameSimulationState(IGameDataProvider gameDataProvider, IGameServices services, IInternalGameNetworkService networkService,
								   Action<IStatechartEvent> statechartTrigger)
		{
			_gameDataProvider = gameDataProvider;
			_services = services;
			_networkService = networkService;
			_statechartTrigger = statechartTrigger;
			_battleRoyaleState = new BattleRoyaleState(services, statechartTrigger);
		}

		/// <summary>
		/// Setups the Game Simulation state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");

			var battleRoyale = stateFactory.Nest("Battle Royale Mode");
			var startSimulation = stateFactory.State("Start Simulation");
			var stopSimulationForDisconnection = stateFactory.State("Stop Simulation");
			var simulationInitializationError = stateFactory.Choice("Stop Simulation Initialization Error");
			var disconnected = stateFactory.State("Disconnected");
			var disconnectedCritical = stateFactory.State("Disconnected Critical");

			initial.Transition().Target(startSimulation);
			initial.OnExit(SubscribeEvents);

			startSimulation.OnEnter(StartSimulation);
			startSimulation.Event(SimulationStartedEvent).Target(battleRoyale);
			startSimulation.Event(SimulationDestroyedEvent).Target(simulationInitializationError);
			startSimulation.Event(NetworkState.LeftRoomEvent).Target(final);
			startSimulation.Event(NetworkState.PhotonDisconnectedEvent).Target(stopSimulationForDisconnection);
			startSimulation.OnExit(() => CloseSwipeTransition().Forget());

			battleRoyale.Nest(_battleRoyaleState.Setup).Target(final);
			battleRoyale.Event(NetworkState.PhotonDisconnectedEvent).Target(stopSimulationForDisconnection);

			simulationInitializationError.Transition().OnTransition(MatchError).Target(final);

			stopSimulationForDisconnection.OnEnter(StopSimulation);
			stopSimulationForDisconnection.Event(NetworkState.JoinedRoomEvent).OnTransition(UnloadSimulation).Target(startSimulation);
			stopSimulationForDisconnection.Event(NetworkState.JoinRoomFailedEvent).OnTransition(UnloadSimulation).Target(disconnectedCritical);

			disconnected.Event(NetworkState.JoinedRoomEvent).Target(startSimulation);
			disconnected.Event(NetworkState.JoinRoomFailedEvent).Target(disconnectedCritical);

			final.OnEnter(UnsubscribeEvents);
		}

		/// <summary>
		/// For tutorial, we close the swipe transition when we actually get into the game, instead of
		/// closing at matchmaking screen opening in matchState. This is to avoid visual glitches with MM screen
		/// still persisting on screen for a second before game simulation
		/// </summary>
		private async UniTaskVoid CloseSwipeTransition()
		{
			await UniTask.NextFrame();
			await _services.UIService.CloseScreen<SwipeTransitionScreenPresenter>(false);
		}

		private void SubscribeEvents()
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			_services.MessageBrokerService.Subscribe<QuitGameClickedMessage>(OnQuitGameScreenClickedMessage);

			QuantumEvent.SubscribeManual<EventOnAllPlayersJoined>(this, OnAllPlayersJoined);
			QuantumCallback.SubscribeManual<CallbackGameStarted>(this, OnGameStart);
			QuantumCallback.SubscribeManual<CallbackGameResynced>(this, OnGameResync);
			QuantumCallback.SubscribeManual<CallbackGameDestroyed>(this, OnGameDestroyed);
			QuantumCallback.SubscribeManual<CallbackPluginDisconnect>(this, OnPluginDisconnected);
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
			//_matchServices?.SpectateService?.SpectatedPlayer?.StopObserving(TO_DELETE_WITH_NEW_START_SEQUENCE);
			QuantumEvent.UnsubscribeListener(this);
			QuantumCallback.UnsubscribeListener(this);

			_matchServices = null;
		}

		private void UnloadSimulation()
		{
			FLog.Verbose("Unloading Simulation");
			if (QuantumRunner.Default != null && !QuantumRunner.Default.IsDestroyed())
			{
				QuantumRunner.Default.Shutdown();
			}
		}

		private void OnGameDestroyed(CallbackGameDestroyed cb)
		{
			FLog.Verbose("Game Destroyed");
			_statechartTrigger(SimulationDestroyedEvent);
		}

		private void OnPluginDisconnected(CallbackPluginDisconnect callback)
		{
			if (callback.Reason == GameConstants.QuantumPluginDisconnectReasons.NOT_ENOUGH_PLAYERS)
			{
				_services.InGameNotificationService.QueueNotification(
					ScriptLocalization.UITMatchmaking.failed_to_find_players,
					InGameNotificationStyle.Error,
					InGameNotificationDuration.Long);
			}

			_services.MessageBrokerService.Publish(new QuantumServerSimulationDisconnectedMessage()
			{
				Reason = callback.Reason
			});
		}

		private async UniTask WaitForCameraOnPlayer()
		{
			await UniTask.WaitUntil(IsSpectatingPlayer);
		}

		private bool IsSpectatingPlayer()
		{
			if (!QuantumRunner.Default.IsDefinedAndRunning(false) || _matchServices == null) return false;
			var spectated = _matchServices.SpectateService.SpectatedPlayer.Value;
			if (!spectated.Entity.IsValid) return false;
			return true;
		}

		private async UniTaskVoid WaitForCamera()
		{
			await WaitForCameraOnPlayer();
		}

		private bool IsSpectator()
		{
			return _services.RoomService.IsLocalPlayerSpectator;
		}

		private void OnGameStart(CallbackGameStarted callback)
		{
			FLog.Verbose("Game Start");
			// paused on Start means waiting for Snapshot
			if (callback.Game.Session.IsPaused)
			{
				FLog.Verbose("Waiting for snapshot");
				return;
			}

			GameStartAsync(callback.Game).Forget();
			FLog.Verbose("Waiting for all players to join");
		}

		private async UniTaskVoid GameStartAsync(QuantumGame game)
		{
			FLog.Info("Game Started Message");
			//await UniTask.Delay(100); // tech debt, leftover shall eb removed
			await UniTask.WaitUntil(() => QuantumRunner.Default.IsDefinedAndRunning());
			await PublishMatchStartedMessage(game, false);
			//await UniTask.Delay(1000); // tech debt, leftover shall eb removed
			await UniTask.WaitUntil(_services.UIService.IsScreenOpen<HUDScreenPresenter>);

			if (!QuantumRunner.Default.IsDefinedAndRunning(false)) return;

			var f = game.Frames.Verified;
			var entityRef = game.GetLocalPlayerEntityRef();
			if (f != null && entityRef.IsValid && f.TryGet<PlayerCharacter>(entityRef, out var pc))
			{
				if (!pc.RealPlayer)
				{
					_services.MessageBrokerService.Publish(new LeftBeforeMatchFinishedMessage());
					_statechartTrigger(LocalPlayerExitEvent);
				}
			}
		}

		private void OnAllPlayersJoined(EventOnAllPlayersJoined callback)
		{
			FLog.Info("All Players Joined Event");
			// paused on Start means waiting for Snapshot
			if (callback.Game.Session.IsPaused)
			{
				return;
			}

			if (callback.GameFailed)
			{
				FLog.Error("All players joined with errors, finishing simulation");
				StopSimulation();
				return;
			}

			_statechartTrigger(SimulationStartedEvent);
			WaitForCamera().Forget();
		}

		private void OnGameResync(CallbackGameResynced callback)
		{
			FLog.Info(
				$"Game Resync {callback.Game.Frames.Verified.Number} vs {_gameDataProvider.AppDataProvider.LastFrameSnapshot.Value.FrameNumber}");

			ResyncCoroutine().Forget();
		}

		private async UniTaskVoid ResyncCoroutine()
		{
			await UniTask.WaitUntil(() => QuantumRunner.Default.IsDefinedAndRunning());
			_statechartTrigger(SimulationStartedEvent);

			await PublishMatchStartedMessage(QuantumRunner.Default.Game, true);
			await UniTask.WaitUntil(_services.UIService.IsScreenOpen<HUDScreenPresenter>);

			WaitForCamera().Forget();
		}

		private void OnQuitGameScreenClickedMessage(QuitGameClickedMessage message)
		{
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.Yes,
				ButtonOnClick = QuitGameConfirmedClicked
			};

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.confirmation,
				ScriptLocalization.AdventureMenu.AreYouSureQuit,
				true, confirmButton);
		}

		private void QuitGameConfirmedClicked()
		{
			if (!_services.RoomService.IsLocalPlayerSpectator)
			{
				QuantumRunner.Default.Game.SendCommand(new PlayerQuitCommand());
			}

			_statechartTrigger(MatchState.MatchQuitEvent);
		}

		private void MatchError()
		{
			MatchState.SharedData.Simulation = MatchState.Data.SimulationResult.Error;
			FLog.Verbose("Raising Match Error");
		}

		private void StartSimulation()
		{
			MatchState.SharedData.Simulation = MatchState.Data.SimulationResult.Success;
			Assert.IsNull(QuantumRunner.Default, "Simulation already running");

			FLog.Info($"Starting simulation from source {_services.NetworkService.JoinSource.ToString()}");

			var client = _services.NetworkService.QuantumClient;

			var startParams = _services.RoomService.CurrentRoom.GetDefaultStartParameters();
			startParams.NetworkClient = client;
			if (IsSpectator())
			{
				startParams.GameMode = DeterministicGameMode.Spectating;
			}

			FLog.Verbose("Starting " + ModelSerializer.Serialize(startParams.RuntimeConfig.MatchConfigs).Value);

			var snapShot = _gameDataProvider.AppDataProvider.LastFrameSnapshot.Value;
			if (snapShot is {FrameNumber: > 0, Offline: true} && _services.RoomService.CurrentRoom.IsOffline)
			{
				FLog.Info("Restoring Local Snapshot");
				FLog.Verbose(snapShot);
				startParams.FrameData = snapShot.SnapshotBytes;
				startParams.InitialFrame = snapShot.FrameNumber;
			}

			_networkService.SetLastRoom();

			QuantumRunner.StartGame(_services.NetworkService.UserId, startParams);

			_services.MessageBrokerService.Publish(new MatchSimulationStartedMessage());
		}

		private void CleanupFrame()
		{
			_matchServices.FrameSnapshotService.ClearFrameSnapshot();
		}

		/// <summary>
		/// This StopSimulation method is only used for disconnection flow.
		/// There is another StopSimulation method in MatchState which handles stopping simulation once the player
		/// has reached the complete end of flow, past any disconnection cases.
		/// </summary>
		private void StopSimulation()
		{
			FLog.Verbose("Stopping Simulation");
			if (QuantumRunner.Default == null || QuantumRunner.Default.IsDestroyed())
			{
				FLog.Verbose("Simulation already destroyed");
				return;
			}

			_services.MessageBrokerService.Publish(new SimulationEndedMessage
			{
				Game = QuantumRunner.Default.Game,
				Reason = SimulationEndReason.Disconnected
			});
			QuantumRunner.ShutdownAll();
			_services.RoomService.LeaveRoom(false);
		}

		private async UniTask PublishMatchStartedMessage(QuantumGame game, bool isResync)
		{
			if (!isResync)
			{
				_services.AnalyticsService.MatchCalls.MatchStart();
				SetPlayerMatchData(game);
			}

			await UniTask.WaitUntil(IsSimulationReady);

			_services.MessageBrokerService.Publish(new MatchStartedMessage {Game = game, IsResync = isResync});
		}

		private unsafe bool IsSimulationReady()
		{
			if (!QuantumRunner.Default.IsDefinedAndRunning()) return false;
			var game = QuantumRunner.Default.Game;

			if (game.GetLocalPlayers().Length == 0 ||
				game.Frames.Verified == null || game.Frames.Verified.Number == 0)
			{
				return false;
			}

			if (!game.Frames.Verified.Unsafe.TryGetPointerSingleton<GameContainer>(out var container))
			{
				return false;
			}

			if (!container->PlayersData[game.GetLocalPlayerRef()].IsValid)
			{
				return false;
			}

			// Waiting specific system initializations until we update photon 2.1.9
			var hasCircle = game.Frames.Verified.Context.GameModeConfig.Systems.Any(s => s == typeof(ShrinkingCircleSystem).FullName);
			if (hasCircle && !game.Frames.Verified.Unsafe.TryGetPointerSingleton<ShrinkingCircle>(out var circle))
			{
				return false;
			}

			return true;
		}

		private void SetPlayerMatchData(QuantumGame game)
		{
			if (IsSpectator())
			{
				return;
			}

			var spawnPosition = _services.RoomService.CurrentRoom.LocalPlayerProperties.DropPosition.Value;
			var equippedCosmetics = _gameDataProvider.CollectionDataProvider
				.GetCollectionsCategories()
				.Select(id => _gameDataProvider.CollectionDataProvider.GetEquipped(id))
				.Where(data => data != null)
				.Select(data => data.Id)
				.ToArray();

			var config = _services.ConfigsProvider.GetConfig<AvatarCollectableConfig>();
			var avatarUrl = AvatarHelpers.GetAvatarUrl(_gameDataProvider.CollectionDataProvider.GetEquipped(CollectionCategories.PROFILE_PICTURE),
				config);
			var useBotBehaviour = (FLGTestRunner.Instance.IsRunning() && FLGTestRunner.Instance.UseBotBehaviour) ||
				FeatureFlags.GetLocalConfiguration().UseBotBehaviour;

#if UNITY_EDITOR || (DEVELOPMENT_BUILD && !DISABLE_SRDEBUGGER)

			if (SROptions.Current.DontSendPlayerData)
			{
				FLog.Warn("Not sending runtime player data. This is a hack for the local player, not an exception!");
				return;
			}
#endif
			FLog.Info("Sending player runtime data");
			game.SendPlayerData(game.GetLocalPlayerRef(), new RuntimePlayer
			{
				PlayerId = PlayFabSettings.staticPlayer.PlayFabId,
				UnityId = AuthenticationService.Instance.PlayerId,
				PlayerName = _services.AuthService.GetPrettyLocalPlayerName(),
				Cosmetics = equippedCosmetics,
				PlayerLevel = _gameDataProvider.PlayerDataProvider.Level.Value,
				PlayerTrophies = _gameDataProvider.PlayerDataProvider.Trophies.Value,
				NormalizedSpawnPosition = spawnPosition.ToFPVector2(),
				LeaderboardRank = (uint) _services.LeaderboardService.CurrentRankedEntry.Position,
				PartyId = _services.TeamService.GetTeamForPlayer(_services.RoomService.CurrentRoom.LocalPlayer),
				AvatarUrl = avatarUrl,
				UseBotBehaviour = useBotBehaviour,
				TeamColor = _services.TeamService.GetTeamMemberColorIndex(_services.RoomService.CurrentRoom.LocalPlayer)
			});
		}

		[Conditional("DEBUG")]
		private void DebugSimulation() => FLog.Verbose(QuantumRunner.Default.GetSimulationDebugString());
	}
}