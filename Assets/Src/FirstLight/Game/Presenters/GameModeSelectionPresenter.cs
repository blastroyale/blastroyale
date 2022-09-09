using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using UnityEngine;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using FirstLight.Game.Views.MainMenuViews;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles game mode selector
	/// </summary>
	public class GameModeSelectionPresenter : AnimatedUiPresenterData<GameModeSelectionPresenter.StateData>
	{
		public struct StateData
		{
			public Action GameModeChosen;
		}

		[SerializeField, Required] private RectTransform _slotsContainer;
		[SerializeField, Required] private GameModeButtonView _slotPrefab;

		[SerializeField, Required] private GameModeButtonView _testingButton;
		[SerializeField, Required] private Button _backButton;

		private IGameServices _services;

		private List<GameModeButtonView> _slotViews;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();

			_backButton.onClick.AddListener(OnBlockerButtonPressed);

			_testingButton.gameObject.SetActive(FeatureFlags.TESTING_GAME_MODE_ENABLED);
			_testingButton.Init(new GameModeInfo("Testing", MatchType.Custom, new List<string>()), OnModeButtonClicked);

			foreach (var slot in _services.GameModeService.Slots)
			{
				var view = Instantiate(_slotPrefab, _slotsContainer);
				view.Init(slot, OnModeButtonClicked);
				_slotViews.Add(view);
			}

			_services.GameModeService.Slots.Observe(OnSlotUpdated);
		}

		private void OnSlotUpdated(int index, GameModeInfo previous, GameModeInfo current,
		                           ObservableUpdateType updateType)
		{
			Assert.AreEqual(ObservableUpdateType.Updated, updateType);

			_slotViews[index].Init(current, OnModeButtonClicked);
		}

		private void OnDestroy()
		{
			_backButton.onClick.RemoveAllListeners();
			_services.GameModeService.Slots.StopObserving(OnSlotUpdated);
		}

		private void OnModeButtonClicked(GameModeInfo info)
		{
			_services.GameModeService.SelectedGameMode.Value = info;
			Data.GameModeChosen();
		}

		private void OnBlockerButtonPressed()
		{
			Data.GameModeChosen();
		}
	}
}