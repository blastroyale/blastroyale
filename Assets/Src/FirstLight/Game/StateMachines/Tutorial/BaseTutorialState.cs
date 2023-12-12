using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Statechart;

namespace FirstLight.Game.StateMachines
{
	public class TutorialState
	{
		private static readonly IStatechartEvent _startFirstGameTutorialEvent = new StatechartEvent("TUTORIAL - Start First Game Tutorial");
		private static readonly IStatechartEvent _startEquipmentBpTutorialEvent = new StatechartEvent("TUTORIAL - Start Equipment BP Tutorial");

		private readonly IGameServices _services;
		private readonly IInternalTutorialService _tutorialService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly FirstGameTutorialState _firstGameTutorialState;
		private readonly MetaAndMatchTutorialState _metaAndMatchTutorialState;

		public TutorialState(IGameDataProvider logic, IGameServices services, IInternalTutorialService tutorialService,
							 Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_tutorialService = tutorialService;
			_statechartTrigger = statechartTrigger;
			_firstGameTutorialState = new FirstGameTutorialState(logic, services, tutorialService, statechartTrigger);
			_metaAndMatchTutorialState = new MetaAndMatchTutorialState(logic, services, tutorialService, statechartTrigger);
		}

		/// <summary>
		/// Setups the audio state - root state, and then per gamemode type nested states
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var loadTutorialUi = stateFactory.TaskWait("Load tutorial UI");
			var idle = stateFactory.State("TUTORIAL - Idle");
			var firstGameTutorial = stateFactory.Nest("TUTORIAL - First Game Tutorial");
			var metaAndMatchTutorial = stateFactory.Nest("TUTORIAL - Equipment BP Tutorial");

			initial.Transition().Target(loadTutorialUi);
			initial.OnExit(SubscribeMessages);

			loadTutorialUi.WaitingFor(OpenTutorialScreens).Target(idle);

			idle.OnEnter(() => SetCurrentSection(TutorialSection.NONE));
			idle.Event(_startFirstGameTutorialEvent).Target(firstGameTutorial);
			idle.Event(_startEquipmentBpTutorialEvent).Target(metaAndMatchTutorial);

			firstGameTutorial.OnEnter(() => SetCurrentSection(TutorialSection.FIRST_GUIDE_MATCH));
			firstGameTutorial.Nest(_firstGameTutorialState.Setup).Target(idle);
			firstGameTutorial.OnExit(() => SendSectionCompleted(TutorialSection.FIRST_GUIDE_MATCH));

			metaAndMatchTutorial.OnEnter(() => SetCurrentSection(TutorialSection.META_GUIDE_AND_MATCH));
			metaAndMatchTutorial.Nest(_metaAndMatchTutorialState.Setup).Target(idle);
			metaAndMatchTutorial.OnExit(() => SendSectionCompleted(TutorialSection.META_GUIDE_AND_MATCH));
		}

		private async UniTask OpenTutorialScreens()
		{
			await _services.GameUiService.OpenUiAsync<TutorialUtilsScreenPresenter>();
			await _services.GameUiService.OpenUiAsync<CharacterDialogScreenPresenter>();
			await _services.GameUiService.OpenUiAsync<GuideHandPresenter>();
		}

		private void SetCurrentSection(TutorialSection section)
		{
			_tutorialService.CurrentRunningTutorial.Value = section;
		}

		private void SendSectionCompleted(TutorialSection section)
		{
			_tutorialService.CompleteTutorialSection(section);
		}

		private void SubscribeMessages()
		{
			_services.MessageBrokerService.Subscribe<RequestStartFirstGameTutorialMessage>(OnRequestStartFirstTutorialMessage);
			_services.MessageBrokerService.Subscribe<RequestStartMetaMatchTutorialMessage>(OnRequestStartMetaMatchTutorialMessage);
		}

		private void OnRequestStartFirstTutorialMessage(RequestStartFirstGameTutorialMessage msg)
		{
			if (_tutorialService.HasCompletedTutorialSection(TutorialSection.FIRST_GUIDE_MATCH)) return;

			_statechartTrigger(_startFirstGameTutorialEvent);
		}

		private void OnRequestStartMetaMatchTutorialMessage(RequestStartMetaMatchTutorialMessage msg)
		{
			if (_tutorialService.HasCompletedTutorialSection(TutorialSection.META_GUIDE_AND_MATCH)) return;

			_statechartTrigger(_startEquipmentBpTutorialEvent);
		}
	}
}