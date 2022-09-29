using System;
using UnityEngine;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.UiService;
using Quantum;
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

			_playerNameLabel = _root.Q<Label>("PlayerNameLabel");
			_playerTrophiesLabel = _root.Q<Label>("PlayerTrophiesLabel");
			_gameModeLabel = _root.Q<Label>("GameModeLabel");
			_gameTypeLabel = _root.Q<Label>("GameTypeLabel");
			_battlePassCrownIcon = _root.Q<Label>("BattlePassCrownIcon");

			// TODO: Probably a better way to query this, with .Query<>
			_csAmountLabel = _root.Q<VisualElement>("CSCurrency").Q<Label>("Label");
			_blstAmountLabel = _root.Q<VisualElement>("BLSTCurrency").Q<Label>("Label");
			_battlePassLevelLabel = _root.Q<Label>("BattlePassLevelLabel");
			_battlePassProgressElement = _root.Q<VisualElement>("BattlePassProgressElement");

			_root.Q<Button>("PlayButton").clicked += OnPlayButtonClicked;
			_root.Q<Button>("GameModeButton").clicked += OnGameModeClicked;
			_root.Q<Button>("SettingsButton").clicked += OnSettingsButtonClicked;
			_root.Q<Button>("BattlePassButton").clicked += OnBattlePassButtonClicked;
			_root.Q<Button>("CustomGameButton").clicked += OnCustomGameClicked;

			_root.Q<Button>("EquipmentButton").clicked += OnEquipmentButtonClicked;
			_root.Q<Button>("HeroesButton").clicked += OnHeroesButtonClicked;
			_root.Q<Button>("MarketplaceButton").clicked += OnMarketplaceButtonClicked;
			_root.Q<Button>("LeaderboardsButton").clicked += OnLeaderboardsButtonClicked;
			
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
			_gameDataProvider.CurrencyDataProvider.Currencies.StopObservingAll(this);
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			if (_root == null) return; // First open

			_root.style.display = DisplayStyle.Flex;
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			_root.style.display = DisplayStyle.None;
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
			_playerNameLabel.text = current;
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
			_battlePassLevelLabel.text = current.ToString();
		}

		private void OnBattlePassCurrentPointsChanged(uint _, uint current)
		{
			var hasRewards = _gameDataProvider.BattlePassDataProvider.IsRedeemable(out var nextLevel);
			_battlePassLevelLabel.style.flexGrow = Mathf.Clamp01((float) current / nextLevel);
			_battlePassCrownIcon.style.display = hasRewards ? DisplayStyle.Flex : DisplayStyle.None;
		}
	}
}