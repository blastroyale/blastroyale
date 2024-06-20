using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Tutorial;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using I2.Loc;

namespace FirstLight.Game.StateMachines
{
	public class MetaAndMatchTutorialState
	{
		private static readonly IStatechartEvent _selectedMapPointEvent = new StatechartEvent("TUTORIAL - Selected map point event");

		private readonly IGameServices _services;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		private IMatchServices _matchServices;
		private TutorialOverlayPresenter _tutorialOverlay;
		private readonly MetaTutorialSequence _sequence;

		public MetaAndMatchTutorialState(IGameServices services, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
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
			var completionCheck = stateFactory.Choice("Completion check");
			var playGame = stateFactory.State("Play game");
			var mapSelect = stateFactory.State("Map Select");
			var disconnected = stateFactory.State("Disconnected");
			var createTutorialRoom = stateFactory.State("Join Room");
			var waitSimulationStart = stateFactory.State("WaitSimulationStart");

			initial.Transition().Target(completionCheck);
			initial.OnExit(SubscribeMessages);
			initial.OnExit(_services.GameModeService.SelectDefaultRankedMode);
			initial.OnExit(GetTutorialScreenRefs);

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
			mapSelect.Event(GameSimulationState.SimulationStartedEvent).OnTransition(() => _sequence.EnterStep(TutorialClientStep.TutorialFinish)).Target(final);
			mapSelect.OnExit(OnMapSelectExit);

			disconnected.OnEnter(CloseTutorialUi);
			disconnected.Event(NetworkState.PhotonMasterConnectedEvent).Target(completionCheck);
			disconnected.OnExit(_sequence.Reset);

			waitSimulationStart.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.WaitTutorialMatchStart); });
			waitSimulationStart.Event(GameSimulationState.SimulationStartedEvent).Target(final);
			waitSimulationStart.OnExit(() => { _sequence.EnterStep(TutorialClientStep.TutorialFinish); });

			final.OnEnter(_sequence.SendCurrentStepCompletedAnalytics);
			final.OnEnter(UnsubscribeMessages);
		}

		private void StartSecondTutorialMatch()
		{
			_services.TutorialService.CreateJoinSecondTutorialRoom();
		}

		private void GetTutorialScreenRefs()
		{
			_tutorialOverlay = _services.UIService.GetScreen<TutorialOverlayPresenter>();
		}

		private void CloseTutorialUi()
		{
			_tutorialOverlay.Unblock();
			_tutorialOverlay.RemoveHighlight();
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

		private async UniTaskVoid OnPlayGameEnter()
		{
			await Task.Delay(GameConstants.Tutorial.TIME_1000MS);

			_tutorialOverlay.Dialog.ShowDialog(ScriptLocalization.UITTutorial.lets_play_real_match, CharacterType.Female, CharacterDialogMoodType.Happy, CharacterDialogPosition.TopLeft);

			await _tutorialOverlay.BlockAround<HomeScreenPresenter>("play-button");
			_tutorialOverlay.Highlight<HomeScreenPresenter>("play-button");
		}

		private void OnPlayGameExit()
		{
			CloseTutorialUi();
			_tutorialOverlay.Dialog.HideDialog(CharacterType.Female);
		}

		private async UniTaskVoid OnMapSelectEnter()
		{
			_services.RoomService.CurrentRoom.ResumeTimer(GameConstants.Tutorial.SECONDS_TO_START_SECOND_MATCH);

			_tutorialOverlay.BlockFullScreen();

			await _tutorialOverlay.EnsurePresenterElement<PreGameLoadingScreenPresenter>("tutorial-drop-pos");
			await UniTask.Delay(GameConstants.Tutorial.TIME_1000MS);

			_tutorialOverlay.Dialog.ShowDialog(ScriptLocalization.UITTutorial.select_map_position, CharacterType.Female, CharacterDialogMoodType.Happy,
				CharacterDialogPosition.TopLeft);
			_tutorialOverlay.Unblock();
			await _tutorialOverlay.BlockAround<PreGameLoadingScreenPresenter>("tutorial-drop-pos");
			_tutorialOverlay.Highlight<PreGameLoadingScreenPresenter>("tutorial-drop-pos");
		}

		private void OnMapSelectExit()
		{
			CloseTutorialUi();
			_tutorialOverlay.Dialog.HideDialog(CharacterType.Female);
		}
	}
}