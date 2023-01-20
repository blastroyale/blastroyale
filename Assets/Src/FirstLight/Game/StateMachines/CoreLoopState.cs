using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using FirstLight.Statechart;
using I2.Loc;
using Photon.Realtime;
using Quantum;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic to control the loop between the <see cref="MainMenuState"/>
	/// and the <see cref="MatchState"/> in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class CoreLoopState
	{
		private readonly MatchState _matchState;
		private readonly MainMenuState _mainMenuState;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly IGameBackendNetworkService _networkService;
		private readonly IGameUiService _uiService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		
		private Coroutine _csPoolTimerCoroutine;

		public CoreLoopState(IGameServices services, IGameDataProvider dataProvider, IDataService dataService, IGameBackendNetworkService networkService, IGameUiService uiService, IGameLogic gameLogic, 
		                     IAssetAdderService assetAdderService, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_dataProvider = dataProvider;
			_networkService = networkService;
			_uiService = uiService;
			_statechartTrigger = statechartTrigger;
			_matchState = new MatchState(services, dataService, networkService, uiService, gameLogic, assetAdderService, statechartTrigger);
			_mainMenuState = new MainMenuState(services, uiService, gameLogic, assetAdderService, statechartTrigger);
		}

		/// <summary>
		/// Setups the Adventure gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var firstMatchCheck = stateFactory.Choice("First Match Check");
			var match = stateFactory.Nest("Match");
			var mainMenu = stateFactory.Nest("Main Menu");
			var joinTutorialRoom = stateFactory.State("Room Join Wait");
			var connectionWait = stateFactory.Wait("Connection Wait");
			
			initial.Transition().Target(connectionWait);
			initial.OnExit(SubscribeEvents);
			
			connectionWait.WaitingFor(WaitForPhotonConnection).Target(firstMatchCheck);
			
			firstMatchCheck.Transition().Condition(IsFirstTimeGuest).Target(joinTutorialRoom);
			firstMatchCheck.Transition().Target(mainMenu);
			
			mainMenu.Nest(_mainMenuState.Setup).Target(match);

			match.Nest(_matchState.Setup).Target(mainMenu);

			// TODO - Decide what to do if join room fails
			joinTutorialRoom.OnEnter(AttemptJoinTutorialRoom);
			joinTutorialRoom.Event(NetworkState.JoinedRoomEvent).Target(match);
			joinTutorialRoom.Event(NetworkState.JoinRoomFailedEvent).Target(mainMenu);
			
			final.OnEnter(UnsubscribeEvents);
		}

		private async void WaitForPhotonConnection(IWaitActivity activity)
		{
			while (!_services.NetworkService.QuantumClient.IsConnectedAndReady)
			{
				await Task.Yield();
			}
			
			activity.Complete();
		}

		private async void AttemptJoinTutorialRoom()
		{
			await _uiService.OpenUiAsync<SwipeScreenPresenter>();
			await _uiService.CloseUi<LoadingScreenPresenter>();
			await Task.Delay(1000);

			_statechartTrigger(TutorialState.StartFirstGameTutorialEvent);
		}

		private void SubscribeEvents()
		{
			
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
		}
		
		private bool IsFirstTimeGuest()
		{
			return string.IsNullOrEmpty(_dataProvider.AppDataProvider.LastLoginEmail.Value);
		}

		private void CallLeaveRoom()
		{
			_services.MessageBrokerService.Publish(new RoomLeaveClickedMessage());
		}
	}
}