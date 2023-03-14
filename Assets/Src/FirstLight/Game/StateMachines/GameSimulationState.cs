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
using FirstLight.Game.Data;
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
using Extensions = FirstLight.Game.Utils.Extensions;
using PlayerMatchData = FirstLight.Game.Services.PlayerMatchData;
using Random = UnityEngine.Random;

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
			var disconnected = stateFactory.State("Disconnected");
			var disconnectedCritical = stateFactory.State("Disconnected Critical");

			initial.Transition().Target(startSimulation);
			initial.OnExit(SubscribeEvents);
			
			startSimulation.OnEnter(StartSimulation);
			startSimulation.Event(SimulationStartedEvent).Target(modeCheck);
			startSimulation.Event(NetworkState.LeftRoomEvent).Target(final);
			startSimulation.OnExit(CloseSwipeTransitionTutorial);

			modeCheck.OnEnter(OpenAdventureWorldHud);
			modeCheck.OnEnter(OpenLowConnectionScreen);
			modeCheck.Transition().Condition(ShouldUseDeathmatchSM).Target(deathmatch);
			modeCheck.Transition().Condition(ShouldUseBattleRoyaleSM).Target(battleRoyale);
			modeCheck.Transition().Target(battleRoyale);

			deathmatch.Nest(_deathmatchState.Setup).Target(final);
			deathmatch.Event(NetworkState.PhotonDisconnectedEvent).Target(disconnected);
			deathmatch.OnExit(CleanUpMatch);

			battleRoyale.Nest(_battleRoyaleState.Setup).Target(final);
			battleRoyale.Event(NetworkState.PhotonDisconnectedEvent).Target(disconnected);
			battleRoyale.OnExit(CleanUpMatch);

			disconnected.OnEnter(StopSimulation);
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
		private void CloseSwipeTransitionTutorial()
		{
			if (_uiService.HasUiPresenter<SwipeScreenPresenter>() && _services.TutorialService.IsTutorialRunning)
			{
				_uiService.CloseUi<SwipeScreenPresenter>(true);
			}
		}

		private void SubscribeEvents()
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			_services.MessageBrokerService.Subscribe<QuitGameClickedMessage>(OnQuitGameScreenClickedMessage);
			_matchServices.SpectateService.SpectatedPlayer.InvokeObserve(TO_DELETE_WITH_NEW_START_SEQUENCE);

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
			if (!_services.NetworkService.LocalPlayer.IsSpectator())
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
			startParams.NetworkClient = client;

			QuantumRunner.StartGame(_services.NetworkService.UserId, startParams);

			_services.MessageBrokerService.Publish(new MatchSimulationStartedMessage());

			_services.NetworkService.EnableClientUpdate(false);
		}


		/// <summary>
		/// This StopSimulation method is only used for disconnection flow.
		/// There is another StopSimulation method in MatchState which handles stopping simulation once the player
		/// has reached the complete end of flow, past any disconnection cases.
		/// </summary>
		private void StopSimulation()
		{
			_services.MessageBrokerService.Publish(new MatchSimulationEndedMessage { Game = QuantumRunner.Default.Game });
			_services.NetworkService.EnableClientUpdate(true);
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

			_services.MessageBrokerService.Publish(new MatchStartedMessage {Game = game, IsResync = isResync});
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
				DeathMarker = info.DeathMarker,
				PlayerLevel = info.Level,
				PlayerTrophies = info.TotalTrophies,
				NormalizedSpawnPosition = spawnPosition.ToFPVector2(),
				Loadout = loadoutArray,
				LoadoutMetadata = loadoutMetadata,
				PartyId = GetTeamId()
			});
		}
	}
}