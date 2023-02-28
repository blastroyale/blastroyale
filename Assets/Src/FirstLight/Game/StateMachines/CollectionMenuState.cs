﻿using System;
using FirstLight.Game.Commands;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Statechart;
using Quantum;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Collection Menu State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class CollectionMenuState
	{
		private readonly IStatechartEvent _backButtonClickedEvent =
			new StatechartEvent("Equipment Back Button Clicked Event");

		private readonly IStatechartEvent _closeButtonClickedEvent =
			new StatechartEvent("Equipment Close Button Clicked Event");

		private readonly IGameUiService _uiService;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		private GameIdGroup _equipmentSlotTypePicked;

		public CollectionMenuState(IGameServices services, IGameUiService uiService, IGameDataProvider gameDataProvider,
								   Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_uiService = uiService;
			_gameDataProvider = gameDataProvider;
			_statechartTrigger = statechartTrigger;
		}

		/// <summary>
		/// Setups the Main Menu state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var collectionMenuState = stateFactory.State("Collection Menu Screen State");
			var final = stateFactory.Final("Final");

			initial.Transition().Target(collectionMenuState);

			collectionMenuState.OnEnter(OpenCollectionScreen);
			collectionMenuState.Event(_backButtonClickedEvent).Target(final);
			collectionMenuState.Event(_closeButtonClickedEvent).Target(final);
			
			final.OnEnter(SendLoadoutUpdateCommand);
		}
		
		private void CloseCollectionScreen()
		{
			_uiService.CloseUi<CollectionScreenPresenter>();
		}

		private void OpenCollectionScreen()
		{
			var data = new CollectionScreenPresenter.StateData
			{
				OnHomeClicked = () => _statechartTrigger(_closeButtonClickedEvent),
				OnBackClicked = () => _statechartTrigger(_backButtonClickedEvent),
			};

			_uiService.OpenScreen<CollectionScreenPresenter, CollectionScreenPresenter.StateData>(data);
		}

		private void SendLoadoutUpdateCommand()
		{
			var loadOut = _gameDataProvider.EquipmentDataProvider.Loadout.ReadOnlyDictionary;

			_services.CommandService.ExecuteCommand(new UpdateLoadoutCommand {SlotsToUpdate = loadOut});
		}
	}
}