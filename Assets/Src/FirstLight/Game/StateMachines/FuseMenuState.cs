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
	public class FuseMenuState
	{
		private readonly IStatechartEvent _fuseStartedEvent = new StatechartEvent("Fuse Clicked Event");
		private readonly IStatechartEvent _backButtonClickedEvent = new StatechartEvent("Fuse Screen Back Button Clicked Event");


		private readonly IGameUiService _uiService;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly CollectFuseRewardState _collectFuseRewardState;

		public FuseMenuState(IGameServices services, IGameUiService uiService, IGameDataProvider gameDataProvider, 
		                     Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_uiService = uiService;
			_gameDataProvider = gameDataProvider;
			_statechartTrigger = statechartTrigger;
			_collectFuseRewardState = new CollectFuseRewardState(services, statechartTrigger, _gameDataProvider);
		}

		/// <summary>
		/// Setups the Main Menu state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var fuseMenuState = stateFactory.State("Fuse Menu State");
			var collectFuse = stateFactory.Nest("Collect Fuse State");
			var final = stateFactory.Final("Final");

			initial.Transition().Target(fuseMenuState);
			initial.OnExit(SubscribeEvents);
			
			fuseMenuState.OnEnter(OpenFuseMenuUI);
			fuseMenuState.Event(_backButtonClickedEvent).Target(final);
			fuseMenuState.Event(_fuseStartedEvent).Target(collectFuse);
			fuseMenuState.OnExit(CloseFuseMenuUI);
			
			collectFuse.Nest(_collectFuseRewardState.Setup).Target(fuseMenuState);

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

		private void OpenFuseMenuUI()
		{
			var data = new FuseScreenPresenter.StateData
			{
				OnCloseClicked = () => _statechartTrigger(_backButtonClickedEvent),
				OnFusionClicked = FusionClicked
			};

			_uiService.OpenUi<FuseScreenPresenter, FuseScreenPresenter.StateData>(data);
		}

		private void CloseFuseMenuUI()
		{
			_uiService.CloseUi<FuseScreenPresenter>();
		}
		
		private void FusionClicked(List<UniqueId> fusionList) 
		{
			_collectFuseRewardState.SetFusionList(fusionList);
			_statechartTrigger(_fuseStartedEvent);
		}
	}
}