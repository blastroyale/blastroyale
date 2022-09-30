using System;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using UnityEngine;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.UiService;
using Quantum;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Home Screen.
	/// </summary>
	[LoadSynchronously]
	public class HomeScreenPresenter : UiCloseActivePresenterData<HomeScreenPresenter.StateData>
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

		[SerializeField] private UIDocument _document;

		private VisualElement _root;

		private IGameDataProvider _gameDataProvider;
		private IGameServices _gameServices;

		private Label _playerNameLabel;
		private Label _playerTrophiesLabel;
		private Label _gameModeLabel;
		private Label _gameTypeLabel;
		private Label _csAmountLabel;
		private Label _blstAmountLabel;
		private Label _battlePassLevelLabel;
		private VisualElement _battlePassProgressElement;
		private VisualElement _battlePassCrownIcon;

		private void Start()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_gameServices = MainInstaller.Resolve<IGameServices>();

			_root = _document.rootVisualElement;

			_playerNameLabel = _root.Q<Label>("PlayerNameLabel").Required();
			_playerTrophiesLabel = _root.Q<Label>("PlayerTrophiesLabel").Required();
			_gameModeLabel = _root.Q<Label>("GameModeLabel").Required();
			_gameTypeLabel = _root.Q<Label>("GameTypeLabel").Required();

			// TODO: Probably a better way to query this, with .Query<>
			_csAmountLabel = _root.Q<VisualElement>("CSCurrency").Q<Label>("Label").Required();
			_blstAmountLabel = _root.Q<VisualElement>("BLSTCurrency").Q<Label>("Label").Required();
			_battlePassLevelLabel = _root.Q<Label>("BattlePassLevelLabel").Required();
			_battlePassProgressElement = _root.Q<VisualElement>("BattlePassProgressElement").Required();
			_battlePassCrownIcon = _root.Q<VisualElement>("BattlePassCrownIcon").Required();

			_root.Q<Button>("PlayButton").clicked += OnPlayButtonClicked;
			_root.Q<Button>("GameModeButton").clicked += OnGameModeClicked;
			_root.Q<Button>("SettingsButton").clicked += OnSettingsButtonClicked;
			_root.Q<Button>("BattlePassButton").clicked += OnBattlePassButtonClicked;
			_root.Q<Button>("CustomGameButton").clicked += OnCustomGameClicked;

			_root.Q<Button>("EquipmentButton").clicked += OnEquipmentButtonClicked;
			_root.Q<Button>("HeroesButton").clicked += OnHeroesButtonClicked;
			_root.Q<Button>("MarketplaceButton").clicked += OnMarketplaceButtonClicked;
			_root.Q<Button>("LeaderboardsButton").clicked += OnLeaderboardsButtonClicked;

			// TODO: Move to shared code
			_root.Query<Button>().Build().ForEach(b =>
			{
				b.RegisterCallback<PointerDownEvent>(e => { _gameServices.AudioFxService.PlayClip2D(AudioId.ButtonClickForward); },
				                                     TrickleDown.TrickleDown);
			});

			_playerNameLabel.RegisterCallback<ClickEvent>(OnPlayerNameClicked);

			_gameDataProvider.AppDataProvider.DisplayName.InvokeObserve(OnDisplayNameChanged);
			_gameDataProvider.PlayerDataProvider.Trophies.InvokeObserve(OnTrophiesChanged);
			_gameDataProvider.CurrencyDataProvider.Currencies.InvokeObserve(GameId.CS, OnCSCurrencyChanged);
			_gameDataProvider.CurrencyDataProvider.Currencies.InvokeObserve(GameId.BLST, OnBLSTCurrencyChanged);
			_gameDataProvider.BattlePassDataProvider.CurrentLevel.InvokeObserve(OnBattlePassCurrentLevelChanged);
			_gameDataProvider.BattlePassDataProvider.CurrentPoints.InvokeObserve(OnBattlePassCurrentPointsChanged);
			_gameServices.GameModeService.SelectedGameMode.InvokeObserve(OnSelectedGameModeChanged);
		}

		private void OnDestroy()
		{
			_gameDataProvider.AppDataProvider.DisplayName.StopObserving(OnDisplayNameChanged);
			_gameDataProvider.PlayerDataProvider.Trophies.StopObserving(OnTrophiesChanged);
			_gameServices.GameModeService.SelectedGameMode.StopObserving(OnSelectedGameModeChanged);
			_gameDataProvider.CurrencyDataProvider.Currencies.StopObserving(GameId.CS);
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			if (_root == null) return; // First open

			_root.EnableInClassList("hidden", false);
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			_root.EnableInClassList("hidden", true);
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

		private void OnMarketplaceButtonClicked()
		{
			Application.OpenURL(GameConstants.Links.MARKETPLACE_URL);
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
			_gameModeLabel.text = current.Entry.GameModeId.ToUpper();
			_gameTypeLabel.text = current.Entry.MatchType.ToString().ToUpper();
		}

		private void OnCSCurrencyChanged(GameId id, ulong previous, ulong current, ObservableUpdateType updateType)
		{
			if (id != GameId.CS) return;

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

		private void UpdateBattlePassPoints(uint predictedLevel, uint predictedPoints)
		{
			var battlePassConfig = _gameServices.ConfigsProvider.GetConfig<BattlePassConfig>();
			var hasRewards = _gameDataProvider.BattlePassDataProvider.IsRedeemable();
			_battlePassProgressElement.style.flexGrow = Mathf.Clamp01((float) predictedPoints / battlePassConfig.PointsPerLevel);
			_battlePassCrownIcon.style.display = hasRewards ? DisplayStyle.Flex : DisplayStyle.None;

			if (predictedLevel == _gameDataProvider.BattlePassDataProvider.MaxLevel)
			{
				_battlePassProgressElement.style.flexGrow = 1f;
			}
			
			UpdateBattlePassLevel(predictedLevel);
		}
	}
}