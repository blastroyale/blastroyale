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

		[SerializeField, Required] private List<GameModeButtonView> _fixedSlots;
		[SerializeField, Required] private GameModeRotationView _rotationSlot1;
		[SerializeField, Required] private GameModeRotationView _rotationSlot2;
		[SerializeField, Required] private GameModeButtonView _testingButton;
		[SerializeField, Required] private Button _backButton;

		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();

			_backButton.onClick.AddListener(OnBlockerButtonPressed);

			_testingButton.gameObject.SetActive(FeatureFlags.TESTING_GAME_MODE_ENABLED);
			_testingButton.Init("Testing", new List<string>(), MatchType.Casual, false, default, OnModeButtonClicked);

			var fixedSlots = _services.GameModeService.FixedSlots;
			for (var i = 0; i < fixedSlots.Count; i++)
			{
				var entry = fixedSlots[i];
				_fixedSlots[i].Init(entry.GameModeId, entry.Mutators, entry.MatchType, false, default, OnModeButtonClicked);
			}

			_services.GameModeService.RotationSlot1.InvokeObserve(OnRotationGameMode1Changed);
			_services.GameModeService.RotationSlot2.InvokeObserve(OnRotationGameMode2Changed);
		}

		private void OnRotationGameMode1Changed(GameModeRotationInfo previous, GameModeRotationInfo current)
		{
			_rotationSlot1.Init(current, OnModeButtonClicked);
		}
		
		private void OnRotationGameMode2Changed(GameModeRotationInfo previous, GameModeRotationInfo current)
		{
			_rotationSlot2.Init(current, OnModeButtonClicked);
		}

		private void OnDestroy()
		{
			_backButton.onClick.RemoveAllListeners();
			_services.GameModeService.RotationSlot1.StopObserving(OnRotationGameMode1Changed);
			_services.GameModeService.RotationSlot2.StopObserving(OnRotationGameMode2Changed);
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