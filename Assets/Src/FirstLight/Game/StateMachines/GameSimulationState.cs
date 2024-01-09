using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Src.FirstLight.Game.Commands.QuantumLogicCommands;
using ExitGames.Client.Photon;
using FirstLight.FLogger;
using Cinemachine;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes.Helpers;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.TestCases;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using I2.Loc;
using Photon.Deterministic;
using Quantum;
using Quantum.Commands;
using UnityEngine;


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
		private readonly IGameUiService _uiService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly IGameNetworkService _network;
		private readonly IInternalGameNetworkService _networkService;

		private IMatchServices _matchServices;

		public GameSimulationState(IGameDataProvider gameDataProvider, IGameServices services, IInternalGameNetworkService networkService,
								   IGameUiService uiService, Action<IStatechartEvent> statechartTrigger)
		{
			_gameDataProvider = gameDataProvider;
			_services = services;
			_networkService = networkService;
			_uiService = uiService;
			_statechartTrigger = statechartTrigger;
			_battleRoyaleState = new BattleRoyaleState(services, uiService, statechartTrigger);
		}

		/// <summary>
		/// Setups the Game Simulation state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");

			var battleRoyale = stateFactory.Nest("Battle Royale Mode");
			var modeCheck = stateFactory.Choice("Game Mode Check");
			var startSimulation = stateFactory.State("Start Simulation");
			var stopSimulationForDisconnection = stateFactory.State("Stop Simulation");
			var simulationInitializationError = stateFactory.Choice("Stop Simulation Initialization Error");
			var disconnected = stateFactory.State("Disconnected");
			var disconnectedCritical = stateFactory.State("Disconnected Critical");

			initial.Transition().Target(startSimulation);
			initial.OnExit(SubscribeEvents);

			startSimulation.OnEnter(StartSimulation);
			startSimulation.Event(SimulationStartedEvent).Target(modeCheck);
			startSimulation.Event(SimulationDestroyedEvent).Target(simulationInitializationError);
			startSimulation.Event(NetworkState.LeftRoomEvent).Target(final);
			startSimulation.Event(NetworkState.PhotonDisconnectedEvent).Target(stopSimulationForDisconnection);
			startSimulation.OnExit(CloseSwipeTransition);

			//modeCheck.OnEnter(OpenAdventureWorldHud);
			// TODO: modeCheck.OnEnter(OpenLowConnectionScreen);
			modeCheck.Transition().Condition(ShouldUseBattleRoyaleSM).Target(battleRoyale);
			modeCheck.Transition().Target(battleRoyale);

			battleRoyale.Nest(_battleRoyaleState.Setup).Target(final);
			battleRoyale.Event(NetworkState.PhotonDisconnectedEvent).Target(stopSimulationForDisconnection);
			battleRoyale.OnExit(CleanUpMatch);

			simulationInitializationError.Transition().OnTransition(() => _ = MatchError()).Target(final);

			stopSimulationForDisconnection.OnEnter(StopSimulation);
			stopSimulationForDisconnection.Event(NetworkState.JoinedRoomEvent).OnTransition(UnloadSimulation).Target(startSimulation);
			stopSimulationForDisconnection.Event(NetworkState.JoinRoomFailedEvent).OnTransition(UnloadSimulation).Target(disconnectedCritical);

			disconnected.Event(NetworkState.JoinedRoomEvent).Target(startSimulation);
			disconnected.Event(NetworkState.JoinRoomFailedEvent).Target(disconnectedCritical);

			final.OnEnter(UnloadSimulationUi);
			final.OnEnter(UnsubscribeEvents);
		}

		/// <summary>
		/// For tutorial, we close the swipe transition when we actually get into the game, instead of
		/// closing at matchmaking screen opening in matchState. This is to avoid visual glitches with MM screen
		/// still persisting on screen for a second before game simulation
		/// </summary>
		private void CloseSwipeTransition()
		{
			_ = SwipeScreenPresenter.Finish();
		}

		private void SubscribeEvents()
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			_services.MessageBrokerService.Subscribe<QuitGameClickedMessage>(OnQuitGameScreenClickedMessage);

			QuantumEvent.SubscribeManual<EventOnAllPlayersJoined>(this, OnAllPlayersJoined);
			QuantumCallback.SubscribeManual<CallbackGameStarted>(this, OnGameStart);
			QuantumCallback.SubscribeManual<CallbackGameResynced>(this, OnGameResync);
			QuantumCallback.SubscribeManual<CallbackGameDestroyed>(this, OnGameDestroyed);
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

		private void UnloadSimulationUi()
		{
			if (_uiService.HasUiPresenter<LowConnectionPresenter>())
			{
				_uiService.UnloadUi<LowConnectionPresenter>();
			}
		}

		private void OpenLowConnectionScreen()
		{
			_uiService.LoadUiAsync<LowConnectionPresenter>(true);
		}

		private void OpenDisconnectedMatchEndDialog()
		{
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};

			StopSimulation();
			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.info,
				ScriptLocalization.MainMenu.DisconnectedMatchEndInfo.ToUpper(), false, confirmButton);
		}

		private void OnGameDestroyed(CallbackGameDestroyed cb)
		{
			FLog.Verbose("Game Destroyed");
			_statechartTrigger(SimulationDestroyedEvent);
		}

		private async UniTask WaitForCameraOnPlayer()
		{
			await UniTask.WaitUntil(IsSpectatingPlayer);
		}

		private bool IsSpectatingPlayer()
		{
			if (!QuantumRunner.Default.IsDefinedAndRunning() || _matchServices == null) return false;
			var spectated = _matchServices.SpectateService.SpectatedPlayer.Value;
			if (!spectated.Entity.IsValid) return false;
			return true;
		}

		private async UniTaskVoid CloseMatchmakingScreen()
		{
			await WaitForCameraOnPlayer();
			await _uiService.CloseUi<CustomLobbyScreenPresenter>();
			await _uiService.CloseUi<PreGameLoadingScreenPresenter>();
		}

		private bool IsSpectator()
		{
			return _services.RoomService.IsLocalPlayerSpectator;
		}

		private string GetTeamId()
		{
			return _services.RoomService.CurrentRoom.LocalPlayerProperties.TeamId.Value;
		}


		private bool ShouldUseBattleRoyaleSM()
		{
			return _services.RoomService.CurrentRoom.GameModeConfig.AudioStateMachine ==
				AudioStateMachine.BattleRoyale;
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

			// Delays one frame just to guarantee that the game objects are created before anything else

			_services.CoroutineService.StartCoroutine(GameStartCoroutine(callback.Game));
			FLog.Verbose("Waiting for all players to join");
		}

		private IEnumerator GameStartCoroutine(QuantumGame game)
		{
			yield return new WaitForSeconds(0.1f);
			PublishMatchStartedMessage(game, false);
			yield return new WaitForSeconds(1f);
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
			FLog.Verbose("Players Joined");
			// paused on Start means waiting for Snapshot
			if (callback.Game.Session.IsPaused)
			{
				return;
			}

			_statechartTrigger(SimulationStartedEvent);
			_ = CloseMatchmakingScreen();
		}

		private void OnGameResync(CallbackGameResynced callback)
		{
			FLog.Verbose(
				$"Game Resync {callback.Game.Frames.Verified.Number} vs {_gameDataProvider.AppDataProvider.LastFrameSnapshot.Value.FrameNumber}");

			_ = ResyncCoroutine();
		}

		private async UniTaskVoid ResyncCoroutine()
		{
			await UniTask.NextFrame();
			PublishMatchStartedMessage(QuantumRunner.Default.Game, true);
			_statechartTrigger(SimulationStartedEvent);
			CloseMatchmakingScreen().Forget();
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

		private async UniTask MatchError()
		{
			FLog.Verbose("Raising Match Error");
			await UniTask.NextFrame(); // to avoid state machine fork https://tree.taiga.io/project/firstlightgames-blast-royale-reloaded/issue/2737
			_statechartTrigger(MatchState.MatchErrorEvent);
		}

		private void StartSimulation()
		{
			if (QuantumRunner.Default != null)
			{
				FLog.Error("Starting simulation while another still active");
			}

			FLog.Info($"Starting simulation from source {_services.NetworkService.JoinSource.ToString()}");

			var client = _services.NetworkService.QuantumClient;

			var startParams = _services.RoomService.CurrentRoom.GetDefaultStartParameters();
			startParams.NetworkClient = client;
			if (IsSpectator())
			{
				startParams.GameMode = DeterministicGameMode.Spectating;
			}

			var snapShot = _gameDataProvider.AppDataProvider.LastFrameSnapshot.Value;
			if (snapShot.FrameNumber > 0 && _services.NetworkService.CurrentRoom.CanBeRestoredWithLocalSnapshot())
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
			QuantumRunner.ShutdownAll(true);
			_services.RoomService.LeaveRoom(false);
		}

		private void CleanUpMatch()
		{
			_services.VfxService.DespawnAll();
		}

		// private void OpenAdventureWorldHud()
		// {
		// 	_uiService.OpenUi<MatchWorldHudPresenter>();
		// }

		private void PublishMatchStartedMessage(QuantumGame game, bool isResync)
		{
			if (!isResync)
			{
				_services.AnalyticsService.MatchCalls.MatchStart();
				SetPlayerMatchData(game);
			}
			
			_services.MessageBrokerService.Publish(new MatchStartedMessage {Game = game, IsResync = isResync});
		}

		private void SetPlayerMatchData(QuantumGame game)
		{
			if (IsSpectator())
			{
				return;
			}

			var loadout = _gameDataProvider.EquipmentDataProvider.Loadout;
			var inventory = _gameDataProvider.EquipmentDataProvider.Inventory;
			var f = game.Frames.Verified;
			var spawnPosition = _services.RoomService.CurrentRoom.LocalPlayerProperties.DropPosition.Value;
			var loadoutArray = loadout.ReadOnlyDictionary.Values.Select(id => inventory[id]).ToArray();
			var nftLoadout = _gameDataProvider.EquipmentDataProvider.GetLoadoutEquipmentInfo(EquipmentFilter.NftOnly);
			var loadoutMetadata = loadoutArray.Select(e => new EquipmentSimulationMetadata()
			{
				IsNft = nftLoadout.Any(nft => nft.Equipment.Equals(e))
			}).ToArray();

			var equippedCosmetics = _gameDataProvider.CollectionDataProvider
				.GetCollectionsCategories()
				.Select(id => _gameDataProvider.CollectionDataProvider.GetEquipped(id))
				.Where(data => data != null)
				.Select(data => data.Id)
				.ToArray();


			var config = _services.ConfigsProvider.GetConfig<AvatarCollectableConfig>();
			var avatarUrl = AvatarHelpers.GetAvatarUrl(_gameDataProvider.CollectionDataProvider.GetEquipped(CollectionCategories.PROFILE_PICTURE),
				config);
			game.SendPlayerData(game.GetLocalPlayerRef(), new RuntimePlayer
			{
				PlayerId = _gameDataProvider.AppDataProvider.PlayerId,
				PlayerName = _gameDataProvider.AppDataProvider.DisplayNameTrimmed,
				Cosmetics = equippedCosmetics,
				PlayerLevel = _gameDataProvider.PlayerDataProvider.Level.Value,
				PlayerTrophies = _gameDataProvider.PlayerDataProvider.Trophies.Value,
				NormalizedSpawnPosition = spawnPosition.ToFPVector2(),
				Loadout = loadoutArray,
				LoadoutMetadata = loadoutMetadata,
				LeaderboardRank = (uint) _services.LeaderboardService.CurrentRankedEntry.Position,
				PartyId = GetTeamId(),
				AvatarUrl = avatarUrl,
				UseBotBehaviour = FLGTestRunner.Instance.IsRunning() && FLGTestRunner.Instance.UseBotBehaviour,
				TeamColor = _services.RoomService.CurrentRoom.LocalPlayerProperties.ColorIndex.Value
			});
		}

		[Conditional("DEBUG")]
		private void DebugSimulation() => FLog.Verbose(QuantumRunner.Default.GetSimulationDebugString());
	}
}