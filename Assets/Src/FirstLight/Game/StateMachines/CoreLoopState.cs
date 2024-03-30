using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.Utils;
using FirstLight.Services;
using FirstLight.Statechart;
using Photon.Realtime;
using UnityEngine;

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
		
		private Coroutine _csPoolTimerCoroutine;

		public CoreLoopState(ReconnectionState reconnection, IGameServices services, IGameDataProvider dataProvider, IDataService dataService, IInternalGameNetworkService networkService, IGameLogic gameLogic, 
		                     IAssetAdderService assetAdderService, Action<IStatechartEvent> statechartTrigger, IRoomService roomService)
		{
			_services = services;
			_dataProvider = dataProvider;
			_networkService = networkService;
			_statechartTrigger = statechartTrigger;
			_matchState = new MatchState(services, dataService, networkService, gameLogic, assetAdderService, statechartTrigger,roomService);
			_mainMenuState = new MainMenuState(services, gameLogic, assetAdderService, statechartTrigger);
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
			
			initial.Transition().Target(connectionWait);
			initial.OnExit(SubscribeEvents);
			
			connectionWait.WaitingFor(WaitForPhotonConnection).Target(reconnection);

			reconnection.Nest(_reconnection.Setup).Target(firstMatchCheck);

			firstMatchCheck.Transition().Condition(InRoom).Target(match);
			firstMatchCheck.Transition().Condition(CheckSkipTutorial).Target(mainMenu);
			firstMatchCheck.Transition().Condition(HasCompletedFirstGameTutorial).Target(mainMenu);
			firstMatchCheck.Transition().Target(joinTutorialRoom);
			
			mainMenu.Nest(_mainMenuState.Setup).Target(match);

			match.Nest(_matchState.Setup).Target(mainMenu);

			// TODO - Decide what to do if join room fails
			joinTutorialRoom.OnEnter(AttemptJoinTutorialRoom);
			joinTutorialRoom.Event(NetworkState.JoinedRoomEvent).Target(match);
			
			final.OnEnter(UnsubscribeEvents);
		}

		private bool InRoom()
		{
			FLog.Info("InRoom: "+_networkService.QuantumClient.InRoom+" Status: "+_networkService.QuantumClient.State);
			return _networkService.InRoom;
		}

		private async UniTask WaitForPhotonConnection()
		{
			while (!_services.NetworkService.QuantumClient.IsConnectedAndReady || _services.NetworkService.QuantumClient.Server == ServerConnection.NameServer)
			{
				await UniTask.Yield();
			}
		}

		/// <summary>
		/// If player already have items equipped, he does not need to do tutorial
		/// This is to allow players to skip tutorial if they are not new as we implement more tutorial steps.
		///
		/// The main reason is to easily avoid edge cases e.g a player which item on slot 1 in inventory equipped but
		/// meta tutorial will ask player to equip while blocking the UI, soft locking the game.
		/// </summary>
		private bool CheckSkipTutorial()
		{
			if (_dataProvider.EquipmentDataProvider.Loadout.Count >= 1)
			{
				if (!_services.TutorialService.HasCompletedTutorialSection(TutorialSection.FIRST_GUIDE_MATCH))
				{
					_services.CommandService.ExecuteCommand(new CompleteTutorialSectionCommand()
					{
						Section = TutorialSection.FIRST_GUIDE_MATCH
					});
				}

				return true;
			}
			return false;
		}

		private async UniTask TutorialJoinTask(bool transition = false)
		{
			await _services.UIService.CloseScreen<PrivacyDialogPresenter>();
			if (transition)
			{
				await TransitionScreen();
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

		private async UniTask AcceptPrivacyDialog()
		{
			await TransitionScreen();
			var data = new PrivacyDialogPresenter.StateData
			{
				OnAccept = AcceptTerms
			};
			await _services.UIService.OpenScreen<PrivacyDialogPresenter>(data);
		}

		private void AcceptTerms()
		{
			
			_ = TutorialJoinTask();
		}

		private void AttemptJoinTutorialRoom()
		{
			if (_dataProvider.AppDataProvider.IsFirstSession)
			{
				_ = AcceptPrivacyDialog();
				return;
			}
			_ = TutorialJoinTask(true);
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
			return !FeatureFlags.TUTORIAL ||_services.TutorialService.HasCompletedTutorialSection(TutorialSection.FIRST_GUIDE_MATCH);
		}
	}
}