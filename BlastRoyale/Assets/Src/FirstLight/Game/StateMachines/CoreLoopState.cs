using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Domains.HomeScreen;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.Utils;
using FirstLight.Services;
using FirstLight.Statechart;
using Photon.Realtime;
using Quantum;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic to control the loop between the <see cref="MainMenuState"/>
	/// and the <see cref="MatchState"/> in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class CoreLoopState
	{
		private readonly ReconnectionState _reconnection;
		private readonly MatchState _matchState;
		private readonly MainMenuState _mainMenuState;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly IInternalGameNetworkService _networkService;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		public static bool AUTO_TEST_SCENE => FeatureFlags.GetLocalConfiguration().StartTestGameAutomatically;

		private Coroutine _csPoolTimerCoroutine;

		public CoreLoopState(ReconnectionState reconnection, IGameServices services, IGameDataProvider dataProvider, IDataService dataService,
							 IInternalGameNetworkService networkService,
							 IAssetAdderService assetAdderService, Action<IStatechartEvent> statechartTrigger, IRoomService roomService)
		{
			_services = services;
			_dataProvider = dataProvider;
			_networkService = networkService;
			_statechartTrigger = statechartTrigger;
			_matchState = new MatchState(services, dataService, networkService, dataProvider, assetAdderService, statechartTrigger, roomService);
			_mainMenuState = new MainMenuState(services, dataProvider, assetAdderService, statechartTrigger);
			_reconnection = reconnection;
		}

		/// <summary>
		/// Setups the Adventure gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var firstMatchCheck = stateFactory.Choice("First Match Check");
			var reconnection = stateFactory.Nest("Reconnection");
			var match = stateFactory.Nest("Match");
			var mainMenu = stateFactory.Nest("Main Menu");
			var joinTutorialRoom = stateFactory.State("Room Join Wait");
			var connectionWait = stateFactory.TaskWait("Connection Wait");
			var joinTestScene = stateFactory.State("Join Test Scene");

			initial.Transition().Target(connectionWait);
			initial.OnExit(SubscribeEvents);

			connectionWait.WaitingFor(WaitForPhotonConnection).Target(reconnection);

			reconnection.Nest(_reconnection.Setup).Target(firstMatchCheck);

			firstMatchCheck.Transition().Condition(() => AUTO_TEST_SCENE).Target(joinTestScene);
			firstMatchCheck.Transition().Condition(InRoom)
				.OnTransition(() => _services.MessageBrokerService.Publish(new CoreLoopInitialized {ConnectedToMatch = true}))
				.Target(match);
			firstMatchCheck.Transition().Condition(HasCompletedFirstGameTutorial)
				.OnTransition(() => _services.MessageBrokerService.Publish(new CoreLoopInitialized {ConnectedToMatch = false}))
				.Target(mainMenu);
			firstMatchCheck.Transition().Target(joinTutorialRoom);

			mainMenu.Nest(_mainMenuState.Setup).Target(match);

			match.Nest(_matchState.Setup).Target(mainMenu);

			// TODO - Decide what to do if join room fails
			joinTutorialRoom.OnEnter(AttemptJoinTutorialRoom);
			joinTutorialRoom.Event(NetworkState.JoinedRoomEvent).Target(match);

			joinTestScene.OnEnter(UniTask.Action(AttemptJoinTestGame));
			joinTestScene.Event(NetworkState.JoinedRoomEvent).Target(match);

			final.OnEnter(UnsubscribeEvents);
		}

		private bool InRoom()
		{
			FLog.Info("InRoom: " + _networkService.QuantumClient.InRoom + " Status: " + _networkService.QuantumClient.State);
			return _networkService.InRoom;
		}

		private async UniTask WaitForPhotonConnection()
		{
			while (!_services.NetworkService.QuantumClient.IsConnectedAndReady ||
				   _services.NetworkService.QuantumClient.Server == ServerConnection.NameServer)
			{
				await UniTask.Yield();
			}
		}

		private async UniTask TutorialJoinTask(bool transition = true)
		{
			await _services.UIService.CloseScreen<PrivacyDialogPresenter>(false);
			if (transition)
			{
				// This is an ugly hack - if we dont wait a second here the state machine will get back to iddle state
				// before the event of starting the match is fireds causing an infinite loop and crash.
				// This can still happen on some devices so this hack needs to be solved.
				await UniTask.Delay(GameConstants.Tutorial.TIME_1000MS);
			}

			_services.MessageBrokerService.Publish(new RequestStartFirstGameTutorialMessage());
		}

		private UniTask TransitionScreen()
		{
			return _services.UIService.OpenScreen<SwipeTransitionScreenPresenter>();
		}

		private void AttemptJoinTutorialRoom()
		{
			TutorialJoinTask().Forget();
		}

		private async UniTaskVoid AttemptJoinTestGame()
		{
			await UniTask.Delay(250);

			var config = await Addressables.LoadAssetAsync<SimulationConfigAsset>(AddressableId.Configs_Settings_SimulationConfig.GetConfig()
				.Address);
			config.Settings.DeltaTimeType = SimulationUpdateTime.EngineDeltaTime;
			_services.RoomService.CreateRoom(new MatchRoomSetup()
			{
				RoomIdentifier = Guid.NewGuid().ToString(),
				SimulationConfig = new SimulationMatchConfig()
				{
					MapId = GameId.TestScene.ToString(),
					GameModeID = "Testing",
					MatchType = MatchType.Custom,
					TeamSize = 1,
					UniqueConfigId = "duashhuasd",
				}
			}, true);
		}

		private void SubscribeEvents()
		{
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
		}

		private bool HasCompletedFirstGameTutorial()
		{
			return _services.TutorialService.HasCompletedTutorialSection(TutorialSection.FIRST_GUIDE_MATCH);
		}
	}
}