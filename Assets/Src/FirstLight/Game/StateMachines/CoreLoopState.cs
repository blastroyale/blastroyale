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
using FirstLight.Game.Services;
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
		
		private Coroutine _csPoolTimerCoroutine;

		public CoreLoopState(IGameServices services, IGameDataProvider dataProvider, IDataService dataService, IGameBackendNetworkService networkService, IGameUiService uiService, IGameLogic gameLogic, 
		                     IAssetAdderService assetAdderService, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_dataProvider = dataProvider;
			_networkService = networkService;
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
			var joinTutorialRoom = stateFactory.Wait("Room Join Wait");
			
			initial.Transition().Target(firstMatchCheck);
			initial.OnExit(SubscribeEvents);
			
			firstMatchCheck.Transition().Condition(IsFirstTimeGuest).Target(joinTutorialRoom);
			firstMatchCheck.Transition().Target(mainMenu);
			
			mainMenu.Nest(_mainMenuState.Setup).Target(match);

			match.Nest(_matchState.Setup).Target(mainMenu);
			
			joinTutorialRoom.WaitingFor(AttemptJoinTutorialRoom).Target(match);

			final.OnEnter(UnsubscribeEvents);
		}

		private async void AttemptJoinTutorialRoom(IWaitActivity activity)
		{
			// TODO: NETWORK - JOIN ROOM, RESOLVE ACTIVITY ON COMPLETE
			var gameModeId = msg.GameModeConfig.Id;
			var gameModeConfig = _services.ConfigsProvider.GetConfig<QuantumGameModeConfig>(gameModeId.GetHashCode());
			_dataProvider.AppDataProvider.SetLastCustomGameOptions(msg.CustomGameOptions);
			_services.DataSaver.SaveData<AppData>();
			
			_networkService.CreateRoom();
			await Task.Yield();
			activity.Complete();
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

		private bool IsConnectedAndReady()
		{
			return _services.NetworkService.QuantumClient.IsConnectedAndReady;
		}

		private void CallLeaveRoom()
		{
			_services.MessageBrokerService.Publish(new RoomLeaveClickedMessage());
		}
	}
}