using System;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using UnityEngine;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using FirstLight.Game.Views.MainMenuViews;
using Quantum;
using Sirenix.OdinInspector;
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
		
		[SerializeField, Required] private List<GameModeButtonView> _modeAndMatchButtons;
		[SerializeField, Required] private GameModeButtonView _testingButton;
		[SerializeField, Required] private Button _backButton;

		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();

			foreach (var button in _modeAndMatchButtons)
			{
				button.GameModeAndMatchTypeSelected += OnModeButtonClicked;
			}

			
			_testingButton.gameObject.SetActive(FeatureFlags.TESTING_GAME_MODE_ENABLED);
			_testingButton.GameModeAndMatchTypeSelected += OnModeButtonClicked;
			_backButton.onClick.AddListener(OnBlockerButtonPressed);
		}

		private void OnDestroy()
		{
			foreach (var button in _modeAndMatchButtons)
			{
				button.GameModeAndMatchTypeSelected -= OnModeButtonClicked;
			}
			
			_backButton.onClick.RemoveAllListeners();
		}

		private void OnModeButtonClicked(string gameMode, MatchType matchType)
		{
			_gameDataProvider.AppDataProvider.SelectedGameModeId.Value = gameMode;
			_gameDataProvider.AppDataProvider.SelectedMatchType.Value = matchType;
			Data.GameModeChosen();
		}

		private void OnBlockerButtonPressed()
		{
			Data.GameModeChosen();
		}
	}
}