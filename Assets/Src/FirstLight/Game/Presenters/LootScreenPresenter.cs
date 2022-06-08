using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MainMenuViews;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Loot Screen, where players can equip items and upgrade loot.
	/// </summary>
	public class LootScreenPresenter : AnimatedUiPresenterData<LootScreenPresenter.StateData>
	{
		private const float LATE_CALL_DELAY = 0.1f;
		
		public struct StateData
		{
			public Action OnAllGearClicked;
			public Action<GameIdGroup> OnSlotButtonClicked;
			public Action<UniqueId> OnEquipmentButtonClicked;
			public Action OnChangeSkinClicked;
			public Action OnLootBackButtonClicked;
			public Func<List<UniqueId>> CurrentTempLoadout;
		}

		[SerializeField] private FilterLootView[] _filterButtons;
		[SerializeField] private EquippedLootView[] _equipmentButtons;
		[SerializeField, Required] private Button _allGearButton;
		[SerializeField, Required] private Button _changeSkinButton;
		[SerializeField, Required] private Button _backButton;
		[SerializeField, Required] private TextMeshProUGUI _playerNameText;
		[SerializeField, Required] private TextMeshProUGUI _playerLevelText;
		[SerializeField, Required] private TextMeshProUGUI _powerRatingText;
		[SerializeField, Required] private TextMeshProUGUI _powerValueText;
		[SerializeField, Required] private Slider _playerLevelSlider;
		[SerializeField, Required] private Image _playerLevelBadge;
		[SerializeField, Required] private Button _blockerButton;
		
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_allGearButton.onClick.AddListener(OnAllGearClicked);
			_changeSkinButton.onClick.AddListener(OnChangeSkinClicked);
			_backButton.onClick.AddListener(OnBackButtonPressed);
			_blockerButton.onClick.AddListener(OnBlockerButtonPressed);

			foreach (var button in _equipmentButtons)
			{
				button.OnClick.AddListener(OnSlotClicked);
			}

			foreach (var button in _filterButtons)
			{
				button.OnClick.AddListener(OnSlotClicked);
			}

			LoadPlayerLevelInformation();
		}

		protected override void OnSetData()
		{
			base.OnSetData();

			LoadPlayerLevelInformation();
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

			// Late call as the state data is not yet set in the same frame, and we need access to some of its properties
			this.LateCall(Time.deltaTime, SetBasicPlayerInformation);
		}

		private void SetBasicPlayerInformation()
		{
			_playerNameText.text = _gameDataProvider.AppDataProvider.Nickname;
			_powerRatingText.text = ScriptLocalization.MainMenu.TotalPower;
			_powerValueText.text = _gameDataProvider.EquipmentDataProvider
			                                        .GetTotalEquippedStat(StatType.Power, Data.CurrentTempLoadout())
			                                        .ToString("F0");
		}

		private async void LoadPlayerLevelInformation()
		{
			var level = _gameDataProvider.PlayerDataProvider.Level.Value;
			_playerLevelBadge.sprite = await _services.AssetResolverService.RequestAsset<int, Sprite>((int) level);
			_playerLevelText.text = level.ToString("N0");
			_playerLevelSlider.value = GetXpSliderValue();
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

		private void OnBlockerButtonPressed()
		{
			Data.OnLootBackButtonClicked();
		}
	}
}