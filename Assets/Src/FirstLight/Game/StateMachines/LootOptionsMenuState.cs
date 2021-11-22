using System;
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
	/// This object contains the behaviour logic for the Loot Menu State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class LootOptionsMenuState
	{
		private readonly IStatechartEvent _skinClickedEvent = new StatechartEvent("Skin Clicked Event");
		private readonly IStatechartEvent _fuseClickedEvent = new StatechartEvent("Fuse Clicked Event");
		private readonly IStatechartEvent _enhanceClickedEvent = new StatechartEvent("Enhance Clicked Event");
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

		private readonly LootMenuState _lootMenuState;
		private readonly FuseMenuState _fuseMenuState;
		private readonly EnhanceMenuState _enhanceMenuState;

		public LootOptionsMenuState(IGameServices services, IGameUiService uiService, IGameDataProvider gameDataProvider, 
		                     Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_uiService = uiService;
			_gameDataProvider = gameDataProvider;
			_statechartTrigger = statechartTrigger;
			
			_lootMenuState = new LootMenuState(services, uiService, gameDataProvider, statechartTrigger);
			_fuseMenuState = new FuseMenuState(services, uiService, gameDataProvider, statechartTrigger);
			_enhanceMenuState = new EnhanceMenuState(services, uiService, gameDataProvider, statechartTrigger);
		}

		/// <summary>
		/// Setups the Main Menu state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var lootOptionsMenuState = stateFactory.State("Loot Menu State");
			var equipmentScreenState = stateFactory.State("Equipment Screen State");
			var playerSkinPopupState = stateFactory.State("Player Skin Popup State");
			var lootMenu = stateFactory.Nest("Loot Menu");
			var fuseMenu = stateFactory.Nest("Fuse Menu State");
			var enhanceMenu = stateFactory.Nest("Enhance Menu State");
			var final = stateFactory.Final("Final");

			initial.Transition().Target(lootOptionsMenuState);
			initial.OnExit(SubscribeEvents);

			lootOptionsMenuState.OnEnter(OpenLootOptionsMenuUI);
			lootOptionsMenuState.Event(_skinClickedEvent).Target(playerSkinPopupState);
			lootOptionsMenuState.Event(_allGearClickedEvent).Target(lootMenu);
			lootOptionsMenuState.Event(_fuseClickedEvent).Target(fuseMenu);
			lootOptionsMenuState.Event(_enhanceClickedEvent).Target(enhanceMenu);
			lootOptionsMenuState.Event(_backButtonClickedEvent).Target(final);
			lootOptionsMenuState.OnExit(CloseLootOptionsMenuUI);
			
			lootMenu.Nest(_lootMenuState.Setup, true, true).Target(lootOptionsMenuState);
			lootMenu.OnExit(CloseLootMenuUI);
			
			fuseMenu.Nest(_fuseMenuState.Setup, true, true).Target(lootOptionsMenuState);
			fuseMenu.OnExit(CloseFuseMenuUI);

			enhanceMenu.Nest(_enhanceMenuState.Setup, true, true).Target(lootOptionsMenuState);
			enhanceMenu.OnExit(CloseEnhanceMenuUI);

			playerSkinPopupState.OnEnter(OpenPlayerSkinMenuUI);
			playerSkinPopupState.Event(_playerSkinClickedEvent).Target(lootOptionsMenuState);
			playerSkinPopupState.Event(_playerSkinClosedEvent).Target(lootOptionsMenuState);
			playerSkinPopupState.OnExit(ClosePlayerSkinMenuUI);

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

		private void OpenLootOptionsMenuUI()
		{
			var data = new LootOptionsScreenPresenter.StateData
			{
				OnEquipmentButtonClicked = () => _statechartTrigger(_allGearClickedEvent),
				OnChangeSkinClicked = () => _statechartTrigger(_skinClickedEvent),
				OnLootBackButtonClicked = () => _statechartTrigger(_backButtonClickedEvent),
				OnFuseClicked = () => _statechartTrigger(_fuseClickedEvent),
				OnEnhanceClicked = () => _statechartTrigger(_enhanceClickedEvent),
			};

			_uiService.OpenUi<LootOptionsScreenPresenter, LootOptionsScreenPresenter.StateData>(data);
		}

		private void CloseLootOptionsMenuUI()
		{
			_uiService.CloseUi<LootOptionsScreenPresenter>();
		}

		private void SetAllGearSlot()
		{
			_equipmentSlotTypePicked = GameIdGroup.Equipment;
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
				OnCloseClicked = () => _statechartTrigger(_equipmentScreenCloseClickedEvent),
			};

			_uiService.OpenUi<EquipmentScreenPresenter, EquipmentScreenPresenter.StateData>(data);
		}
		
		private void CloseEquipmentMenuUI()
		{
			_uiService.CloseUi<EquipmentScreenPresenter>();
		}
		
		private void CloseLootMenuUI()
		{
			_uiService.CloseUi<LootScreenPresenter>();
		}
		
		private void CloseFuseMenuUI()
		{
			_uiService.CloseUi<FuseScreenPresenter>();
		}

		private void CloseEnhanceMenuUI()
		{
			_uiService.CloseUi<EnhanceScreenPresenter>();
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