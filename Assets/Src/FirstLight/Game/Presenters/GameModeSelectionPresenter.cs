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

		[SerializeField, Required] private GameModeButtonView _gameMode1;
		[SerializeField, Required] private GameModeButtonView _gameMode2;
		[SerializeField, Required] private GameModeRotationView _rotationGameMode;
		[SerializeField, Required] private GameModeButtonView _testingButton;
		[SerializeField, Required] private Button _backButton;

		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();

			_backButton.onClick.AddListener(OnBlockerButtonPressed);

			_testingButton.gameObject.SetActive(FeatureFlags.TESTING_GAME_MODE_ENABLED);
			_testingButton.Init("Testing", new List<string>(), MatchType.Casual, false, default, OnModeButtonClicked);

			var rotationConfig = _services.ConfigsProvider.GetConfig<GameModeRotationConfig>();
			_gameMode1.Init(rotationConfig.GameModeId1, new List<string>(), rotationConfig.MatchType1, false, default,
			                OnModeButtonClicked);
			_gameMode2.Init(rotationConfig.GameModeId2, new List<string>(), rotationConfig.MatchType2, false, default,
			                OnModeButtonClicked);

			_services.GameModeService.RotationGameMode.InvokeObserve(OnRotationGameModeChanged);
		}

		private void OnRotationGameModeChanged(GameModeRotationInfo previous, GameModeRotationInfo current)
		{
			_rotationGameMode.Init(current, OnModeButtonClicked);
		}

		private void OnDestroy()
		{
			_backButton.onClick.RemoveAllListeners();
			_services.GameModeService.RotationGameMode.StopObserving(OnRotationGameModeChanged);
		}

		private void OnModeButtonClicked(string gameMode, List<string> mutators, MatchType matchType, bool fromRotation,
		                                 DateTime endTime)
		{
			_services.GameModeService.SelectedGameMode.Value =
				new SelectedGameModeInfo(gameMode, matchType, mutators, fromRotation, endTime);

			Data.GameModeChosen();
		}

		private void OnBlockerButtonPressed()
		{
			Data.GameModeChosen();
		}
	}
}