using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Presenters;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// This class manages the visual elements of battle pass segments on the battle pass screen
	/// </summary>
	public class BattlePassSegmentView : IUIView
	{
		private const string UssRewardHolderRarity = "reward-holder__rarity";
		private const string UssRewardButtonOutline = "reward__button-outline";
		private const string UssRarityCommon = UssRewardHolderRarity + "--common";
		private const string UssRarityUncommon = UssRewardHolderRarity + "--uncommon";
		private const string UssRarityRare = UssRewardHolderRarity + "--rare";
		private const string UssRarityEpic = UssRewardHolderRarity + "--epic";
		private const string UssRarityLegendary = UssRewardHolderRarity + "--legendary";
		private const string UssRarityRainbow = UssRewardHolderRarity + "--rainbow";
		private const string UssOutlineClaimed = UssRewardButtonOutline + "--claimed";
		private const string UssOutlineNotClaimed = UssRewardButtonOutline + "--not-claimed";

		public event Action<BattlePassSegmentView> Clicked;

		private VisualElement _root;
		private VisualElement _rewardRoot;
		private VisualElement _blocker;
		private VisualElement _claimBubble;
		private VisualElement _rarityImage;
		private VisualElement _claimStatusOutline;
		private VisualElement _readyToClaimOutline;
		private VisualElement _progressBarFill;
		private VisualElement _progressBackground;
		private VisualElement _checkmark;
		private AutoSizeLabel _title;
		private AutoSizeLabel _levelNumber;
		private ImageButton _button;

		private BattlePassSegmentData _data;

		public void Attached(VisualElement element)
		{
			_root = element;
			_rewardRoot = _root.Q("Reward").Required();
			_progressBackground = _root.Q("ProgressBackground").Required();
			_progressBarFill = _root.Q("ProgressFill").Required();
			_blocker = _root.Q("Blocker").Required();
			_claimStatusOutline = _root.Q("Outline").Required();
			_readyToClaimOutline = _root.Q("ReadyToClaim").Required();
			_claimBubble = _root.Q("ClaimBubble").Required();
			_checkmark = _root.Q("Checkmark").Required();
			_button = _root.Q<ImageButton>("Button").Required();
			_rarityImage = _root.Q("RewardRarity").Required();
			_title = _root.Q<AutoSizeLabel>("Title");
			_levelNumber = _root.Q<AutoSizeLabel>("LevelLabel");

			_button.clicked += () => Clicked?.Invoke(this);
		}

		public void SubscribeToEvents()
		{
		}

		public void UnsubscribeFromEvents()
		{
		}

		/// <summary>
		/// Sets the data needed to fill the segment visuals
		/// </summary>
		public async void InitWithData(BattlePassSegmentData data)
		{
			_data = data;
			var levelForUi = _data.SegmentLevelForRewards + 1;
			var isRewardClaimed = _data.CurrentLevel >= data.SegmentLevelForRewards;

			_title.text = GetRewardName(_data.RewardConfig.GameId);
			_levelNumber.text = levelForUi.ToString();

			_rarityImage.AddToClassList(GetRarityStyle(_data.RewardConfig.GameId));

			_claimStatusOutline.AddToClassList(isRewardClaimed ? UssOutlineClaimed : UssOutlineNotClaimed);
			_readyToClaimOutline.SetDisplayActive(_data.PredictedCurrentLevel == _data.SegmentLevelForRewards);

			_progressBackground.SetDisplayActive(_data.SegmentLevelForRewards != _data.MaxLevel);
			_progressBarFill.SetDisplayActive(_data.SegmentLevelForRewards != _data.MaxLevel);

			_claimBubble.SetDisplayActive(!isRewardClaimed && _data.SegmentLevelForRewards == _data.PredictedCurrentLevel);
			
			_checkmark.SetDisplayActive(isRewardClaimed);
			
			SetProgressFill(_data.PredictedCurrentLevel > _data.SegmentLevelForRewards
				? 1f
				: (float) _data.PredictedCurrentProgress / data.MaxProgress);
		}

		/// <summary>
		/// Sets the data needed to fill the segment visuals
		/// </summary>
		public void InitMinimalWithData(uint segmentLevel, uint predictedCurrentLevel, uint predictedCurrentProgress, uint maxProgress)
		{
			_levelNumber.text = (segmentLevel + 1).ToString();
			_rewardRoot.SetVisible(false);

			SetProgressFill(predictedCurrentLevel > segmentLevel ? 1f : (float) predictedCurrentProgress / maxProgress);
		}

		private void SetProgressFill(float percent)
		{
			var barWidth = _progressBackground.contentRect.width;
			var contentRect = _progressBarFill.contentRect;
			contentRect.width = barWidth * percent;
		}

		private string GetRarityStyle(GameId id)
		{
			switch (id)
			{
				case GameId.CoreCommon:
					return UssRarityCommon;

				case GameId.CoreUncommon:
					return UssRarityUncommon;

				case GameId.CoreRare:
					return UssRarityRare;

				case GameId.CoreEpic:
					return UssRarityEpic;

				case GameId.CoreLegendary:
					return UssRarityLegendary;

				default:
					return UssRarityRainbow;
			}
		}

		private string GetRewardName(GameId id)
		{
			switch (id)
			{
				case GameId.CoreCommon:
				case GameId.CoreUncommon:
				case GameId.CoreRare:
				case GameId.CoreEpic:
				case GameId.CoreLegendary:
					return ScriptLocalization.UITBattlePass.random_equipment;

				default:
					return id.GetTranslation();
			}
		}
	}

	/// <summary>
	/// This class holds the data used to update BattlePassSegmentViews
	/// </summary>
	public struct BattlePassSegmentData
	{
		public uint SegmentLevel;
		public uint CurrentLevel;
		public uint CurrentProgress;
		public uint PredictedCurrentLevel;
		public uint PredictedCurrentProgress;
		public uint MaxProgress;
		public uint MaxLevel;
		public EquipmentRewardConfig RewardConfig;

		public uint SegmentLevelForRewards => SegmentLevel + 1;
	}
}