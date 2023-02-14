using System;
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
		private readonly IStatechartEvent _slotClickedEvent = new StatechartEvent("Slot Clicked Event");
		private readonly IStatechartEvent _itemProcessedEvent = new StatechartEvent("Item Processed Event");

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
			var collectionMenuSelectionState = stateFactory.State("Collection Menu Selection Screen State");
			var scrapState = stateFactory.State("Collection Menu Scrap Popup State");
			var upgradeState = stateFactory.State("Collection Menu Upgrade Popup State");
			var repairState = stateFactory.State("Collection Menu Repair Popup State");
			var final = stateFactory.Final("Final");

			initial.Transition().Target(collectionMenuState);

			collectionMenuState.OnEnter(OpenEquipmentScreen);
			collectionMenuState.Event(_slotClickedEvent).Target(collectionMenuSelectionState);
			collectionMenuState.Event(_backButtonClickedEvent).Target(final);
			collectionMenuState.Event(_closeButtonClickedEvent).Target(final);

			collectionMenuSelectionState.OnEnter(OpenEquipmentSelectionScreen);
			collectionMenuSelectionState.Event(_backButtonClickedEvent).Target(collectionMenuState);
			collectionMenuSelectionState.Event(_closeButtonClickedEvent).Target(final);

			final.OnEnter(SendLoadoutUpdateCommand);
		}

		private void OpenScrapPopup()
		{
			OpenPopup(EquipmentPopupPresenter.Mode.Scrap);
		}

		private void OpenUpgradePopup()
		{
			OpenPopup(EquipmentPopupPresenter.Mode.Upgrade);
		}

		private void OpenRepairPopup()
		{
			OpenPopup(EquipmentPopupPresenter.Mode.Repair);
		}


		private void CloseEquipmentPopup()
		{
			_uiService.CloseUi<PlayerSkinScreenPresenter>();
		}

		private async void OpenPopup(EquipmentPopupPresenter.Mode mode)
		{
			var data = new EquipmentPopupPresenter.StateData
			{
				EquipmentIds = new[] {_uiService.GetUi<EquipmentSelectionPresenter>().SelectedItem},
				PopupMode = mode,
				// OnActionConfirmed = OnPopupActionConfirmed,
				OnCloseClicked = () => _statechartTrigger(_closeButtonClickedEvent)
			};

			await _uiService.OpenUiAsync<EquipmentPopupPresenter, EquipmentPopupPresenter.StateData>(data);
		}

		private void OpenEquipmentScreen()
		{
			var data = new PlayerSkinScreenPresenter.StateData
			{
				OnSkinSelected = ItemClicked,
				OnCloseClicked = () => _statechartTrigger(_closeButtonClickedEvent),
			};

			_uiService.OpenScreen<PlayerSkinScreenPresenter, PlayerSkinScreenPresenter.StateData>(data);
			_services.MessageBrokerService.Publish(new SkinsScreenOpenedMessage());
		}

		private void SendLoadoutUpdateCommand()
		{
			var loadOut = _gameDataProvider.EquipmentDataProvider.Loadout.ReadOnlyDictionary;

			_services.CommandService.ExecuteCommand(new UpdateLoadoutCommand {SlotsToUpdate = loadOut});
		}

		private void ItemClicked()
		{
			
		}
		
		private void SlotButtonClicked(GameIdGroup slot)
		{
			_equipmentSlotTypePicked = slot;
			_statechartTrigger(_slotClickedEvent);
			
			_services.MessageBrokerService.Publish(new EquipmentSlotOpenedMessage() {Slot = slot} );
		}

		private void OpenEquipmentSelectionScreen()
		{
			var data = new EquipmentSelectionPresenter.StateData
			{
				EquipmentSlot = _equipmentSlotTypePicked,
				OnBackClicked = () => _statechartTrigger(_backButtonClickedEvent),
				OnCloseClicked = () => _statechartTrigger(_closeButtonClickedEvent),
			};

			_uiService.OpenScreen<EquipmentSelectionPresenter, EquipmentSelectionPresenter.StateData>(data);
		}
	}
}