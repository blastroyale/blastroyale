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
	public enum RewardState
	{
		Claimed, Claimable, NotReached
	}
	
	/// <summary>
	/// This class manages the visual elements of battle pass segments on the battle pass screen
	/// </summary>
	public class BattlePassSegmentView : UIView
	{
		private const string UssSpriteRarityModifier = "--sprite-home__pattern-rewardglow-";
		private const string UssClaimableButton = "reward__button-claimable";
		private const string UssUnclaimedFree = "reward__button--unclaimed-free";
		private const string UssUnclaimedPaid = "reward__button--unclaimed-paid";
		public event Action<BattlePassSegmentView> Clicked;
		private VisualElement _rewardRoot;
		private VisualElement _blocker;
		private VisualElement _claimBubble;
		private VisualElement _rewardImage;
		private VisualElement _imageContainer;
		private VisualElement _readyToClaimShine;
		private VisualElement _readyToClaimOutline;
		private VisualElement _claimedCheckmark;
		private AutoSizeLabel _title;
		private AutoSizeLabel _type;
		private ImageButton _button;
		private VisualElement _lock;
		
		public BattlePassSegmentData SegmentData { get; private set; }
		private IGameDataProvider _dataProvider;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_rewardRoot = element.Q("Reward").Required();
			_blocker = element.Q("Blocker").Required();
			_readyToClaimShine = element.Q("ReadyToClaimShine").Required();
			_readyToClaimOutline = element.Q("ReadyToClaimOutline").Required();
			_claimBubble = element.Q("ClaimBubble").Required();
			_claimedCheckmark = element.Q("Checkmark").Required();
			_button = element.Q<ImageButton>("Button").Required();
			_rewardImage = element.Q("RewardImage").Required();
			_title = element.Q<AutoSizeLabel>("Title").Required();
			_type = element.Q<AutoSizeLabel>("Type").Required();
			_lock = element.Q("Lock").Required();
			_imageContainer = _rewardImage.parent;
			_button.clicked += () => Clicked?.Invoke(this);
			_dataProvider = MainInstaller.ResolveData();
		}
		
		private uint PointsRequired => _dataProvider.BattlePassDataProvider.GetRequiredPointsForLevel((int) SegmentData.SegmentLevel);

		public RewardState GetRewardState()
		{
			if (_dataProvider.BattlePassDataProvider.IsRewardClaimed(
					SegmentData.LevelNeededToClaim, SegmentData.PassType)) 
				return RewardState.Claimed;
			
			if (SegmentData.RewardConfig.GameId != GameId.Random && _dataProvider.BattlePassDataProvider.IsRewardClaimable(
					SegmentData.LevelAfterClaiming, SegmentData.LevelNeededToClaim, SegmentData.PassType)) 
				return RewardState.Claimable;
			
			return RewardState.NotReached;
		}
		
		/// <summary>
		/// Sets the data needed to fill the segment visuals
		/// </summary>
		public void SetData(BattlePassSegmentData data)
		{
			SegmentData = data;
			DrawIcon();
			SetStatusVisuals();
		}

		private void SetStatusVisuals()
		{
			var state = GetRewardState();
			var locked = SegmentData.PassType == PassType.Paid && !_dataProvider.BattlePassDataProvider.HasPurchasedSeason();
			_lock.SetDisplay(locked);
			_readyToClaimOutline.SetDisplay(state == RewardState.Claimable && !locked);
			_readyToClaimShine.SetDisplay(state == RewardState.Claimable && !locked);
			_claimBubble.SetDisplay(state == RewardState.Claimable && !locked);
			_blocker.SetDisplay(state == RewardState.Claimed || locked);
			_claimedCheckmark.SetDisplay(state == RewardState.Claimed);
			if(state == RewardState.Claimable) 	_button.AddToClassList(UssClaimableButton);
			else if (SegmentData.PassType == PassType.Free) _button.AddToClassList(UssUnclaimedFree);
			else if (SegmentData.PassType == PassType.Paid) _button.AddToClassList(UssUnclaimedPaid);
		}

		private void DrawIcon()
		{
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
			_type.text = itemView.ItemTypeDisplayName;
		}
	}

	/// <summary>
	/// This class holds the data used to update BattlePassSegmentViews
	/// </summary>
	public class BattlePassSegmentData
	{
		public uint SegmentLevel;
		public uint LevelAfterClaiming;
		public uint PointsAfterClaiming;
		public EquipmentRewardConfig RewardConfig;
		public PassType PassType;
		public uint LevelNeededToClaim => SegmentLevel + 1;

	}
}