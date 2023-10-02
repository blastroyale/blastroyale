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
	/// This object contains the behaviour logic for the Loot Menu State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class EquipmentMenuState
	{
		public static readonly IStatechartEvent CloseButtonClickedEvent = new StatechartEvent("Equipment Close Button Clicked Event");
		
		private readonly IStatechartEvent _slotClickedEvent = new StatechartEvent("Slot Clicked Event");
		private readonly IStatechartEvent _scrapClickedEvent = new StatechartEvent("Scrap Clicked Event");
		private readonly IStatechartEvent _upgradeClickedEvent = new StatechartEvent("Upgrade Clicked Event");
		private readonly IStatechartEvent _fuseClickedEvent = new StatechartEvent("Fuse Clicked Event");
		private readonly IStatechartEvent _repairClickedEvent = new StatechartEvent("Repair Clicked Event");
		private readonly IStatechartEvent _itemProcessedEvent = new StatechartEvent("Item Processed Event");
		private readonly IStatechartEvent _backButtonClickedEvent = new StatechartEvent("Equipment Back Button Clicked Event");
		

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
			var scrapState = stateFactory.State("Equipment Scrap Popup State");
			var fuseState = stateFactory.State("Equipment Fuse Popup State");
			var upgradeState = stateFactory.State("Equipment Upgrade Popup State");
			var repairState = stateFactory.State("Equipment Repair Popup State");
			var final = stateFactory.Final("Final");

			initial.Transition().Target(equipmentState);

			equipmentState.OnEnter(OpenEquipmentScreen);
			equipmentState.Event(_slotClickedEvent).Target(equipmentSelectionState);
			equipmentState.Event(_backButtonClickedEvent).Target(final);
			equipmentState.Event(CloseButtonClickedEvent).Target(final);
			equipmentState.Event(_scrapClickedEvent).Target(scrapState);
			equipmentState.Event(_upgradeClickedEvent).Target(upgradeState);
			equipmentState.Event(_fuseClickedEvent).Target(fuseState);
			equipmentState.Event(_repairClickedEvent).Target(repairState);

			equipmentSelectionState.OnEnter(OpenEquipmentSelectionScreen);
			equipmentSelectionState.Event(_scrapClickedEvent).Target(scrapState);
			equipmentSelectionState.Event(_upgradeClickedEvent).Target(upgradeState);
			equipmentSelectionState.Event(_fuseClickedEvent).Target(fuseState);

			equipmentSelectionState.Event(_repairClickedEvent).Target(repairState);
			equipmentSelectionState.Event(_backButtonClickedEvent).Target(equipmentState);
			equipmentSelectionState.Event(CloseButtonClickedEvent).Target(final);

			scrapState.OnEnter(OpenScrapPopup);
			scrapState.Event(CloseButtonClickedEvent).Target(equipmentSelectionState);
			scrapState.Event(_itemProcessedEvent).Target(equipmentSelectionState);
			scrapState.OnExit(CloseEquipmentPopup);

			fuseState.OnEnter(OpenFusePopup);
			fuseState.Event(CloseButtonClickedEvent).Target(equipmentSelectionState);
			fuseState.Event(_itemProcessedEvent).Target(equipmentSelectionState);
			fuseState.OnExit(CloseEquipmentPopup);

			upgradeState.OnEnter(OpenUpgradePopup);
			upgradeState.Event(CloseButtonClickedEvent).Target(equipmentSelectionState);
			upgradeState.Event(_itemProcessedEvent).Target(equipmentSelectionState);
			upgradeState.OnExit(CloseEquipmentPopup);

			repairState.OnEnter(OpenRepairPopup);
			repairState.Event(CloseButtonClickedEvent).Target(equipmentSelectionState);
			repairState.Event(_itemProcessedEvent).Target(equipmentSelectionState);
			repairState.OnExit(CloseEquipmentPopup);

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

		private void OpenFusePopup()
		{
			OpenPopup(EquipmentPopupPresenter.Mode.Fuse);
		}
		private void OpenRepairPopup()
		{
			OpenPopup(EquipmentPopupPresenter.Mode.Repair);
		}


		private void CloseEquipmentPopup()
		{
			_uiService.CloseUi<EquipmentPopupPresenter>();
		}

		private async void OpenPopup(EquipmentPopupPresenter.Mode mode)
		{
			var data = new EquipmentPopupPresenter.StateData
			{
				EquipmentIds = new[] {_uiService.GetUi<EquipmentSelectionPresenter>().SelectedItem},
				PopupMode = mode,
				OnActionConfirmed = OnPopupActionConfirmed,
				OnCloseClicked = () => _statechartTrigger(CloseButtonClickedEvent)
			};

			await _uiService.OpenUiAsync<EquipmentPopupPresenter, EquipmentPopupPresenter.StateData>(data);
		}

		private void OnPopupActionConfirmed(EquipmentPopupPresenter.Mode mode, UniqueId id)
		{
			bool sameItem = true;
			switch (mode)
			{
				case EquipmentPopupPresenter.Mode.Scrap:
					sameItem = false;
					_services.CommandService.ExecuteCommand(new ScrapItemCommand {Item = id});
					break;
				case EquipmentPopupPresenter.Mode.Upgrade:
					_services.CommandService.ExecuteCommand(new UpgradeItemCommand {Item = id});
					break;
				case EquipmentPopupPresenter.Mode.Repair:
					_services.CommandService.ExecuteCommand(new RepairItemCommand {Item = id});
					break;

					//TODO: Fuse item command must be made and referenced here, if this comment is still here it means I forgort and didnt do it :(
				case EquipmentPopupPresenter.Mode.Fuse:
					_services.CommandService.ExecuteCommand(new UpgradeItemCommand { Item = id });
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
			}

			_uiService.GetUi<EquipmentSelectionPresenter>().RefreshItems(!sameItem);

			_statechartTrigger(_itemProcessedEvent);
		}

		private void OpenEquipmentScreen()
		{
			var data = new EquipmentPresenter.StateData
			{
				OnSlotButtonClicked = SlotButtonClicked,
				OnBackClicked = () => _statechartTrigger(_backButtonClickedEvent),
				OnHomeClicked = () => _statechartTrigger(CloseButtonClickedEvent),
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
			
			_services.MessageBrokerService.Publish(new EquipmentSlotOpenedMessage() {Slot = slot} );
		}

		private void OpenEquipmentSelectionScreen()
		{
			var data = new EquipmentSelectionPresenter.StateData
			{
				EquipmentSlot = _equipmentSlotTypePicked,
				OnBackClicked = () => _statechartTrigger(_backButtonClickedEvent),
				OnCloseClicked = () => _statechartTrigger(CloseButtonClickedEvent),
				OnScrapClicked = () => _statechartTrigger(_scrapClickedEvent),
				OnUpgradeClicked = () => _statechartTrigger(_upgradeClickedEvent),
				OnFuseClicked = () => _statechartTrigger(_fuseClickedEvent),
				OnRepairClicked = () => _statechartTrigger(_repairClickedEvent),
			};

			_uiService.OpenScreen<EquipmentSelectionPresenter, EquipmentSelectionPresenter.StateData>(data);
		}
	}
}