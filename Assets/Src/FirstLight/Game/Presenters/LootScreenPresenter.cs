using System;
using FirstLight.Game.Configs;
using UnityEngine;
using UnityEngine.UI;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MainMenuViews;
using I2.Loc;
using Quantum;
using TMPro;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Loot Screen, where players can equip items and upgrade loot.
	/// </summary>
	public class LootScreenPresenter : AnimatedUiPresenterData<LootScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action OnAllGearClicked;
			public Action<GameIdGroup> OnSlotButtonClicked;
			public Action<UniqueId> OnEquipmentButtonClicked;
			public Action OnChangeSkinClicked;
			public Action OnLootBackButtonClicked;
		}
		
		[SerializeField] private FilterLootView[] _filterButtons;
		[SerializeField] private EquippedLootView[] _equipmentButtons;
		[SerializeField] private Button _allGearButton;
		[SerializeField] private Button _changeSkinButton;
		[SerializeField] private Button _backButton;
		[SerializeField] private TextMeshProUGUI _playerNameText;
		[SerializeField] private TextMeshProUGUI _playerLevelText;
		[SerializeField] private TextMeshProUGUI _powerRatingText;
		[SerializeField] private TextMeshProUGUI _powerValueText;
		[SerializeField] private Slider _playerLevelSlider;
		[SerializeField] private Image _playerLevelBadge;
		
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private void Start()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_allGearButton.onClick.AddListener(OnAllGearClicked); 
			_changeSkinButton.onClick.AddListener(OnChangeSkinClicked);
			_backButton.onClick.AddListener(OnBackButtonPressed);

			foreach (var button in _equipmentButtons)
			{
				button.OnClick.AddListener(OnSlotClicked);
			}
			
			foreach (var button in _filterButtons)
			{
				button.OnClick.AddListener(OnSlotClicked);
			}
			
			SetPlayerLevelInformation();
		}

		protected override void OnSetData()
		{
			base.OnSetData();
			
			SetPlayerLevelInformation();
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			
			foreach (var button in _equipmentButtons)
			{
				button.UpdateItem();
			}

			foreach (var filterButton in _filterButtons)
			{
				filterButton.SetNotificationState();
			}
		}

		private async void SetPlayerLevelInformation()
		{
			var level = _gameDataProvider.PlayerDataProvider.Level.Value;
			
			_playerNameText.text = _gameDataProvider.PlayerDataProvider.Nickname;
			_playerLevelText.text = level.ToString("N0");
			_playerLevelSlider.value = GetXpSliderValue();
			_powerRatingText.text = ScriptLocalization.MainMenu.TotalPower;
			_powerValueText.text = _gameDataProvider.EquipmentDataProvider.GetTotalEquippedItemPower().ToString();
			_playerLevelBadge.sprite = await _services.AssetResolverService.RequestAsset<int, Sprite>((int) level);
		}
		
		private float GetXpSliderValue()
		{
			var info = _gameDataProvider.PlayerDataProvider.CurrentLevelInfo;
			
			return (float) info.Xp / info.Config.LevelUpXP;
		}

		private void OnAllGearClicked()
		{
			Data.OnAllGearClicked();
		}
		
		private void OnSlotClicked(GameIdGroup slot)
		{
			Data.OnSlotButtonClicked(slot);
		}

		private void OnChangeSkinClicked()
		{
			Data.OnChangeSkinClicked();
		}

		private void OnBackButtonPressed()
		{
			Data.OnLootBackButtonClicked();
		}
	}
}