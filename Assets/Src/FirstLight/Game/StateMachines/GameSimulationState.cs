using System;
using System.Collections.Generic;
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
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using I2.Loc;
using Photon.Realtime;
using PlayFab;
using Newtonsoft.Json;
using Quantum;
using Quantum.Commands;

using Quantum.Task;
using UnityEngine;
using UnityEngine.SceneManagement;
using PlayerMatchData = FirstLight.Game.Services.PlayerMatchData;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Adventure Game Simulation State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class GameSimulationState
	{
		public static readonly IStatechartEvent SimulationStartedEvent = new StatechartEvent("Simulation Ready Event");

		private readonly DeathmatchState _deathmatchState;
		private readonly BattleRoyaleState _battleRoyaleState;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IGameServices _services;
		private readonly IGameUiService _uiService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly IGameNetworkService _network;
		private readonly IGameBackendNetworkService _networkService;

		private IMatchServices _matchServices; 

		public GameSimulationState(IGameDataProvider gameDataProvider, IGameServices services, IGameBackendNetworkService networkService, 
								   IGameUiService uiService, Action<IStatechartEvent> statechartTrigger)
		{
			_gameDataProvider = gameDataProvider;
			_services = services;
			_networkService = networkService;
			_uiService = uiService;
			_statechartTrigger = statechartTrigger;
			_deathmatchState = new DeathmatchState(gameDataProvider, services, uiService, statechartTrigger);
			_battleRoyaleState = new BattleRoyaleState(services, uiService, statechartTrigger);
		}

		/// <summary>
		/// Setups the Game Simulation state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");

			var deathmatch = stateFactory.Nest("Deathmatch Mode");
			var battleRoyale = stateFactory.Nest("Battle Royale Mode");
			var modeCheck = stateFactory.Choice("Game Mode Check");
			var startSimulation = stateFactory.State("Start Simulation");
			var disconnectedPlayerCheck = stateFactory.Choice("Disconnected Player Check");
			var disconnected = stateFactory.State("Disconnected");
			var disconnectedCritical = stateFactory.State("Disconnected Critical");
			
			initial.Transition().Target(startSimulation);
			initial.OnExit(SubscribeEvents);
			initial.OnExit(OpenLowConnectionScreen);

			startSimulation.OnEnter(StartSimulation);
			startSimulation.Event(SimulationStartedEvent).Target(modeCheck);
			startSimulation.Event(NetworkState.LeftRoomEvent).Target(final);

			modeCheck.OnEnter(OpenAdventureWorldHud);
			modeCheck.Transition().Condition(ShouldUseDeathmatchSM).Target(deathmatch);
			modeCheck.Transition().Condition(ShouldUseBattleRoyaleSM).Target(battleRoyale);
			modeCheck.Transition().Target(battleRoyale);
			//modeCheck.OnExit(CloseMatchmakingScreen); uncomment with new start sequence

			deathmatch.Nest(_deathmatchState.Setup).Target(final);
			deathmatch.Event(NetworkState.PhotonDisconnectedEvent).Target(disconnectedPlayerCheck);
			deathmatch.OnExit(CleanUpMatch);

			battleRoyale.Nest(_battleRoyaleState.Setup).Target(final);
			battleRoyale.Event(NetworkState.PhotonDisconnectedEvent).Target(disconnectedPlayerCheck);
			battleRoyale.OnExit(CleanUpMatch);
			
			// TODO: @ROB move this block out of the game simulation. This belongs to the MatchState. We are duplicating flow and code. It's not needed to be here
			{
				disconnectedPlayerCheck.Transition().Condition(IsSoloGame).OnTransition(OpenDisconnectedMatchEndDialog).Target(final);
				disconnectedPlayerCheck.Transition().Target(disconnected);
			
				disconnected.OnEnter(StopSimulation);
				disconnected.Event(NetworkState.JoinedRoomEvent).Target(startSimulation);
				disconnected.Event(NetworkState.JoinRoomFailedEvent).Target(disconnectedCritical);

				disconnectedCritical.OnEnter(NotifyCriticalDisconnection);
			}

			final.OnEnter(UnloadSimulationUi);
			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			
			_services.MessageBrokerService.Subscribe<QuitGameClickedMessage>(OnQuitGameScreenClickedMessage);
			_matchServices.SpectateService.SpectatedPlayer.InvokeObserve(TO_DELETE_WITH_NEW_START_SEQUENCE);
			
			QuantumEvent.SubscribeManual<EventFireQuantumServerCommand>(this, OnServerCommand);
			QuantumEvent.SubscribeManual<EventOnAllPlayersJoined>(this, OnAllPlayersJoined);
			QuantumCallback.SubscribeManual<CallbackGameStarted>(this, OnGameStart);
			QuantumCallback.SubscribeManual<CallbackGameResynced>(this, OnGameResync);
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
			_matchServices?.SpectateService?.SpectatedPlayer?.StopObserving(TO_DELETE_WITH_NEW_START_SEQUENCE);
			QuantumEvent.UnsubscribeListener(this);
			QuantumCallback.UnsubscribeListener(this);
			
			_matchServices = null;
		}

		private void UnloadSimulationUi()
		{
			_uiService.UnloadUi<LowConnectionPresenter>();
		}

		private bool IsSoloGame()
		{
			return _services.NetworkService.LastMatchPlayers.Count == 1;
		}
		
		private void NotifyCriticalDisconnection()
		{
			_statechartTrigger(NetworkState.PhotonCriticalDisconnectedEvent);
		}

		private void OpenLowConnectionScreen()
		{
			_uiService.OpenUiAsync<LowConnectionPresenter>();
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

		// TODO: Delete with new start sequence visual cinematic
		private void TO_DELETE_WITH_NEW_START_SEQUENCE(SpectatedPlayer spectatedPlayer, SpectatedPlayer player)
		{
			if (player.Player.IsValid)
			{
				CloseMatchmakingScreen();
				_matchServices.SpectateService.SpectatedPlayer.StopObserving(TO_DELETE_WITH_NEW_START_SEQUENCE);
			}
		}

		private void CloseMatchmakingScreen()
		{
			_uiService.CloseUi<CustomLobbyScreenPresenter>();
			_uiService.CloseUi<MatchmakingScreenPresenter>();
		}

		/// <summary>
		/// Whenever the simulation wants to fire logic commands.
		/// This will also run on quantum server and will be sent to logic service from there.
		/// </summary>
		private void OnServerCommand(EventFireQuantumServerCommand ev)
		{
			var game = ev.Game;
			if (!game.PlayerIsLocal(ev.Player))
			{
				return;
			}

			FLog.Verbose("Quantum Logic Command Received: " + ev.CommandType.ToString());
			var command = QuantumLogicCommandFactory.BuildFromEvent(ev);
			command.FromFrame(game.Frames.Verified, new QuantumValues()
			{
				ExecutingPlayer = game.GetLocalPlayers()[0],
				MatchType = _services.NetworkService.QuantumClient.CurrentRoom.GetMatchType()
			});
			_services.CommandService.ExecuteCommand(command as IGameCommand);
		}

		private bool IsSpectator()
		{
			return _services.NetworkService.QuantumClient.LocalPlayer.IsSpectator();
		}
		
		private bool IsCustomMatch()
		{
			return _services.NetworkService.QuantumClient.CurrentRoom.GetMatchType() == MatchType.Custom;
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
		
		private async void OnGameStart(CallbackGameStarted callback)
		{
			// paused on Start means waiting for Snapshot
			if (callback.Game.Session.IsPaused)
			{
				return;
			}

			// Delays one frame just to guarantee that the game objects are created before anything else
			await Task.Yield();

			PublishMatchStartedMessage(callback.Game, false);
		}

		private void OnAllPlayersJoined(EventOnAllPlayersJoined callback)
		{
			// paused on Start means waiting for Snapshot
			if (callback.Game.Session.IsPaused)
			{
				return;
			}
			
			_statechartTrigger(SimulationStartedEvent);
		}

		private async void OnGameResync(CallbackGameResynced callback)
		{
			// Delays one frame just to guarantee that the game objects are created before anything else
			await Task.Yield();

			PublishMatchStartedMessage(callback.Game, true);
			_statechartTrigger(SimulationStartedEvent);
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
			if (!_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator())
			{
				QuantumRunner.Default.Game.SendCommand(new PlayerQuitCommand());
			}
			
			_statechartTrigger(MatchState.MatchQuitEvent);
		}
		
		private void StartSimulation()
		{
			var client = _services.NetworkService.QuantumClient;
			var configs = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();
			var room = client.CurrentRoom;
			
			var startPlayersCount = client.CurrentRoom.GetRealPlayerCapacity();
			
			if (room.CustomProperties.TryGetValue(GameConstants.Network.ROOM_PROPS_BOTS, out var gameHasBots) &&
			    !(bool) gameHasBots)
			{
				startPlayersCount = room.GetRealPlayerAmount();
			}

			var startParams = configs.GetDefaultStartParameters(startPlayersCount, IsSpectator(), new FrameSnapshot());
			
			// Unused for now, once local snapshot issues are ironed out, resyncing solo games can be readded
			if (!_services.NetworkService.IsJoiningNewMatch && _services.NetworkService.LastMatchPlayers.Count == 1)
			{
				startParams = configs.GetDefaultStartParameters(_services.NetworkService.LastMatchPlayers.Count, IsSpectator(), 
					MainInstaller.Resolve<IMatchServices>().FrameSnapshotService.GetLastStoredMatchSnapshot());
			}

			startParams.NetworkClient = client;
			
			QuantumRunner.StartGame(_services.NetworkService.UserId, startParams);
			
			_services.MessageBrokerService.Publish(new MatchSimulationStartedMessage());
		}


		private void StopSimulation()
		{
			_services.MessageBrokerService.Publish(new MatchSimulationEndedMessage { Game = QuantumRunner.Default.Game });
			QuantumRunner.ShutdownAll();
		}
		
		private void CleanUpMatch()
		{
			_services.VfxService.DespawnAll();
		}

		private void OpenAdventureWorldHud()
		{
			_uiService.OpenUi<MatchWorldHudPresenter>();
		}

		private void PublishMatchStartedMessage(QuantumGame game, bool isResync)
		{
			if (_services.NetworkService.IsJoiningNewMatch)
			{
				_services.AnalyticsService.MatchCalls.MatchStart();
				SetPlayerMatchData(game);
			}

			_services.MessageBrokerService.Publish(new MatchStartedMessage { Game = game, IsResync = isResync });
		}

		private void SetPlayerMatchData(QuantumGame game)
		{
			if (IsSpectator())
			{
				return;
			}
			
			var info = _gameDataProvider.PlayerDataProvider.PlayerInfo;
			var loadout = _gameDataProvider.EquipmentDataProvider.Loadout;
			var inventory = _gameDataProvider.EquipmentDataProvider.Inventory;
			var f = game.Frames.Verified;
			var spawnPosition = _services.MatchmakingService.NormalizedMapSelectedPosition;
			var spawnWithloadout = f.Context.GameModeConfig.SpawnWithGear || f.Context.GameModeConfig.SpawnWithWeapon;
			var finalLoadOut = new List<Equipment>();
			
			foreach(var item in loadout.ReadOnlyDictionary.Values.ToList())
			{
				var itemId = inventory[item.Id];
				if(itemId.GameId.IsInGroup(GameIdGroup.Gear) && !f.Context.GameModeConfig.SpawnWithGear)
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

			game.SendPlayerData(game.GetLocalPlayerRef(), new RuntimePlayer
			{
				PlayerId = _gameDataProvider.AppDataProvider.PlayerId,
				PlayerName = _gameDataProvider.AppDataProvider.DisplayNameTrimmed,
				Skin = info.Skin,
				DeathMarker = info.DeathMarker,
				PlayerLevel = info.Level,
				PlayerTrophies = info.TotalTrophies,
				NormalizedSpawnPosition = spawnPosition.ToFPVector2(),
				Loadout = spawnWithloadout ? 
					          finalLoadOut.ToArray() : loadout.ReadOnlyDictionary.Values.Select(id => inventory[id]).ToArray()
			});
		}
	}
}