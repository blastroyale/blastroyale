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
	public class LootMenuState
	{
		private readonly IStatechartEvent _slotClickedEvent = new StatechartEvent("Slot Clicked Event");
		private readonly IStatechartEvent _skinClickedEvent = new StatechartEvent("Skin Clicked Event");
		private readonly IStatechartEvent _allGearClickedEvent = new StatechartEvent("All Gear Clicked Event");
		private readonly IStatechartEvent _backButtonClickedEvent = new StatechartEvent("Loot Screen Back Button Clicked Event");
		private readonly IStatechartEvent _equipmentScreenCloseClickedEvent = new StatechartEvent("Equipment Screen Close Clicked Event");
		private readonly IStatechartEvent _playerSkinClickedEvent = new StatechartEvent("Player Skin Button Clicked Event");
		private readonly IStatechartEvent _playerSkinClosedEvent = new StatechartEvent("Player Skin Closed Event");

		private readonly IGameUiService _uiService;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		
		private GameIdGroup _equipmentSlotTypePicked;

		public LootMenuState(IGameServices services, IGameUiService uiService, IGameDataProvider gameDataProvider, 
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
			var lootMenuState = stateFactory.State("Loot Menu State");
			var equipmentScreenState = stateFactory.State("Equipment Screen State");
			var playerSkinPopupState = stateFactory.State("Player Skin Popup State");
			var final = stateFactory.Final("Final");

			initial.Transition().Target(lootMenuState);
			initial.OnExit(SubscribeEvents);

			lootMenuState.OnEnter(OpenLootMenuUI);
			lootMenuState.Event(_slotClickedEvent).Target(equipmentScreenState);
			lootMenuState.Event(_skinClickedEvent).Target(playerSkinPopupState);
			lootMenuState.Event(_allGearClickedEvent).OnTransition(SetAllGearSlot).Target(equipmentScreenState);
			lootMenuState.Event(_backButtonClickedEvent).Target(final);
			lootMenuState.OnExit(CloseLootMenuUI);

			equipmentScreenState.OnEnter(OpenEquipmentMenuUI);
			equipmentScreenState.Event(_equipmentScreenCloseClickedEvent).Target(lootMenuState);
			equipmentScreenState.OnExit(CloseEquipmentMenuUI);

			playerSkinPopupState.OnEnter(OpenPlayerSkinMenuUI);
			playerSkinPopupState.Event(_playerSkinClickedEvent).Target(lootMenuState);
			playerSkinPopupState.Event(_playerSkinClosedEvent).Target(lootMenuState);
			playerSkinPopupState.OnExit(ClosePlayerSkinMenuUI);

			final.OnEnter(SendLoadoutUpdateCommand);
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

		private void OpenLootMenuUI()
		{
			var data = new LootScreenPresenter.StateData
			{
				OnEquipmentButtonClicked = EquipmentButtonClicked,
				OnAllGearClicked = () => _statechartTrigger(_allGearClickedEvent),
				OnSlotButtonClicked = SlotButtonClicked,
				OnChangeSkinClicked = () => _statechartTrigger(_skinClickedEvent),
				OnLootBackButtonClicked = () => _statechartTrigger(_backButtonClickedEvent)
			};

			_uiService.OpenUi<LootScreenPresenter, LootScreenPresenter.StateData>(data);
		}

		private void CloseLootMenuUI()
		{
			_uiService.CloseUi<LootScreenPresenter>();
		}
		
		private void SendLoadoutUpdateCommand()
		{
			var loadOut = _gameDataProvider.EquipmentDataProvider.Loadout.ReadOnlyDictionary;
			
			_services.CommandService.ExecuteCommand(new UpdateLoadoutCommand { SlotsToUpdate = loadOut });
		}

		private void SetAllGearSlot()
		{
			_equipmentSlotTypePicked = GameIdGroup.Equipment;
		}
		
		private void SlotButtonClicked(GameIdGroup slot)
		{
			_equipmentSlotTypePicked = slot;
			_statechartTrigger(_slotClickedEvent);
		}

		private void EquipmentButtonClicked(UniqueId uniqueId)
		{
			_equipmentSlotTypePicked = _gameDataProvider.UniqueIdDataProvider.Ids[uniqueId].GetSlot();
		}

		private void OpenEquipmentMenuUI()
		{
			var data = new EquipmentScreenPresenter.StateData
			{
				EquipmentSlot = _equipmentSlotTypePicked,
				OnCloseClicked = () => _statechartTrigger(_equipmentScreenCloseClickedEvent)
			};

			_uiService.OpenUi<EquipmentScreenPresenter, EquipmentScreenPresenter.StateData>(data);
		}
		
		private void CloseEquipmentMenuUI()
		{
			_uiService.CloseUi<EquipmentScreenPresenter>();
		}
		
		private void OpenPlayerSkinMenuUI()
		{
			var data = new PlayerSkinScreenPresenter.StateData
			{
				OnCloseClicked = () => _statechartTrigger(_playerSkinClosedEvent),
			};
			
			_uiService.OpenUi<PlayerSkinScreenPresenter, PlayerSkinScreenPresenter.StateData>(data);
		}
		
		private void ClosePlayerSkinMenuUI()
		{
			_uiService.CloseUi<PlayerSkinScreenPresenter>();
		}
	}
}