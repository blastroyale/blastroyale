using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Services.Party;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.UITK;
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
	public partial class HomeScreenPresenter : UiToolkitPresenterData<HomeScreenPresenter.StateData>
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
			public Action OnProfileClicked;
			public Action OnGameModeClicked;
			public Action OnLeaderboardClicked;
			public Action OnBattlePassClicked;
			public Action OnStoreClicked;
			public Action OnDiscordClicked;
		}

		private IGameDataProvider _dataProvider;
		private IGameServices _services;
		private IMainMenuServices _mainMenuServices;

		private IPartyService _partyService;

		private LocalizedButton _playButton;

		private ImageButton _header;
		private Label _playerNameLabel;
		private Label _playerTrophiesLabel;

		private VisualElement _equipmentNotification;

		private ImageButton _gameModeButton;
		private Label _gameModeLabel;
		private Label _gameTypeLabel;

		private Label _csAmountLabel;
		private Label _blstAmountLabel;

		private Label _battlePassLevelLabel;
		private LocalizedLabel _battlePassTitle;
		private LocalizedLabel _battlePassTitleClaimReward;
		private VisualElement _battlePassProgressElement;
		private VisualElement _battlePassCrownIcon;
		private VisualElement _battlePassLevelHolder;
		private VisualElement _battlePassProgressBg;
		private ImageButton _battlePassButton;

		private VisualElement _trophiesHolder;

		private VisualElement _bppPoolContainer;
		private Label _bppPoolRestockTimeLabel;
		private Label _bppPoolRestockAmountLabel;
		private Label _bppPoolAmountLabel;
		private VisualElement _csPoolContainer;
		private Label _csPoolRestockTimeLabel;
		private Label _csPoolRestockAmountLabel;
		private Label _csPoolAmountLabel;
		
		private Coroutine _updatePoolsCoroutine;

		private void Awake()
		{
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			_mainMenuServices = MainInstaller.Resolve<IMainMenuServices>();
			_partyService = _services.PartyService;
		}

		protected override void QueryElements(VisualElement root)
		{
			_header = root.Q<ImageButton>("Header").Required();
			_header.clicked += Data.OnProfileClicked;
			_playerNameLabel = _header.Q<Label>("Name").Required();
			_playerTrophiesLabel = _header.Q<Label>("TrophiesAmount").Required();

			_gameModeLabel = root.Q<Label>("GameModeLabel").Required();
			_gameTypeLabel = root.Q<Label>("GameTypeLabel").Required();
			_gameModeButton = root.Q<ImageButton>("GameModeButton").Required();

			_equipmentNotification = root.Q<VisualElement>("EquipmentNotification").Required();

			_trophiesHolder = root.Q<VisualElement>("TrophiesHolder").Required();

			_bppPoolContainer = root.Q<VisualElement>("BPPPoolContainer").Required();
			_bppPoolAmountLabel = _bppPoolContainer.Q<Label>("AmountLabel").Required();
			_bppPoolRestockTimeLabel = _bppPoolContainer.Q<Label>("RestockLabelTime").Required();
			_bppPoolRestockAmountLabel = _bppPoolContainer.Q<Label>("RestockLabelAmount").Required();

			_csPoolContainer = root.Q<VisualElement>("CSPoolContainer").Required();
			_csPoolAmountLabel = _csPoolContainer.Q<Label>("AmountLabel").Required();
			_csPoolRestockTimeLabel = _csPoolContainer.Q<Label>("RestockLabelTime").Required();
			_csPoolRestockAmountLabel = _csPoolContainer.Q<Label>("RestockLabelAmount").Required();

			_battlePassLevelLabel = root.Q<Label>("BattlePassLevelLabel").Required();
			_battlePassProgressElement = root.Q<VisualElement>("BattlePassProgressElement").Required();
			_battlePassCrownIcon = root.Q<VisualElement>("BattlePassCrownIcon").Required();
			_battlePassTitle = root.Q<LocalizedLabel>("BattlePassTitle").Required();
			_battlePassTitleClaimReward = root.Q<LocalizedLabel>("BattlePassTitleClaimReward").Required();
			_battlePassLevelHolder = root.Q<VisualElement>("BattlePassLevelHolder").Required();
			_battlePassProgressBg = root.Q<VisualElement>("BattlePassProgressBg").Required();
			_battlePassButton = root.Q<ImageButton>("BattlePassButton").Required();

			QueryElementsSquads(root);
			
			_playButton = root.Q<LocalizedButton>("PlayButton");
			_playButton.clicked += OnPlayButtonClicked;

			root.Q<CurrencyDisplayElement>("CSCurrency").SetAnimationOrigin(_playButton);
			root.Q<CurrencyDisplayElement>("CoinCurrency").SetAnimationOrigin(_playButton);

			root.Q<ImageButton>("GameModeButton").clicked += Data.OnGameModeClicked;
			root.Q<ImageButton>("SettingsButton").clicked += Data.OnSettingsButtonClicked;
			root.Q<ImageButton>("BattlePassButton").clicked += Data.OnBattlePassClicked;

			root.Q<Button>("EquipmentButton").clicked += Data.OnLootButtonClicked;
			root.Q<Button>("HeroesButton").clicked += Data.OnHeroesButtonClicked;
			root.Q<Button>("LeaderboardsButton").clicked += Data.OnLeaderboardClicked;
			root.Q<Button>("TrophiesHolder").clicked += Data.OnLeaderboardClicked;
			


			var storeButton = root.Q<Button>("StoreButton");
			storeButton.clicked += Data.OnStoreClicked;
			storeButton.SetDisplay(FeatureFlags.STORE_ENABLED);

			var discordButton = root.Q<Button>("DiscordButton");
			discordButton.clicked += () =>
			{
				_services.AnalyticsService.UiCalls.ButtonAction(UIAnalyticsButtonsNames.DiscordLink);
				Data.OnDiscordClicked();
			};

			root.SetupClicks(_services);
			OnAnyPartyUpdate();
		}


		protected override void OnOpened()
		{
			base.OnOpened();
			_equipmentNotification.SetDisplay(_dataProvider.UniqueIdDataProvider.NewIds.Count > 0);
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
			SubscribeToSquadEvents();
			_updatePoolsCoroutine = _services.CoroutineService.StartCoroutine(UpdatePoolLabels());
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
			UnsubscribeFromSquadEvents();
			
			if (_updatePoolsCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_updatePoolsCoroutine);
				_updatePoolsCoroutine = null;
			}
		}

		private void OnPlayButtonClicked()
		{
			if (!NetworkUtils.CheckAttemptNetworkAction()) return;
			Data.OnPlayButtonClicked();
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
			UpdateGameModeButton();
		}

		private IEnumerator AnimateCurrency(GameId id, ulong previous, ulong current, Label label)
		{
			label.text = previous.ToString();

			yield return new WaitForSeconds(CURRENCY_ANIM_DELAY);

			for (int i = 0; i < Mathf.Clamp(current - previous, 1, 20); i++)
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

		private IEnumerator UpdatePoolLabels()
		{
			var waitForSeconds = new WaitForSeconds(GameConstants.Network.NETWORK_ATTEMPT_RECONNECT_SECONDS);

			while (true)
			{
				UpdatePool(GameId.BPP, BPP_POOL_AMOUNT_FORMAT, _bppPoolRestockTimeLabel, _bppPoolRestockAmountLabel,
					_bppPoolAmountLabel);
				UpdatePool(GameId.CS, CS_POOL_AMOUNT_FORMAT, _csPoolRestockTimeLabel, _csPoolRestockAmountLabel,
					_csPoolAmountLabel);

				yield return waitForSeconds;
			}
		}

		private void UpdatePool(GameId id, string amountStringFormat, Label timeLabel, Label restockAmountLabel,
								Label poolAmountLabel)
		{
			var poolInfo = _dataProvider.ResourceDataProvider.GetResourcePoolInfo(id);
			var timeLeft = poolInfo.NextRestockTime - DateTime.UtcNow;

			poolAmountLabel.text = string.Format(amountStringFormat, poolInfo.CurrentAmount, poolInfo.PoolCapacity);

			if (poolInfo.IsFull)
			{
				timeLabel.text = string.Empty;
				restockAmountLabel.text = string.Empty;
			}
			else
			{
				restockAmountLabel.text = $"+ {poolInfo.RestockPerInterval}";
				timeLabel.text = string.Format(
					ScriptLocalization.UITHomeScreen.resource_pool_restock_time,
					timeLeft.ToHoursMinutesSeconds());
			}
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

			var pointsDiff = (int) current - (int) previous;
			var pointsToAnimate = Mathf.Clamp(current - previous, 3, 10) + 1;
			var pointSegment = Mathf.RoundToInt(pointsDiff / pointsToAnimate);

			var pointSegments = new List<int>();

			// Split all points to animate into segments without any precision related errors due to division
			while (pointsDiff > 0)
			{
				var newSegment = pointSegment;

				if (pointSegment > pointsDiff)
				{
					newSegment = pointsDiff;
				}

				pointsDiff -= newSegment;
				pointSegments.Add(newSegment);
			}

			var totalSegmentPointsRedeemed = 0;
			var segmentIndex = 0;

			// Fire point segment VFX and update points
			foreach (var segment in pointSegments)
			{
				totalSegmentPointsRedeemed += segment;
				segmentIndex += 1;

				var points = (int) previous + totalSegmentPointsRedeemed;
				var predictedLevelAndPoints = _dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints(points);

				_mainMenuServices.UiVfxService.PlayVfx(id,
					segmentIndex * 0.1f,
					_playButton.GetPositionOnScreen(Root),
					_battlePassProgressElement.GetPositionOnScreen(Root),
					() =>
					{
						UpdateBattlePassPoints(predictedLevelAndPoints.Item1, predictedLevelAndPoints.Item2, points);
						_services.AudioFxService.PlayClip2D(AudioId.CounterTick1);
					});
			}
		}


		private void UpdateGameModeButton()
		{
			var current = _services.GameModeService.SelectedGameMode.Value.Entry;
			_gameModeLabel.text = LocalizationUtils.GetTranslationForGameModeId(current.GameModeId);
			_gameTypeLabel.text = current.MatchType.ToString().ToUpper();

			_csPoolContainer.SetDisplay(current.MatchType == MatchType.Ranked);

			_gameModeLabel.EnableInClassList("game-mode-button__mode--multiple-line", _gameModeLabel.text.Contains("\\n"));
			_gameTypeLabel.EnableInClassList("game-mode-button__type--ranked", current.MatchType == MatchType.Ranked);

			_gameModeButton.SetEnabled(!_partyService.HasParty.Value && !_partyService.OperationInProgress.Value);
		}

		private void UpdatePlayButton()
		{
			var translationKey = ScriptTerms.UITHomeScreen.play;
			var buttonClass = "play-button";
			var buttonEnabled = true;
			if (_services.PartyService.OperationInProgress.Value)
			{
				buttonEnabled = false;
				translationKey = ScriptTerms.UITShared.waiting;
			}
			else if (_services.PartyService.HasParty.Value && _services.PartyService.GetLocalMember() != null)
			{
				var leader = _services.PartyService.GetLocalMember().Leader;
				if (leader)
				{
					if (!_services.PartyService.PartyReady.Value)
					{
						buttonEnabled = false;
						translationKey = ScriptTerms.UITHomeScreen.waiting_for_members;
					}
					else
					{
						translationKey = ScriptTerms.UITHomeScreen.play;
					}
				}
				else
				{
					buttonClass = "play-button--ready";
					var isReady = _services.PartyService.GetLocalMember()!.Ready;
					translationKey = isReady ? ScriptTerms.UITHomeScreen.cancel : ScriptTerms.UITHomeScreen.ready;
				}
			}

			_playButton.SetEnabled(buttonEnabled);
			_playButton.RemoveModifiers();
			_playButton.EnableInClassList(buttonClass, true);
			_playButton.Localize(translationKey);
		}

		private void UpdateBattlePassLevel(uint predictedLevel)
		{
			var maxLevel = _dataProvider.BattlePassDataProvider.MaxLevel;
			var nextLevel = Math.Clamp(predictedLevel + 1, 0, maxLevel) + 1;
			_battlePassLevelLabel.text = nextLevel.ToString();
		}

		private void UpdateBattlePassPoints(uint predictedLevel, uint predictedPoints, int pointsOverride = -1)
		{
			var hasRewards = _dataProvider.BattlePassDataProvider.IsRedeemable(pointsOverride);
			var currentPointsPerLevel = _dataProvider.BattlePassDataProvider.GetRequiredPointsForLevel((int) predictedLevel);

			_battlePassProgressElement.style.flexGrow =
				Mathf.Clamp01((float) predictedPoints / currentPointsPerLevel);
			_battlePassCrownIcon.visible = hasRewards;
			_battlePassTitleClaimReward.visible = hasRewards;

			if (predictedLevel == _dataProvider.BattlePassDataProvider.MaxLevel)
			{
				_battlePassProgressElement.style.flexGrow = 1f;
			}

			_battlePassProgressElement.visible = !hasRewards;
			_battlePassLevelLabel.visible = !hasRewards;
			_battlePassTitle.visible = !hasRewards;
			_battlePassLevelHolder.visible = !hasRewards;
			_battlePassProgressBg.visible = !hasRewards;

			_battlePassButton.EnableInClassList("battle-pass-button--claimreward", hasRewards);

			UpdateBattlePassLevel(predictedLevel);
		}
	}
}