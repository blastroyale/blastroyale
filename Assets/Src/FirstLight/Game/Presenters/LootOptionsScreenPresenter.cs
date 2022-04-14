using System;
using FirstLight.Game.Configs;
using UnityEngine;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MainMenuViews;
using I2.Loc;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Loot Options Screen, which has access points to Equipment, Fusion, Enhancement
	/// and Heroes.
	/// </summary>
	public class LootOptionsScreenPresenter : AnimatedUiPresenterData<LootOptionsScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action OnEquipmentButtonClicked;
			public Action OnChangeSkinClicked;
			public Action OnFuseClicked;
			public Action OnEnhanceClicked;
			public Action OnLootBackButtonClicked;
		}
		[SerializeField] private VisualStateButtonView _equipmentButton;
		[SerializeField] private VisualStateButtonView _fuseButton;
		[SerializeField] private VisualStateButtonView _enhanceButton;
		[SerializeField] private Button _changeSkinButton;
		[SerializeField] private Button _backButton;

		private IMainMenuServices _mainMenuServices;
		private IGameDataProvider _gameDataProvider;

		private void Start()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_mainMenuServices = MainMenuInstaller.Resolve<IMainMenuServices>();
			
			_equipmentButton.Button.onClick.AddListener(OnEquipmentButtonClicked); 
			_changeSkinButton.onClick.AddListener(OnChangeSkinClicked);
			_fuseButton.Button.onClick.AddListener(OnFuseClicked);
			_enhanceButton.Button.onClick.AddListener(OnEnhanceClicked);
			_backButton.onClick.AddListener(OnBackButtonPressed);
		}

		protected override void OnOpened()
		{
			var unlocked = _gameDataProvider.PlayerDataProvider.CurrentUnlockedSystems;
			var tagged = _gameDataProvider.PlayerDataProvider.SystemsTagged;
			
			base.OnOpened();
			
			_equipmentButton.UpdateState(true, false, false);
			_fuseButton.UpdateState(unlocked.Contains(UnlockSystem.Fusion), !tagged.Contains(UnlockSystem.Fusion), false);
			_enhanceButton.UpdateState(unlocked.Contains(UnlockSystem.Enhancement), !tagged.Contains(UnlockSystem.Enhancement), false);
		}

		private void OnEquipmentButtonClicked()
		{
			Data.OnEquipmentButtonClicked();
		}

		private void OnFuseClicked()
		{
			if (!ButtonClickSystemCheck(UnlockSystem.Fusion))
			{
				return;
			}
			
			Data.OnFuseClicked();
		}
		
		private void OnEnhanceClicked()
		{
			if (!ButtonClickSystemCheck(UnlockSystem.Enhancement))
			{
				return;
			}
			
			Data.OnEnhanceClicked();
		}

		private void OnChangeSkinClicked()
		{
			Data.OnChangeSkinClicked();
		}

		private void OnBackButtonPressed()
		{
			Data.OnLootBackButtonClicked();
		}

		private bool ButtonClickSystemCheck(UnlockSystem system)
		{
			var unlockLevel = _gameDataProvider.PlayerDataProvider.GetUnlockSystemLevel(system);
			
			if (_gameDataProvider.PlayerDataProvider.Level.Value < unlockLevel)
			{
				var unlockAtText = string.Format(ScriptLocalization.General.UnlockAtPlayerLevel, unlockLevel.ToString());
				
				_mainMenuServices.UiVfxService.PlayFloatingText(unlockAtText);
				
				return false;
			}
			
			var tagged = _gameDataProvider.PlayerDataProvider.SystemsTagged;

			if (!tagged.Contains(system))
			{
				tagged.Add(system);
			}

			return true;
		}
	}
}