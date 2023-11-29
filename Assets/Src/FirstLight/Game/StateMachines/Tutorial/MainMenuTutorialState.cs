using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Tutorial;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using FirstLight.Game.Services.Tutorial;
using I2.Loc;
using NUnit.Framework;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	public class MetaAndMatchTutorialState 
	{
		private static readonly IStatechartEvent _selectedMapPointEvent = new StatechartEvent("TUTORIAL - Selected map point event");
	
		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly IInternalTutorialService _tutorialService;
		
		private IMatchServices _matchServices;
		private TutorialUtilsScreenPresenter _tutorialUtilsUi;
		private CharacterDialogScreenPresenter _dialogUi;
		private MetaTutorialSequence _sequence;

		public MetaAndMatchTutorialState(IGameDataProvider logic, IGameServices services,
										 IInternalTutorialService tutorialService,
										 Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_dataProvider = logic;
			_tutorialService = tutorialService;
			_statechartTrigger = statechartTrigger;
			_sequence = new MetaTutorialSequence(_services, TutorialSection.FIRST_GUIDE_MATCH);
		}

		/// <summary> 
		/// Setups the audio state - root state, and then per gamemode type nested states
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var enterName = stateFactory.State("Enter name");
			var completionCheck = stateFactory.Choice("Completion check");
			var playGame = stateFactory.State("Play game");
			var mapSelect = stateFactory.State("Map Select");
			var disconnected = stateFactory.State("Disconnected");
			var createTutorialRoom = stateFactory.State("Join Room");
			var waitSimulationStart = stateFactory.State("WaitSimulationStart");
			
			initial.Transition().Target(enterName);
			initial.OnExit(SubscribeMessages);
			initial.OnExit(_services.GameModeService.SelectDefaultRankedMode);
			initial.OnExit(GetTutorialScreenRefs);
			
			enterName.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.EnterName); });
			enterName.OnEnter(OnEnterNameEnter);
			enterName.Event(EnterNameState.NameSetEvent).Target(completionCheck);
			enterName.Event(NetworkState.PhotonCriticalDisconnectedEvent).Target(disconnected);
			enterName.OnExit(OnEnterNameExit);
			
			completionCheck.OnEnter(_sequence.SendCurrentStepCompletedAnalytics);
			completionCheck.Transition().Target(playGame);
				
			playGame.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.PlayGameClick); });
			playGame.OnEnter(() => _ = OnPlayGameEnter());
			playGame.Event(MainMenuState.PlayClickedEvent).Target(createTutorialRoom);
			playGame.Event(NetworkState.PhotonCriticalDisconnectedEvent).Target(disconnected);
			playGame.OnExit(OnPlayGameExit);
			
			createTutorialRoom.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.CreateTutorialMatchRoom); });
			createTutorialRoom.OnEnter(StartSecondTutorialMatch);
			createTutorialRoom.Event(NetworkState.JoinedRoomEvent).Target(mapSelect);
			createTutorialRoom.Event(NetworkState.PhotonCriticalDisconnectedEvent).Target(disconnected);

			mapSelect.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.SelectMapPoint); });
			mapSelect.OnEnter(() => _ = OnMapSelectEnter());
			mapSelect.Event(_selectedMapPointEvent).Target(waitSimulationStart);
			mapSelect.Event(GameSimulationState.SimulationStartedEvent).OnTransition(()=>_sequence.EnterStep(TutorialClientStep.TutorialFinish)).Target(final);
			mapSelect.OnExit(OnMapSelectExit);

			disconnected.OnEnter(CloseTutorialUi);
			disconnected.Event(NetworkState.PhotonMasterConnectedEvent).Target(enterName);
			disconnected.OnExit(_sequence.Reset);
			
			waitSimulationStart.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.WaitTutorialMatchStart); });
			waitSimulationStart.Event(GameSimulationState.SimulationStartedEvent).Target(final);
			waitSimulationStart.OnExit(() => { _sequence.EnterStep(TutorialClientStep.TutorialFinish); });
			
			final.OnEnter(_sequence.SendCurrentStepCompletedAnalytics);
			final.OnEnter(UnsubscribeMessages);
		}
		
		private void StartSecondTutorialMatch()
		{
			_tutorialService.CreateJoinSecondTutorialRoom();
		}

		private void GetTutorialScreenRefs()
		{
			_dialogUi = _services.GameUiService.GetUi<CharacterDialogScreenPresenter>();
			_tutorialUtilsUi = _services.GameUiService.GetUi<TutorialUtilsScreenPresenter>();
		}
		
		private void CloseTutorialUi()
		{
			_tutorialUtilsUi.Unblock();
			_tutorialUtilsUi.RemoveHighlight();
		}

		private void SubscribeMessages()
		{
			_services.MessageBrokerService.Subscribe<MapDropPointSelectedMessage>(OnMapDropPointSelectedMessage);
		}

		private void OnMapDropPointSelectedMessage(MapDropPointSelectedMessage obj)
		{
			_statechartTrigger(_selectedMapPointEvent);
		}
		
		private void UnsubscribeMessages()
		{
		}
		
		private void OnEnterNameEnter()
		{
			_dialogUi.ShowDialog(ScriptLocalization.UITTutorial.enter_your_name, CharacterType.Female, CharacterDialogMoodType.Neutral, CharacterDialogPosition.TopLeft);
		}
		
		private void OnEnterNameExit()
		{
			_tutorialUtilsUi.BlockFullScreen();
		}
		
		private async UniTaskVoid OnPlayGameEnter()
		{
			await Task.Delay(GameConstants.Tutorial.TIME_1000MS);

			_dialogUi.ShowDialog(ScriptLocalization.UITTutorial.lets_play_real_match, CharacterType.Female, CharacterDialogMoodType.Happy, CharacterDialogPosition.TopLeft);

			_tutorialUtilsUi.Unblock();
			await _tutorialUtilsUi.BlockAround<HomeScreenPresenter>("play-button");
			_tutorialUtilsUi.Highlight<HomeScreenPresenter>("play-button");
		}
		
		private void OnPlayGameExit()
		{
			CloseTutorialUi();
			_dialogUi.HideDialog(CharacterType.Female);
		}
		
		private async UniTaskVoid OnMapSelectEnter()
		{
			_tutorialUtilsUi.BlockFullScreen();
			await Task.Delay(GameConstants.Tutorial.TIME_4000MS);
			_dialogUi.ShowDialog(ScriptLocalization.UITTutorial.select_map_position, CharacterType.Female, CharacterDialogMoodType.Happy, CharacterDialogPosition.TopLeft);
			_tutorialUtilsUi.Unblock();
			await _tutorialUtilsUi.BlockAround<PreGameLoadingScreenPresenter>("tutorial-drop-pos");
			_tutorialUtilsUi.Highlight<PreGameLoadingScreenPresenter>("tutorial-drop-pos");
		}
		
		private void OnMapSelectExit()
		{
			CloseTutorialUi();
			_dialogUi.HideDialog(CharacterType.Female);
		}
	}
}
