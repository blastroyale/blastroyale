using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.MainMenuViews
{
	public class BattlePassButtonView : UIView
	{
		private const string BPP_POOL_AMOUNT_FORMAT = "<color=#49D4D4>{0}</color> / {1}";

		private VisualElement _bppPoolContainer;
		private Label _bppPoolAmountLabel;
		private Label _bppPoolRestockTimeLabel;
		private Label _bppPoolRestockAmountLabel;
		private ImageButton _battlePassButton;
		private VisualElement _battlePassProgressElement;
		private Label _battlePassProgressLabel;
		private VisualElement _battlePassRarity;
		private VisualElement _animationSource;
		private Label _battlePassNextLevelLabel;

		public Action Clicked;
		private IGameDataProvider _dataProvider;
		private IGameServices _services;
		private bool _animation;
		private bool _initialized;

		protected override void Attached()
		{
			_dataProvider = MainInstaller.ResolveData();
			_services = MainInstaller.ResolveServices();

			_animationSource = Presenter.Root.Q<VisualElement>("PlayButton");

			_bppPoolContainer = Element.Q<VisualElement>("BPPPoolContainer").Required();
			_bppPoolAmountLabel = _bppPoolContainer.Q<Label>("AmountLabel").Required();
			_bppPoolRestockTimeLabel = _bppPoolContainer.Q<Label>("RestockLabelTime").Required();
			_bppPoolRestockAmountLabel = _bppPoolContainer.Q<Label>("RestockLabelAmount").Required();

			_battlePassButton = Element.Q<ImageButton>("BattlePassButton").Required();
			_battlePassProgressElement = _battlePassButton.Q<VisualElement>("BattlePassProgressElement").Required();
			_battlePassProgressLabel = _battlePassButton.Q<Label>("BPProgressText").Required();
			_battlePassRarity = _battlePassButton.Q<VisualElement>("BPRarity").Required();
			_battlePassNextLevelLabel = _battlePassButton.Q<Label>("BarLevelLabel").Required();
			_battlePassButton.clicked += () => Clicked?.Invoke();
		}

		public override void OnScreenOpen(bool reload)
		{
			_dataProvider.BattlePassDataProvider.CurrentPoints.InvokeObserve(OnBattlePassCurrentPointsChanged);
			_dataProvider.ResourceDataProvider.ResourcePools.InvokeObserve(GameId.BPP, OnPoolChanged);
			UpdatePoolLabels(Presenter.GetCancellationTokenOnClose()).Forget();
		}

		public override void OnScreenClose()
		{
			_dataProvider.ResourceDataProvider.ResourcePools.StopObserving(GameId.BPP);
			_dataProvider.BattlePassDataProvider.CurrentPoints.StopObserving(OnBattlePassCurrentPointsChanged);
		}

		private void OnPoolChanged(GameId id, ResourcePoolData previous, ResourcePoolData current,
								   ObservableUpdateType updateType)
		{
			UpdatePool();
		}

		private async UniTaskVoid UpdatePoolLabels(CancellationToken tok)
		{
			while (true)
			{
				await UniTask.WaitForSeconds(GameConstants.Visuals.RESOURCE_POOL_UPDATE_TIME_SECONDS, cancellationToken: tok);
				UpdatePool();
			}
		}

		private void UpdatePool()
		{
			var poolInfo = _dataProvider.ResourceDataProvider.GetResourcePoolInfo(GameId.BPP);
			var timeLeft = poolInfo.NextRestockTime - DateTime.UtcNow;

			_bppPoolAmountLabel.text = string.Format(BPP_POOL_AMOUNT_FORMAT, poolInfo.CurrentAmount, poolInfo.PoolCapacity);

			if (poolInfo.IsFull)
			{
				_bppPoolRestockTimeLabel.text = string.Empty;
				_bppPoolRestockAmountLabel.text = string.Empty;
			}
			else
			{
				_bppPoolRestockAmountLabel.text = $"+ {poolInfo.RestockPerInterval}";
				_bppPoolRestockTimeLabel.text = string.Format(
					ScriptLocalization.UITHomeScreen.resource_pool_restock_time,
					timeLeft.ToHoursMinutesSeconds());
			}
		}

		private void OnBattlePassCurrentPointsChanged(uint previous, uint current)
		{
			UpdateBattlePassReward();

			if (current > previous && _initialized && !_animation)
			{
				Presenter.StartCoroutine(AnimateBPP(GameId.BPP, previous, current));
			}
			else
			{
				_initialized = true;
				UpdateBattlePassPoints((int) current);
			}
		}

		private IEnumerator AnimateBPP(GameId id, ulong previous, ulong current)
		{
			_animation = true;
			// Apparently this initial delay is a must, otherwise "GetPositionOnScreen" starts throwing "Element out of bounds" exception OCCASIONALLY
			// I guess it depends on how long the transition to home screen take; so these errors still may appear
			yield return new WaitForSeconds(0.4f);

			var pointsDiff = (int) current - (int) previous;
			var pointsToAnimate = Mathf.Clamp((current - previous) / 10, 3, 10);
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
				var wasRedeemable = _dataProvider.BattlePassDataProvider.HasUnclaimedRewards((int) previous);

				var root = Presenter.Root;
				_services.UIVFXService.PlayVfx(id,
					segmentIndex * 0.05f,
					_animationSource.GetPositionOnScreen(root),
					_battlePassProgressElement.GetPositionOnScreen(root),
					() =>
					{
						_services.AudioFxService.PlayClip2D(AudioId.CounterTick1);

						if (wasRedeemable) return;

						UpdateBattlePassPoints(points);
					});
			}

			_animation = false;
		}

		private void UpdateBattlePassPoints(int points)
		{
			var hasRewards = _dataProvider.BattlePassDataProvider.HasUnclaimedRewards(points);
			_battlePassButton.EnableInClassList("battle-pass-button--claimreward", hasRewards);

			if (!hasRewards)
			{
				if (_dataProvider.BattlePassDataProvider.CurrentLevel.Value ==
					_dataProvider.BattlePassDataProvider.MaxLevel)
				{
					_battlePassButton.EnableInClassList("battle-pass-button--completed", true);
					_bppPoolContainer.SetDisplay(false);
				}
				else
				{
					var predictedLevelAndPoints =
						_dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints(points);
					_battlePassNextLevelLabel.text = (predictedLevelAndPoints.Item1 + 1).ToString();
					var requiredPoints =
						_dataProvider.BattlePassDataProvider.GetRequiredPointsForLevel(
							(int) predictedLevelAndPoints.Item1);

					_battlePassProgressElement.style.width = Length.Percent((float) points / requiredPoints * 100);
					_battlePassProgressLabel.text = $"{points} / {requiredPoints}";
				}
			}
		}

		private void UpdateBattlePassReward()
		{
			var nextLevel = _dataProvider.BattlePassDataProvider.CurrentLevel.Value + 1;
			_battlePassRarity.RemoveSpriteClasses();

			if (nextLevel <= _dataProvider.BattlePassDataProvider.MaxLevel)
			{
				var reward = _dataProvider.BattlePassDataProvider.GetRewardForLevel(nextLevel, PassType.Free);
				_battlePassRarity.AddToClassList(UIUtils.GetBPRarityStyle(reward.GameId));
			}
		}
	}
}