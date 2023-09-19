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
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Data;
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
using Debug = UnityEngine.Debug;


namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Adventure Game Simulation State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class GameSimulationState
	{
		public static readonly IStatechartEvent SimulationStartedEvent = new StatechartEvent("Simulation Ready Event");
		public static readonly IStatechartEvent SimulationDestroyedEvent = new StatechartEvent("Simulation Destroyed Event");

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
			var criticalMatchError = stateFactory.Transition("Critical Match Error");

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
			
			simulationInitializationError.Transition().OnTransition(MatchError).Target(criticalMatchError);

			stopSimulationForDisconnection.OnEnter(StopSimulation);
			stopSimulationForDisconnection.Event(NetworkState.JoinedRoomEvent).OnTransition(UnloadSimulation).Target(startSimulation);
			stopSimulationForDisconnection.Event(NetworkState.JoinRoomFailedEvent).OnTransition(UnloadSimulation).Target(disconnectedCritical);
			
			disconnected.Event(NetworkState.JoinedRoomEvent).Target(startSimulation);
			disconnected.Event(NetworkState.JoinRoomFailedEvent).Target(disconnectedCritical);
			
			criticalMatchError.Transition().Target(final);
			
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
			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.info, ScriptLocalization.MainMenu.DisconnectedMatchEndInfo.ToUpper(), false, confirmButton);
		}

		private void OnGameDestroyed(CallbackGameDestroyed cb)
		{
			FLog.Verbose("Game Destroyed");
			_statechartTrigger(SimulationDestroyedEvent);
		}

		private async Task WaitForCameraOnPlayer()
		{
			var spectated = _matchServices.SpectateService.SpectatedPlayer.Value;
			while (QuantumRunner.Default.IsDefinedAndRunning() && !spectated.Entity.IsValid)
			{
				spectated = _matchServices.SpectateService.SpectatedPlayer.Value;
				if (!spectated.Entity.IsValid)
				{
					await Task.Delay(1);
				}
			}
		}
		
		private async Task CloseMatchmakingScreen()
		{
			await WaitForCameraOnPlayer();
			_uiService.CloseUi<CustomLobbyScreenPresenter>();
			_uiService.CloseUi<MatchmakingScreenPresenter>();
		}

		private bool IsSpectator()
		{
			return _services.NetworkService.LocalPlayer.IsSpectator();
		}

		private string GetTeamId()
		{
			return _services.NetworkService.LocalPlayer.GetTeamId();
		}

		private bool IsCustomMatch()
		{
			return _services.NetworkService.CurrentRoom.GetMatchType() == MatchType.Custom;
		}

		private bool ShouldUseDeathmatchSM()
		{
			return _services.NetworkService.CurrentRoomGameModeConfig.Value.AudioStateMachine ==
				AudioStateMachine.Deathmatch;
		}

		private bool ShouldUseBattleRoyaleSM()
		{
			return _services.NetworkService.CurrentRoomGameModeConfig.Value.AudioStateMachine ==
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
		}
		
		private void OnAllPlayersJoined(EventOnAllPlayersJoined callback)
		{
			FLog.Verbose("Players Joined");
			// paused on Start means waiting for Snapshot
			if (callback.Game.Session.IsPaused)
			{
				return;
			}

			TryEnableClientUpdate();
			_statechartTrigger(SimulationStartedEvent);
			
			Task.Yield();
			
			CloseMatchmakingScreen();
		}

		private void OnGameResync(CallbackGameResynced callback)
		{
			FLog.Verbose(
				$"Game Resync {callback.Game.Frames.Verified.Number} vs {_gameDataProvider.AppDataProvider.LastFrameSnapshot.Value.FrameNumber}");
			TryEnableClientUpdate();
			_services.CoroutineService.StartCoroutine(ResyncCoroutine());
		}

		private void TryEnableClientUpdate()
		{
			// Client update needs to be enabled in offline rooms, and disabled in online ones, otherwise 
			// many things break (different breakage for both online and offline)
			_services.NetworkService.EnableClientUpdate(_services.NetworkService.CurrentRoom.IsOffline);
		}

		private IEnumerator ResyncCoroutine()
		{
			yield return new WaitForSeconds(0.1f);
			PublishMatchStartedMessage(QuantumRunner.Default.Game, true);
			_statechartTrigger(SimulationStartedEvent);
			CloseMatchmakingScreen();
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
			if (!_services.NetworkService.LocalPlayer.IsSpectator())
			{
				QuantumRunner.Default.Game.SendCommand(new PlayerQuitCommand());
			}

			_statechartTrigger(MatchState.MatchQuitEvent);
		}

		private void MatchError()
		{
			FLog.Verbose("Raising Match Error");
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
			var configs = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();

			var startParams = configs.GetDefaultStartParameters(client.CurrentRoom);
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
			_services.NetworkService.LeaveRoom(false, false);
			_services.NetworkService.EnableClientUpdate(true);
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
			var spawnPosition = _services.NetworkService.LocalPlayer.GetDropPosition();
			var spawnWithloadout = f.Context.GameModeConfig.SpawnWithGear || f.Context.GameModeConfig.SpawnWithWeapon;
			var finalLoadOut = new List<Equipment>();

			foreach (var item in loadout.ReadOnlyDictionary.Values.ToList())
			{
				var itemId = inventory[item.Id];
				if (itemId.GameId.IsInGroup(GameIdGroup.Gear) && !f.Context.GameModeConfig.SpawnWithGear)
				{
					continue;
				}

				if (itemId.GameId.IsInGroup(GameIdGroup.Weapon) &&
				    (!f.Context.GameModeConfig.SpawnWithWeapon || f.Context.TryGetMutatorByType(MutatorType.HammerTime, out _)))
				{
					continue;
				}

				finalLoadOut.Add(inventory[item.Id]);
			}

			var loadoutArray = spawnWithloadout
				? finalLoadOut.ToArray()
				: loadout.ReadOnlyDictionary.Values.Select(id => inventory[id]).ToArray();
			
			var nftLoadout = _gameDataProvider.EquipmentDataProvider.GetLoadoutEquipmentInfo(EquipmentFilter.NftOnly);
			var loadoutMetadata = loadoutArray.Select(e => new EquipmentSimulationMetadata()
			{
				IsNft = nftLoadout.Any(nft => nft.Equipment.Equals(e))
			}).ToArray();
			game.SendPlayerData(game.GetLocalPlayerRef(), new RuntimePlayer
			{
				PlayerId = _gameDataProvider.AppDataProvider.PlayerId,
				PlayerName = _gameDataProvider.AppDataProvider.DisplayNameTrimmed,
				Skin = _gameDataProvider.CollectionDataProvider.GetEquipped(new (GameIdGroup.PlayerSkin)).Id,
				DeathMarker = _gameDataProvider.CollectionDataProvider.GetEquipped(new(GameIdGroup.DeathMarker)).Id,
				Glider = _gameDataProvider.CollectionDataProvider.GetEquipped(new(GameIdGroup.Glider)).Id,
				PlayerLevel = _gameDataProvider.PlayerDataProvider.Level.Value,
				PlayerTrophies = _gameDataProvider.PlayerDataProvider.Trophies.Value,
				NormalizedSpawnPosition = spawnPosition.ToFPVector2(),
				Loadout = loadoutArray,
				LoadoutMetadata = loadoutMetadata,
				LeaderboardRank = (uint)_services.LeaderboardService.CurrentRankedEntry.Position,
				PartyId = GetTeamId(),
				AvatarUrl = _gameDataProvider.AppDataProvider.AvatarUrl,
				UseBotBehaviour = FLGTestRunner.Instance.IsRunning() && FLGTestRunner.Instance.UseBotBehaviour
			});
		}
		
		[Conditional("DEBUG")]
		private void DebugSimulation() => FLog.Verbose(QuantumRunner.Default.GetSimulationDebugString());
	}
}