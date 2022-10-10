using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Home Screen.
	/// </summary>
	[LoadSynchronously]
	public class HomeScreenPresenter : UiToolkitPresenterData<HomeScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action OnPlayButtonClicked;
			public Action OnSettingsButtonClicked;
			public Action OnLootButtonClicked;
			public Action OnHeroesButtonClicked;
			public Action OnPlayRoomJoinCreateClicked;
			public Action OnNameChangeClicked;
			public Action OnGameModeClicked;
			public Action OnLeaderboardClicked;
			public Action OnBattlePassClicked;
		}

		private IGameDataProvider _gameDataProvider;
		private IGameServices _gameServices;
		private IMainMenuServices _mainMenuServices;

		private Label _playerNameLabel;
		private Label _playerTrophiesLabel;
		private Label _gameModeLabel;
		private Label _gameTypeLabel;
		private Label _csAmountLabel;
		private Label _blstAmountLabel;
		private Label _battlePassLevelLabel;
		private VisualElement _battlePassProgressElement;
		private VisualElement _battlePassCrownIcon;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_gameServices = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_playerNameLabel = root.Q<Label>("PlayerNameLabel").Required();
			_playerTrophiesLabel = root.Q<Label>("PlayerTrophiesLabel").Required();
			_gameModeLabel = root.Q<Label>("GameModeLabel").Required();
			_gameTypeLabel = root.Q<Label>("GameTypeLabel").Required();

			_csAmountLabel = root.Q<VisualElement>("CSCurrency").Q<Label>("Label").Required();
			_blstAmountLabel = root.Q<VisualElement>("BLSTCurrency").Q<Label>("Label").Required();
			_battlePassLevelLabel = root.Q<Label>("BattlePassLevelLabel").Required();
			_battlePassProgressElement = root.Q<VisualElement>("BattlePassProgressElement").Required();
			_battlePassCrownIcon = root.Q<VisualElement>("BattlePassCrownIcon").Required();

			root.Q<Button>("PlayButton").clicked += OnPlayButtonClicked;
			root.Q<Button>("GameModeButton").clicked += OnGameModeClicked;
			root.Q<Button>("SettingsButton").clicked += OnSettingsButtonClicked;
			root.Q<Button>("BattlePassButton").clicked += OnBattlePassButtonClicked;
			root.Q<Button>("CustomGameButton").clicked += OnCustomGameClicked;

			root.Q<Button>("EquipmentButton").clicked += OnEquipmentButtonClicked;
			root.Q<Button>("HeroesButton").clicked += OnHeroesButtonClicked;
			root.Q<Button>("LeaderboardsButton").clicked += OnLeaderboardsButtonClicked;

			// TODO: Move to shared code
			root.Query<Button>().Build().ForEach(b =>
			{
				b.RegisterCallback<PointerDownEvent>(
					e => { _gameServices.AudioFxService.PlayClip2D(AudioId.ButtonClickForward); },
					TrickleDown.TrickleDown);
			});

			_playerNameLabel.RegisterCallback<ClickEvent>(OnPlayerNameClicked);
		}

		protected override void SubscribeToEvents()
		{
			_gameDataProvider.AppDataProvider.DisplayName.InvokeObserve(OnDisplayNameChanged);
			_gameDataProvider.PlayerDataProvider.Trophies.InvokeObserve(OnTrophiesChanged);
			_gameDataProvider.CurrencyDataProvider.Currencies.InvokeObserve(GameId.CS, OnCSCurrencyChanged);
			_gameDataProvider.CurrencyDataProvider.Currencies.InvokeObserve(GameId.BLST, OnBLSTCurrencyChanged);
			_gameDataProvider.BattlePassDataProvider.CurrentLevel.InvokeObserve(OnBattlePassCurrentLevelChanged);
			_gameDataProvider.BattlePassDataProvider.CurrentPoints.InvokeObserve(OnBattlePassCurrentPointsChanged);
			_gameServices.GameModeService.SelectedGameMode.InvokeObserve(OnSelectedGameModeChanged);
		}

		protected override void UnsubscribeFromEvents()
		{
			_gameDataProvider.AppDataProvider.DisplayName.StopObserving(OnDisplayNameChanged);
			_gameDataProvider.PlayerDataProvider.Trophies.StopObserving(OnTrophiesChanged);
			_gameServices.GameModeService.SelectedGameMode.StopObserving(OnSelectedGameModeChanged);
			_gameDataProvider.CurrencyDataProvider.Currencies.StopObserving(GameId.CS);
		}

		private void OnPlayButtonClicked()
		{
			Data.OnPlayButtonClicked();
		}

		private void OnGameModeClicked()
		{
			Data.OnGameModeClicked();
		}

		private void OnSettingsButtonClicked()
		{
			Data.OnSettingsButtonClicked();
		}

		private void OnBattlePassButtonClicked()
		{
			Data.OnBattlePassClicked();
		}

		private void OnCustomGameClicked()
		{
			Data.OnPlayRoomJoinCreateClicked();
		}

		private void OnEquipmentButtonClicked()
		{
			Data.OnLootButtonClicked();
		}

		private void OnHeroesButtonClicked()
		{
			Data.OnHeroesButtonClicked();
		}

		private void OnLeaderboardsButtonClicked()
		{
			Data.OnLeaderboardClicked();
		}

		private void OnPlayerNameClicked(ClickEvent evt)
		{
			Data.OnNameChangeClicked();
		}

		private void OnTrophiesChanged(uint _, uint current)
		{
			_playerTrophiesLabel.text = current.ToString();
		}

		private void OnDisplayNameChanged(string _, string current)
		{
			_playerNameLabel.text = _gameDataProvider.AppDataProvider.DisplayNameTrimmed;
		}

		private void OnSelectedGameModeChanged(GameModeInfo _, GameModeInfo current)
		{
			UpdateGameModeButton(current);
		}

		private void OnCSCurrencyChanged(GameId id, ulong previous, ulong current, ObservableUpdateType updateType)
		{
			if (id != GameId.CS) return;

			//_mainMenuServices.UiVfxService.PlayVfx(GameId.CS, Vector3.zero, );

			_csAmountLabel.text = current.ToString();
		}

		private void OnBLSTCurrencyChanged(GameId id, ulong previous, ulong current, ObservableUpdateType updateType)
		{
			if (id != GameId.BLST) return;

			_blstAmountLabel.text = current.ToString();
		}

		private void OnBattlePassCurrentLevelChanged(uint _, uint current)
		{
			UpdateBattlePassLevel(_gameDataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints().Item1);
		}

		private void OnBattlePassCurrentPointsChanged(uint _, uint current)
		{
			var predictedLevelAndPoints = _gameDataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints();
			UpdateBattlePassPoints(predictedLevelAndPoints.Item1, predictedLevelAndPoints.Item2);
		}

		private void UpdateBattlePassLevel(uint predictedLevel)
		{
			var maxLevel = _gameDataProvider.BattlePassDataProvider.MaxLevel;
			var nextLevel = Math.Clamp(predictedLevel + 1, 0, maxLevel) + 1;
			_battlePassLevelLabel.text = nextLevel.ToString();
		}

		private void UpdateGameModeButton(GameModeInfo current)
		{
			_gameModeLabel.text = current.Entry.GameModeId.ToUpper();
			_gameTypeLabel.text = current.Entry.MatchType.ToString().ToUpper();
		}

		private void UpdateBattlePassPoints(uint predictedLevel, uint predictedPoints)
		{
			var battlePassConfig = _gameServices.ConfigsProvider.GetConfig<BattlePassConfig>();
			var hasRewards = _gameDataProvider.BattlePassDataProvider.IsRedeemable();
			_battlePassProgressElement.style.flexGrow =
				Mathf.Clamp01((float) predictedPoints / battlePassConfig.PointsPerLevel);
			_battlePassCrownIcon.style.display = hasRewards ? DisplayStyle.Flex : DisplayStyle.None;

			if (predictedLevel == _gameDataProvider.BattlePassDataProvider.MaxLevel)
			{
				_battlePassProgressElement.style.flexGrow = 1f;
			}

			UpdateBattlePassLevel(predictedLevel);
		}

		// private void OnPlayUiVfxMessage(PlayUiVfxMessage message)
		// {
		// 	var closure = message;
		//
		// 	if (message.Id == _targetID)
		// 	{
		// 		_mainMenuServices.UiVfxService.PlayVfx(message.Id, message.OriginWorldPosition,
		// 			_animationTarget.position, () => RackupTween(UpdateAmountText));
		// 	}
		//
		// 	void RackupTween(TweenCallback<float> textUpdated)
		// 	{
		// 		var targetValue = GetAmount();
		// 		var initialValue = targetValue - (int) closure.Quantity;
		//
		// 		DOVirtual.Float(initialValue, targetValue, _rackupTextAnimDurationSeconds, textUpdated);
		// 	}
		// }
	}
}