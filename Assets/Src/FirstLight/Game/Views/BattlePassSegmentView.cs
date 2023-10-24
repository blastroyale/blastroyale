using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
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
	public class BattlePassSegmentView : UIView
	{
		private const string UssSpriteRarityModifier = "--sprite-home__pattern-rewardglow-";
		private const string UssSpriteRarityCommon = UssSpriteRarityModifier + "common";
		private const string UssSpriteRarityUncommon = UssSpriteRarityModifier + "uncommon";
		private const string UssSpriteRarityRare = UssSpriteRarityModifier + "rare";
		private const string UssSpriteRarityEpic = UssSpriteRarityModifier + "epic";
		private const string UssSpriteRarityLegendary = UssSpriteRarityModifier + "legendary";
		private const string UssSpriteRarityRainbow = UssSpriteRarityModifier + "rainbow";
		private const string UssBorderRadius = "circle-border-radius";
		private const string UssOutlineClaimed = "reward__button-outline--claimed";
		private const string UssLevelBgComplete = "progress-bar__level-bg--complete";
		private const string UssFirstReward = "first-reward";
		public event Action<BattlePassSegmentView> Clicked;

		private VisualElement _rewardRoot;
		private VisualElement _blocker;
		private VisualElement _claimBubble;
		private VisualElement _rarityImage;
		private VisualElement _rewardImage;
		private VisualElement _imageContainer;
		private VisualElement _claimStatusOutline;
		private VisualElement _readyToClaimShine;
		private VisualElement _readyToClaimOutline;
		private VisualElement _progressBarFill;
		private VisualElement _progressBackground;
		private VisualElement _claimStatusCheckmark;
		private VisualElement _levelBg;
		private AutoSizeLabel _title;
		private AutoSizeLabel _levelNumber;
		private ImageButton _button;

		private BattlePassSegmentData _data;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_rewardRoot = element.Q("Reward").Required();
			_progressBackground = element.Q("ProgressBackground").Required();
			_progressBarFill = element.Q("ProgressFill").Required();
			_blocker = element.Q("Blocker").Required();
			_claimStatusOutline = element.Q("Outline").Required();
			_readyToClaimShine = element.Q("ReadyToClaimShine").Required();
			_readyToClaimOutline = element.Q("ReadyToClaimOutline").Required();
			_claimBubble = element.Q("ClaimBubble").Required();
			_claimStatusCheckmark = element.Q("Checkmark").Required();
			_levelBg = element.Q("LevelBg").Required();
			_button = element.Q<ImageButton>("Button").Required();
			_rarityImage = element.Q("RewardRarity").Required();
			_rewardImage = element.Q("RewardImage").Required();
			_title = element.Q<AutoSizeLabel>("Title");
			_levelNumber = element.Q<AutoSizeLabel>("LevelLabel");
			_imageContainer = _rewardImage.parent;
			_button.clicked += () => Clicked?.Invoke(this);
		}

		/// <summary>
		/// Sets the data needed to fill the segment visuals
		/// </summary>
		public void InitWithData(BattlePassSegmentData data)
		{
			_data = data;
			var isRewardClaimed = _data.PlayerCurrentLevel >= data.SegmentLevelForRewards;
			var reward = data.RewardConfig;
			var item = ItemFactory.Legacy(new LegacyItemData()
			{
				RewardId = reward.GameId,
				Value = reward.Amount
			});
			var itemView = item.GetViewModel();
			itemView.DrawIcon(_rewardImage);
			_title.text = itemView.DisplayName;
			_levelNumber.text = (_data.SegmentLevelForRewards + 1).ToString();

			_levelBg.EnableInClassList(UssLevelBgComplete, data.PredictedCurrentLevel >= data.SegmentLevelForRewards);
			_claimStatusOutline.EnableInClassList(UssOutlineClaimed, isRewardClaimed);
			_claimStatusCheckmark.SetDisplay(isRewardClaimed);
			_readyToClaimOutline.SetDisplay(!isRewardClaimed &&
				_data.PredictedCurrentLevel >= _data.SegmentLevelForRewards);
			_readyToClaimShine.SetDisplay(!isRewardClaimed &&
				_data.PredictedCurrentLevel >= _data.SegmentLevelForRewards);

			_blocker.SetDisplay(isRewardClaimed || _data.PredictedCurrentLevel != _data.SegmentLevelForRewards);
			_claimBubble.SetDisplay(!isRewardClaimed && _data.PredictedCurrentLevel == _data.SegmentLevelForRewards);

			if (data.PredictedCurrentLevel > data.SegmentLevel)
			{
				SetProgressFill(1f);
			}
			else if (data.PredictedCurrentLevel == data.SegmentLevel)
			{
				SetProgressFill((float) data.PredictedCurrentPoints / data.PointsToLevel);
			}
			else
			{
				SetProgressFill(0);
			}

			// Used for tutorial targeting
			if (data.SegmentLevel == 0)
			{
				_rewardRoot.AddToClassList(UssFirstReward);
			}
		}

		private void SetProgressFill(float percent)
		{
			_progressBarFill.style.flexGrow = percent;
		}
	}

	/// <summary>
	/// This class holds the data used to update BattlePassSegmentViews
	/// </summary>
	public struct BattlePassSegmentData
	{
		/// <summary>
		/// Current index of the segment in the segment list
		/// </summary>
		public uint SegmentLevel;
		
		/// <summary>
		/// Current level 
		/// </summary>
		public uint PlayerCurrentLevel;
		
		/// <summary>
		/// The predicted level is what would be the new level after battle pass points were redeemed
		/// </summary>
		public uint PredictedCurrentLevel;
		
		/// <summary>
		/// Same as predicted current level but 
		/// </summary>
		public uint PredictedCurrentPoints;
		
		/// <summary>
		/// Max points for this whole level
		/// </summary>
		public uint PointsToLevel;
		
		/// <summary>
		/// Reward config for this segment
		/// </summary>
		public EquipmentRewardConfig RewardConfig;

		/// <summary>
		/// Level needed to be to be able to claim the rewards
		/// </summary>
		public uint SegmentLevelForRewards => SegmentLevel + 1;
	}
}