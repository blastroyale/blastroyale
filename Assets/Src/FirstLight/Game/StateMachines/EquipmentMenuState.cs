using System;
using System.Collections.Generic;
using FirstLight.Game.Commands;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Statechart;
using FirstLight.Game.Utils;
using Quantum;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Loot Menu State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class EquipmentMenuState
	{
		private readonly IStatechartEvent _slotClickedEvent = new StatechartEvent("Slot Clicked Event");

		private readonly IStatechartEvent _backButtonClickedEvent =
			new StatechartEvent("Equipment Back Button Clicked Event");

		private readonly IStatechartEvent _closeButtonClickedEvent =
			new StatechartEvent("Equipment Close Button Clicked Event");

		private readonly IGameUiService _uiService;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		private GameIdGroup _equipmentSlotTypePicked;

		public EquipmentMenuState(IGameServices services, IGameUiService uiService, IGameDataProvider gameDataProvider,
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
			var equipmentState = stateFactory.State("Equipment Screen State");
			var equipmentSelectionState = stateFactory.State("Equipment Selection Screen State");
			var final = stateFactory.Final("Final");

			initial.Transition().Target(equipmentState);

			equipmentState.OnEnter(OpenEquipmentScreen);
			equipmentState.Event(_slotClickedEvent).Target(equipmentSelectionState);
			equipmentState.Event(_backButtonClickedEvent).Target(final);
			equipmentState.Event(_closeButtonClickedEvent).Target(final);

			equipmentSelectionState.OnEnter(OpenEquipmentSelectionScreen);
			equipmentSelectionState.Event(_backButtonClickedEvent).Target(equipmentState);
			equipmentSelectionState.Event(_closeButtonClickedEvent).Target(final);

			final.OnEnter(SendLoadoutUpdateCommand);
		}

		private void OpenEquipmentScreen()
		{
			var data = new EquipmentPresenter.StateData
			{
				OnSlotButtonClicked = SlotButtonClicked,
				OnBackClicked = () => _statechartTrigger(_backButtonClickedEvent),
				OnCloseClicked = () => _statechartTrigger(_closeButtonClickedEvent)
			};

			_uiService.OpenScreen<EquipmentPresenter, EquipmentPresenter.StateData>(data);
		}

		private void SendLoadoutUpdateCommand()
		{
			var loadOut = _gameDataProvider.EquipmentDataProvider.Loadout.ReadOnlyDictionary;

			_services.CommandService.ExecuteCommand(new UpdateLoadoutCommand {SlotsToUpdate = loadOut});
		}

		private void SlotButtonClicked(GameIdGroup slot)
		{
			_equipmentSlotTypePicked = slot;
			_statechartTrigger(_slotClickedEvent);
		}

		private void OpenEquipmentSelectionScreen()
		{
			var data = new EquipmentSelectionPresenter.StateData
			{
				EquipmentSlot = _equipmentSlotTypePicked,
				OnBackClicked = () => _statechartTrigger(_backButtonClickedEvent),
				OnCloseClicked = () => _statechartTrigger(_closeButtonClickedEvent)
			};

			_uiService.OpenScreen<EquipmentSelectionPresenter, EquipmentSelectionPresenter.StateData>(data);
		}
	}
}