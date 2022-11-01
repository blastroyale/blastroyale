using System;
using System.Collections;
using DG.Tweening;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Home Screen.
	/// </summary>
	[LoadSynchronously]
	public class HomeScreenPresenter : UiToolkitPresenterData<HomeScreenPresenter.StateData>
	{
		private const float CURRENCY_ANIM_DELAY = 2f;

		private const string CS_POOL_AMOUNT_FORMAT = "<color=#FE6C07>{0}</color> / {1}";
		private const string BPP_POOL_AMOUNT_FORMAT = "<color=#49D4D4>{0}</color> / {1}";

		public struct StateData
		{
			public Action OnPlayButtonClicked;
			public Action OnSettingsButtonClicked;
			public Action OnLootButtonClicked;
			public Action OnHeroesButtonClicked;
			public Action OnNameChangeClicked;
			public Action OnGameModeClicked;
			public Action OnLeaderboardClicked;
			public Action OnBattlePassClicked;
			public Action OnStoreClicked;
		}

		private IGameDataProvider _dataProvider;
		private IGameServices _services;
		private IMainMenuServices _mainMenuServices;

		private Button _playButton;
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

		private void Awake()
		{
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			_mainMenuServices = MainInstaller.Resolve<IMainMenuServices>();
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

			_battlePassLevelLabel = root.Q<Label>("BattlePassLevelLabel").Required();
			_battlePassProgressElement = root.Q<VisualElement>("BattlePassProgressElement").Required();
			_battlePassCrownIcon = root.Q<VisualElement>("BattlePassCrownIcon").Required();

			_playButton = root.Q<Button>("PlayButton");
			_playButton.clicked += OnPlayButtonClicked;

			root.Q<CurrencyDisplayElement>("CSCurrency").SetAnimationOrigin(_playButton);
			root.Q<CurrencyDisplayElement>("BLSTCurrency").SetAnimationOrigin(_playButton);

			root.Q<Button>("GameModeButton").clicked += Data.OnGameModeClicked;
			root.Q<Button>("SettingsButton").clicked += Data.OnSettingsButtonClicked;
			root.Q<Button>("BattlePassButton").clicked += Data.OnBattlePassClicked;

			root.Q<Button>("EquipmentButton").clicked += Data.OnLootButtonClicked;
			root.Q<Button>("HeroesButton").clicked += Data.OnHeroesButtonClicked;
			root.Q<Button>("LeaderboardsButton").clicked += Data.OnLeaderboardClicked;
			root.Q<Button>("StoreButton").clicked += Data.OnStoreClicked;

			// TODO: Move to shared code
			root.Query<Button>().Build().ForEach(b =>
			{
				b.RegisterCallback<PointerDownEvent>(
					_ => { _services.AudioFxService.PlayClip2D(AudioId.ButtonClickForward); },
					TrickleDown.TrickleDown);
			});

			_playerNameLabel.RegisterCallback<ClickEvent>(OnPlayerNameClicked);
		}

		protected override void SubscribeToEvents()
		{
			base.SubscribeToEvents();
			_dataProvider.AppDataProvider.DisplayName.InvokeObserve(OnDisplayNameChanged);
			_dataProvider.PlayerDataProvider.Trophies.InvokeObserve(OnTrophiesChanged);
			_dataProvider.ResourceDataProvider.ResourcePools.InvokeObserve(GameId.CS, OnPoolChanged);
			_dataProvider.ResourceDataProvider.ResourcePools.InvokeObserve(GameId.BPP, OnPoolChanged);
			_dataProvider.BattlePassDataProvider.CurrentLevel.InvokeObserve(OnBattlePassCurrentLevelChanged);
			_dataProvider.BattlePassDataProvider.CurrentPoints.InvokeObserve(OnBattlePassCurrentPointsChanged);
			_services.GameModeService.SelectedGameMode.InvokeObserve(OnSelectedGameModeChanged);
			_services.TickService.SubscribeOnUpdate(UpdatePoolLabels, 1);
		}

		protected override void UnsubscribeFromEvents()
		{
			base.UnsubscribeFromEvents();
			_dataProvider.AppDataProvider.DisplayName.StopObserving(OnDisplayNameChanged);
			_dataProvider.PlayerDataProvider.Trophies.StopObserving(OnTrophiesChanged);
			_services.GameModeService.SelectedGameMode.StopObserving(OnSelectedGameModeChanged);
			_dataProvider.CurrencyDataProvider.Currencies.StopObserving(GameId.CS);
			_dataProvider.CurrencyDataProvider.Currencies.StopObserving(GameId.BLST);
			_dataProvider.ResourceDataProvider.ResourcePools.StopObserving(GameId.CS);
			_dataProvider.ResourceDataProvider.ResourcePools.StopObserving(GameId.BPP);
			_dataProvider.BattlePassDataProvider.CurrentLevel.StopObserving(OnBattlePassCurrentLevelChanged);
			_dataProvider.BattlePassDataProvider.CurrentPoints.StopObserving(OnBattlePassCurrentPointsChanged);
			_services.MessageBrokerService.UnsubscribeAll(this);
			_services.TickService.UnsubscribeAll(this);
		}

		private void OnPlayButtonClicked()
		{
			if (!NetworkUtils.CheckAttemptNetworkAction()) return;

			Data.OnPlayButtonClicked();
		}

		private void OnPlayerNameClicked(ClickEvent evt)
		{
			Data.OnNameChangeClicked();
		}

		private void OnTrophiesChanged(uint previous, uint current)
		{
			if (_dataProvider.RewardDataProvider.IsCollecting && current > previous)
			{
				StartCoroutine(AnimateCurrency(GameId.Trophies, previous, current, _playerTrophiesLabel));
			}
			else
			{
				_playerTrophiesLabel.text = current.ToString();
			}
		}

		private void OnDisplayNameChanged(string _, string current)
		{
			_playerNameLabel.text = _dataProvider.AppDataProvider.DisplayNameTrimmed;
		}

		private void OnSelectedGameModeChanged(GameModeInfo _, GameModeInfo current)
		{
			_gameModeLabel.text = current.Entry.GameModeId.ToUpper();
			_gameTypeLabel.text = current.Entry.MatchType.ToString().ToUpper();
			_csPoolContainer.style.display =
				current.Entry.MatchType == MatchType.Casual ? DisplayStyle.None : DisplayStyle.Flex;
		}

		private IEnumerator AnimateCurrency(GameId id, ulong previous, ulong current, Label label)
		{
			label.text = previous.ToString();

			yield return new WaitForSeconds(CURRENCY_ANIM_DELAY);

			for (int i = 0; i < Mathf.Min(10, current - previous); i++)
			{
				_mainMenuServices.UiVfxService.PlayVfx(id,
					i * 0.1f,
					Root.GetPositionOnScreen(Root) + Random.insideUnitCircle * 100,
					label.GetPositionOnScreen(Root),
					() =>
					{
						DOVirtual.Float(previous, current, 0.3f, val => { label.text = val.ToString("F0"); });
						_services.AudioFxService.PlayClip2D(AudioId.CounterTick1);
					});
			}
		}

		private void OnPoolChanged(GameId id, ResourcePoolData previous, ResourcePoolData current,
								   ObservableUpdateType updateType)
		{
			UpdatePoolLabels();
		}

		private void UpdatePoolLabels(float _ = 0)
		{
			UpdatePool(GameId.BPP, BPP_POOL_AMOUNT_FORMAT, _bppPoolTimeLabel, _bppPoolAmountLabel);
			UpdatePool(GameId.CS, CS_POOL_AMOUNT_FORMAT, _csPoolTimeLabel, _csPoolAmountLabel);
		}

		private void UpdatePool(GameId id, string amountStringFormat, Label timeLabel, Label amountLabel)
		{
			var poolInfo = _dataProvider.ResourceDataProvider.GetResourcePoolInfo(id);
			var timeLeft = poolInfo.NextRestockTime - DateTime.UtcNow;

			timeLabel.text = string.Format(ScriptLocalization.UITHomeScreen.resource_pool_restock,
				poolInfo.RestockPerInterval,
				id.ToString(),
				timeLeft.ToHoursMinutesSeconds());

			amountLabel.text = string.Format(amountStringFormat, poolInfo.CurrentAmount, poolInfo.PoolCapacity);
		}

		private void OnBattlePassCurrentLevelChanged(uint _, uint current)
		{
			if (!_dataProvider.RewardDataProvider.IsCollecting)
			{
				UpdateBattlePassLevel(_dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints().Item1);
			}
		}

		private void OnBattlePassCurrentPointsChanged(uint previous, uint current)
		{
			if (_dataProvider.RewardDataProvider.IsCollecting ||
				DebugUtils.DebugFlags.OverrideCurrencyChangedIsCollecting)
			{
				StartCoroutine(AnimateBPP(GameId.BPP, previous, current));
			}
			else
			{
				var predictedLevelAndPoints = _dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints();
				UpdateBattlePassPoints(predictedLevelAndPoints.Item1, predictedLevelAndPoints.Item2);
			}
		}

		private IEnumerator AnimateBPP(GameId id, ulong previous, ulong current)
		{
			yield return new WaitForSeconds(CURRENCY_ANIM_DELAY);

			for (int i = 0; i < (int) (current - previous); i++)
			{
				int points = (int) previous + i + 1;
				var predictedLevelAndPoints = _dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints(points);

				_mainMenuServices.UiVfxService.PlayVfx(id,
					i * 0.1f,
					_playButton.GetPositionOnScreen(Root),
					_battlePassProgressElement.GetPositionOnScreen(Root),
					() =>
					{
						UpdateBattlePassPoints(predictedLevelAndPoints.Item1, predictedLevelAndPoints.Item2, points);
						_services.AudioFxService.PlayClip2D(AudioId.CounterTick1);
					});
			}
		}

		private void UpdateBattlePassLevel(uint predictedLevel)
		{
			var maxLevel = _dataProvider.BattlePassDataProvider.MaxLevel;
			var nextLevel = Math.Clamp(predictedLevel + 1, 0, maxLevel) + 1;
			_battlePassLevelLabel.text = nextLevel.ToString();
		}

		private void UpdateBattlePassPoints(uint predictedLevel, uint predictedPoints, int pointsOverride = -1)
		{
			var battlePassConfig = _services.ConfigsProvider.GetConfig<BattlePassConfig>();
			var hasRewards = _dataProvider.BattlePassDataProvider.IsRedeemable(pointsOverride);
			_battlePassProgressElement.style.flexGrow =
				Mathf.Clamp01((float) predictedPoints / battlePassConfig.PointsPerLevel);
			_battlePassCrownIcon.style.display = hasRewards ? DisplayStyle.Flex : DisplayStyle.None;

			if (predictedLevel == _dataProvider.BattlePassDataProvider.MaxLevel)
			{
				_battlePassProgressElement.style.flexGrow = 1f;
			}

			UpdateBattlePassLevel(predictedLevel);
		}
	}
}