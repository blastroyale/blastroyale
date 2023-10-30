using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
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
		private VisualElement _claimedOutline;
		private VisualElement _readyToClaimShine;
		private VisualElement _readyToClaimOutline;
		private VisualElement _progressBarFill;
		private VisualElement _progressBackground;
		private VisualElement _claimedCheckmark;
		private VisualElement _claimableBg;
		private AutoSizeLabel _title;
		private AutoSizeLabel _levelNumber;
		private ImageButton _button;
		
		public BattlePassSegmentData SegmentData { get; private set; }
		private IGameDataProvider _dataProvider;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_rewardRoot = element.Q("Reward").Required();
			_progressBackground = element.Q("ProgressBackground").Required();
			_progressBarFill = element.Q("ProgressFill").Required();
			_blocker = element.Q("Blocker").Required();
			_claimedOutline = element.Q("Outline").Required();
			_readyToClaimShine = element.Q("ReadyToClaimShine").Required();
			_readyToClaimOutline = element.Q("ReadyToClaimOutline").Required();
			_claimBubble = element.Q("ClaimBubble").Required();
			_claimedCheckmark = element.Q("Checkmark").Required();
			_claimableBg = element.Q("LevelBg").Required();
			_button = element.Q<ImageButton>("Button").Required();
			_rarityImage = element.Q("RewardRarity").Required();
			_rewardImage = element.Q("RewardImage").Required();
			_title = element.Q<AutoSizeLabel>("Title");
			_levelNumber = element.Q<AutoSizeLabel>("LevelLabel");
			_imageContainer = _rewardImage.parent;
			_button.clicked += () => Clicked?.Invoke(this);
			_dataProvider = MainInstaller.ResolveData();
		}

		private bool Claimable => SegmentData.RewardConfig.GameId != GameId.Random && _dataProvider.BattlePassDataProvider.IsRewardClaimable(
			SegmentData.LevelAfterClaiming, SegmentData.LevelNeededToClaim, SegmentData.PassType);
		
		private bool Claimed => _dataProvider.BattlePassDataProvider.IsRewardClaimed(
			SegmentData.LevelNeededToClaim, SegmentData.PassType);

		private uint PointsRequired => _dataProvider.BattlePassDataProvider.GetRequiredPointsForLevel((int) SegmentData.SegmentLevel);
		
		/// <summary>
		/// Sets the data needed to fill the segment visuals
		/// </summary>
		public void InitWithData(BattlePassSegmentData data)
		{
			SegmentData = data;
			DrawIcon();
			SetStatusVisuals();
			DrawProgressBar();
			// Used for tutorial targeting
			if (data.SegmentLevel == 0) _rewardRoot.AddToClassList(UssFirstReward);
		}

		private void DrawProgressBar()
		{
			if (SegmentData.LevelAfterClaiming > SegmentData.SegmentLevel) SetProgressFill(1f);
			else if (SegmentData.LevelAfterClaiming == SegmentData.SegmentLevel) SetProgressFill((float) SegmentData.PointsAfterClaiming / PointsRequired);
			else SetProgressFill(0);
		}

		private void SetStatusVisuals()
		{
			_claimableBg.EnableInClassList(UssLevelBgComplete, Claimable);
			_claimedOutline.EnableInClassList(UssOutlineClaimed, Claimed);
			_claimedCheckmark.SetDisplay(Claimed);
			_readyToClaimOutline.SetDisplay(!Claimed && Claimable);
			_readyToClaimShine.SetDisplay(!Claimed && Claimable);
			_blocker.SetDisplay(Claimed || SegmentData.LevelAfterClaiming != SegmentData.LevelNeededToClaim);
			_claimBubble.SetDisplay(!Claimed && Claimable && SegmentData.LevelAfterClaiming == SegmentData.LevelNeededToClaim);
		}

		private void DrawIcon()
		{
			_levelNumber.text = (SegmentData.LevelNeededToClaim + 1).ToString();
			var reward = SegmentData.RewardConfig;
			if (reward.GameId == GameId.Random)
			{
				return;
			}
			var item = ItemFactory.Legacy(new LegacyItemData()
			{
				RewardId = reward.GameId,
				Value = reward.Amount
			});
			var itemView = item.GetViewModel();
			itemView.DrawIcon(_rewardImage);
			_title.text = itemView.DisplayName;
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
		public uint SegmentLevel;
		public uint LevelAfterClaiming;
		public uint PointsAfterClaiming;
		public EquipmentRewardConfig RewardConfig;
		public PassType PassType;
		public uint LevelNeededToClaim => SegmentLevel + 1;

	}
}