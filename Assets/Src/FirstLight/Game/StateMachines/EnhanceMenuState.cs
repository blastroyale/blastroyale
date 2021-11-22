using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Statechart;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Craft Menu State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class EnhanceMenuState
	{
		private readonly IStatechartEvent _enhanceStartedEvent = new StatechartEvent("Enhance Started Event");
		private readonly IStatechartEvent _backButtonClickedEvent = new StatechartEvent("Enhance Screen Back Button Clicked Event");


		private readonly IGameUiService _uiService;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		
		private readonly CollectEnhanceRewardState _collectEnhanceRewardState;

		public EnhanceMenuState(IGameServices services, IGameUiService uiService, IGameDataProvider gameDataProvider, 
		                     Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_uiService = uiService;
			_gameDataProvider = gameDataProvider;
			_statechartTrigger = statechartTrigger;
			
			_collectEnhanceRewardState = new CollectEnhanceRewardState(services, statechartTrigger, _gameDataProvider);
		}

		/// <summary>
		/// Setups the Main Menu state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var enhanceMenuState = stateFactory.State("Enhance Menu State");
			var final = stateFactory.Final("Final");
			var collectEnhance = stateFactory.Nest("Collect Fuse State");

			initial.Transition().Target(enhanceMenuState);
			initial.OnExit(SubscribeEvents);

			enhanceMenuState.OnEnter(OpenEnhanceMenuUI);
			enhanceMenuState.Event(_backButtonClickedEvent).Target(final);
			enhanceMenuState.Event(_enhanceStartedEvent).Target(collectEnhance);
			enhanceMenuState.OnExit(CloseEnhanceMenuUI);

			collectEnhance.Nest(_collectEnhanceRewardState.Setup).Target(enhanceMenuState);
			
			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			// Do Nothing
		}

		private void UnsubscribeEvents()
		{
			// Do Nothing
		}

		private void OpenEnhanceMenuUI()
		{
			var data = new EnhanceScreenPresenter.StateData
			{
				OnCloseClicked = () => _statechartTrigger(_backButtonClickedEvent),
				OnEnhancedClicked = EnhanceClicked
			};

			_uiService.OpenUi<EnhanceScreenPresenter, EnhanceScreenPresenter.StateData>(data);
		}

		private void CloseEnhanceMenuUI()
		{
			_uiService.CloseUi<EnhanceScreenPresenter>();
		}
		
		private void EnhanceClicked(List<UniqueId> enhanceList) 
		{
			_collectEnhanceRewardState.SetEnhanceList(enhanceList);
			_statechartTrigger(_enhanceStartedEvent);
		}
	}
}