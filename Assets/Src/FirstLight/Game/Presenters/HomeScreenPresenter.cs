using System;
using DG.Tweening;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
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
		private const string POOL_TIME_FORMAT = "+{0} {1} IN {2}";
		private const string CS_POOL_AMOUNT_FORMAT = "<color=#FE6C07>{0}</color> / {1}";
		private const string BPP_POOL_AMOUNT_FORMAT = "<color=#49D4D4>{0}</color> / {1}";

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

		private Label _bppPoolTimeLabel;
		private Label _bppPoolAmountLabel;
		private VisualElement _csPoolContainer;
		private Label _csPoolTimeLabel;
		private Label _csPoolAmountLabel;

		private bool _rewardsCollecting;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_gameServices = MainInstaller.Resolve<IGameServices>();
			_mainMenuServices = MainInstaller.Resolve<IMainMenuServices>();
		}

		private void Update()
		{
			if (IsOpen)
			{
				UpdatePoolLabels();
			}
		}

		protected override void QueryElements(VisualElement root)
		{
			_playerNameLabel = root.Q<Label>("PlayerNameLabel").Required();
			_playerTrophiesLabel = root.Q<Label>("PlayerTrophiesLabel").Required();
			_gameModeLabel = root.Q<Label>("GameModeLabel").Required();
			_gameTypeLabel = root.Q<Label>("GameTypeLabel").Required();

			_bppPoolAmountLabel = root.Q<VisualElement>("BPPPoolContainer").Q<Label>("AmountLabel").Required();
			_bppPoolTimeLabel = root.Q<VisualElement>("BPPPoolContainer").Q<Label>("RestockLabel").Required();
			_csPoolContainer = root.Q<VisualElement>("CSPoolContainer").Required();
			_csPoolAmountLabel = _csPoolContainer.Q<Label>("AmountLabel").Required();
			_csPoolTimeLabel = _csPoolContainer.Q<Label>("RestockLabel").Required();

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
					_ => { _gameServices.AudioFxService.PlayClip2D(AudioId.ButtonClickForward); },
					TrickleDown.TrickleDown);
			});

			_playerNameLabel.RegisterCallback<ClickEvent>(OnPlayerNameClicked);

			_gameServices.MessageBrokerService.Subscribe<UnclaimedRewardsCollectingStartedMessage>(_ =>
				_rewardsCollecting = true);
			_gameServices.MessageBrokerService.Subscribe<UnclaimedRewardsCollectedMessage>(_ =>
				_rewardsCollecting = false);
		}

		[Button]
		public void SetCollecting(bool collecting)
		{
			_rewardsCollecting = collecting;
		}

		protected override void SubscribeToEvents()
		{
			_gameDataProvider.AppDataProvider.DisplayName.InvokeObserve(OnDisplayNameChanged);
			_gameDataProvider.PlayerDataProvider.Trophies.InvokeObserve(OnTrophiesChanged);
			_gameDataProvider.CurrencyDataProvider.Currencies.InvokeObserve(GameId.CS, OnCurrencyChanged);
			_gameDataProvider.CurrencyDataProvider.Currencies.InvokeObserve(GameId.BLST, OnCurrencyChanged);
			_gameDataProvider.ResourceDataProvider.ResourcePools.InvokeObserve(GameId.CS, OnPoolChanged);
			_gameDataProvider.ResourceDataProvider.ResourcePools.InvokeObserve(GameId.BPP, OnPoolChanged);
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
			_gameDataProvider.CurrencyDataProvider.Currencies.StopObserving(GameId.BLST);
			_gameDataProvider.ResourceDataProvider.ResourcePools.StopObserving(GameId.CS);
			_gameDataProvider.ResourceDataProvider.ResourcePools.StopObserving(GameId.BPP);
			_gameServices.MessageBrokerService.UnsubscribeAll(this);
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
			_gameModeLabel.text = current.Entry.GameModeId.ToUpper();
			_gameTypeLabel.text = current.Entry.MatchType.ToString().ToUpper();
			_csPoolContainer.style.display =
				current.Entry.MatchType == MatchType.Casual ? DisplayStyle.None : DisplayStyle.Flex;
		}

		private void OnCurrencyChanged(GameId id, ulong previous, ulong current, ObservableUpdateType updateType)
		{
			if (id != GameId.CS && id != GameId.BLST) return;

			var label = GetRewardLabel(id);
			if (_rewardsCollecting)
			{
				AnimateCurrency(id, previous, current, label);
			}
			else
			{
				label.text = current.ToString();
			}
		}

		private void AnimateCurrency(GameId id, ulong previous, ulong current, Label label)
		{
			for (int i = 0; i < Mathf.Min(10, current - previous); i++)
			{
				_mainMenuServices.UiVfxService.PlayVfx(id,
					i * 0.1f,
					Root.GetPositionOnScreen(Root),
					label.GetPositionOnScreen(Root),
					() => { DOVirtual.Float(previous, current, 0.3f, val => { label.text = val.ToString("F0"); }); });
			}
		}
		

		private void OnPoolChanged(GameId id, ResourcePoolData previous, ResourcePoolData current,
			ObservableUpdateType updateType)
		{
			UpdatePoolLabels();
		}

		private void UpdatePoolLabels()
		{
			UpdatePool(GameId.BPP, BPP_POOL_AMOUNT_FORMAT, _bppPoolTimeLabel, _bppPoolAmountLabel);
			UpdatePool(GameId.CS, CS_POOL_AMOUNT_FORMAT, _csPoolTimeLabel, _csPoolAmountLabel);
		}

		private void UpdatePool(GameId id, string amountStringFormat, Label timeLabel, Label amountLabel)
		{
			var poolInfo = _gameDataProvider.ResourceDataProvider.GetResourcePoolInfo(id);
			var timeLeft = poolInfo.NextRestockTime - DateTime.UtcNow;

			if (poolInfo.IsFull)
			{
				timeLabel.text = ScriptLocalization.MainMenu.ResoucePoolFull;
			}
			else
			{
				timeLabel.text = string.Format(POOL_TIME_FORMAT,
					poolInfo.RestockPerInterval,
					id.ToString(),
					timeLeft.ToHoursMinutesSeconds());
			}

			amountLabel.text = string.Format(amountStringFormat, poolInfo.CurrentAmount, poolInfo.PoolCapacity);
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
			_battlePassProgressElement.style.flexGrow =
				Mathf.Clamp01((float) predictedPoints / battlePassConfig.PointsPerLevel);
			_battlePassCrownIcon.style.display = hasRewards ? DisplayStyle.Flex : DisplayStyle.None;

			if (predictedLevel == _gameDataProvider.BattlePassDataProvider.MaxLevel)
			{
				_battlePassProgressElement.style.flexGrow = 1f;
			}

			UpdateBattlePassLevel(predictedLevel);
		}

		private Label GetRewardLabel(GameId id)
		{
			return id switch
			{
				GameId.CS => _csAmountLabel,
				GameId.BLST => _blstAmountLabel,
				_ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
			};
		}
	}
}