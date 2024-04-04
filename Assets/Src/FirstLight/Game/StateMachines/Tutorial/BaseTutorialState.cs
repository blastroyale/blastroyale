using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
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
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly FirstGameTutorialState _firstGameTutorialState;
		private readonly MetaAndMatchTutorialState _metaAndMatchTutorialState;

		public TutorialState(IGameServices services, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_statechartTrigger = statechartTrigger;
			_firstGameTutorialState = new FirstGameTutorialState(services, statechartTrigger);
			_metaAndMatchTutorialState = new MetaAndMatchTutorialState(services, statechartTrigger);
		}

		/// <summary>
		/// Setups the audio state - root state, and then per gamemode type nested states
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var tutorialCompletedCheck = stateFactory.Choice("Tutorial Completed Check");
			var loadTutorialUi = stateFactory.TaskWait("Load tutorial UI");
			var idle = stateFactory.State("TUTORIAL - Idle");
			var firstGameTutorial = stateFactory.Nest("TUTORIAL - First Game Tutorial");
			var metaAndMatchTutorial = stateFactory.Nest("TUTORIAL - Equipment BP Tutorial");
			var final = stateFactory.Final("Final");

			initial.Transition().Target(tutorialCompletedCheck);
			initial.OnExit(SubscribeMessages);

			tutorialCompletedCheck.Transition().Condition(() => _services.TutorialService.HasCompletedTutorial()).Target(final);
			tutorialCompletedCheck.Transition().Target(loadTutorialUi);

			loadTutorialUi.WaitingFor(OpenTutorialScreens).Target(idle);

			idle.OnEnter(() => SetCurrentSection(TutorialSection.NONE));
			idle.Event(_startFirstGameTutorialEvent).Target(firstGameTutorial);
			idle.Event(_startEquipmentBpTutorialEvent).Target(metaAndMatchTutorial);

			firstGameTutorial.OnEnter(() => SetCurrentSection(TutorialSection.FIRST_GUIDE_MATCH));
			firstGameTutorial.Nest(_firstGameTutorialState.Setup).Target(idle);
			firstGameTutorial.OnExit(() => SendSectionCompleted(TutorialSection.FIRST_GUIDE_MATCH));

			metaAndMatchTutorial.OnEnter(() => SetCurrentSection(TutorialSection.META_GUIDE_AND_MATCH));
			metaAndMatchTutorial.Nest(_metaAndMatchTutorialState.Setup).Target(final);
			metaAndMatchTutorial.OnExit(() => SendSectionCompleted(TutorialSection.META_GUIDE_AND_MATCH));

			final.OnEnter(CloseTutorialScreens);
		}

		private async UniTask OpenTutorialScreens()
		{
			await _services.UIService.OpenScreen<TutorialOverlayPresenter>();
		}

		private void CloseTutorialScreens()
		{
			_services.UIService.CloseScreen<TutorialOverlayPresenter>(false).Forget();
		}

		private void SetCurrentSection(TutorialSection section)
		{
			_services.TutorialService.CurrentRunningTutorial.Value = section;
		}

		private void SendSectionCompleted(TutorialSection section)
		{
			_services.TutorialService.CompleteTutorialSection(section);
		}

		private void SubscribeMessages()
		{
			_services.MessageBrokerService.Subscribe<RequestStartFirstGameTutorialMessage>(OnRequestStartFirstTutorialMessage);
			_services.MessageBrokerService.Subscribe<RequestStartMetaMatchTutorialMessage>(OnRequestStartMetaMatchTutorialMessage);
		}

		private void OnRequestStartFirstTutorialMessage(RequestStartFirstGameTutorialMessage msg)
		{
			if (_services.TutorialService.HasCompletedTutorialSection(TutorialSection.FIRST_GUIDE_MATCH)) return;

			_statechartTrigger(_startFirstGameTutorialEvent);
		}

		private void OnRequestStartMetaMatchTutorialMessage(RequestStartMetaMatchTutorialMessage msg)
		{
			if (_services.TutorialService.HasCompletedTutorialSection(TutorialSection.META_GUIDE_AND_MATCH)) return;

			_statechartTrigger(_startEquipmentBpTutorialEvent);
		}
	}
}