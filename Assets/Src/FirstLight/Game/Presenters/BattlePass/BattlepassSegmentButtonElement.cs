using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters.BattlePass
{
	/// <summary>
	/// Displays a glint over an element. Must be used with a vector mask for shaped glints.
	/// </summary>
	public class BattlepassSegmentButtonElement : VisualElement
	{
		private const string UssSpriteRarityModifier = "--sprite-home__pattern-rewardglow-";
		private const string UssClaimableButton = "reward__button-claimable";
		private const string UssClaimedButton = "reward__button--claimed";
		private const string UssUnclaimedFree = "reward__button--unclaimed-free";
		private const string UssUnclaimedPaid = "reward__button--unclaimed-paid";
		private const string UssRootModifierSmall = "reward__root--small";

		public event Action<BattlepassSegmentButtonElement> Clicked;
		private VisualElement _rewardRoot;
		private VisualElement _blocker;
		private VisualElement _claimBubble;
		private VisualElement _rewardImage;
		private VisualElement _imageContainer;
		private VisualElement _readyToClaimOutline;
		private VisualElement _claimedCheckmark;
		private AutoSizeLabel _title;
		private AutoSizeLabel _type;
		private ImageButton _button;
		private VisualElement _lock;

		public BattlePassSegmentData SegmentData { get; private set; }
		public bool UnlockedPremium { get; private set; }
		public RewardState RewardState { get; private set; }

		public PassType PassType { get; private set; }

		public BattlepassSegmentButtonElement()
		{
			// TODO: Move to unitask and addressables
			var a = Resources.Load<VisualTreeAsset>("BattlepassSegmentButtonElement");
			a.CloneTree(this);
			var element = this;
			_rewardRoot = element.Q("Reward").Required();
			_blocker = element.Q("Blocker").Required();
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
		}

		/// <summary>
		/// Sets the data needed to fill the segment visuals
		/// </summary>
		public void SetData(BattlePassSegmentData data, RewardState state, bool unlockedPremium, bool small = false)
		{
			SegmentData = data;
			RewardState = state;
			UnlockedPremium = unlockedPremium;
			SetStatusVisuals();
			_rewardRoot.RemoveModifiers();
			if (small)
			{
				_rewardRoot.AddToClassList(UssRootModifierSmall);
			}
		}

		private void SetStatusVisuals()
		{
			DrawIcon();
			_rewardRoot.RemoveModifiers();

			var locked = SegmentData.PassType == PassType.Paid && !UnlockedPremium;
			_lock.SetDisplay(locked);
			_readyToClaimOutline.SetDisplay(RewardState == RewardState.Claimable && !locked);
			_claimBubble.SetDisplay(RewardState == RewardState.Claimable && !locked);
			_blocker.SetDisplay(RewardState == RewardState.Claimed || locked);
			_claimedCheckmark.SetDisplay(RewardState == RewardState.Claimed);
			_button.RemoveModifiers();
			var isClaimable = RewardState == RewardState.Claimable && !locked;
			_readyToClaimOutline.SetDisplay(isClaimable);
			_claimBubble.SetDisplay(isClaimable);
			if (isClaimable)
			{
				_claimBubble.AnimatePing(1.3f, 500, true);
			}

			if (RewardState == RewardState.Claimed) _button.AddToClassList(UssClaimedButton);
			else if (RewardState == RewardState.Claimable) _button.AddToClassList(UssClaimableButton);
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
			_type.text = itemView.ItemTypeDisplayName.ToUpperInvariant();
			// Dirty hack to only show amount of currencies
			if (itemView is CurrencyItemViewModel cv)
			{
				_title.text = cv.Amount.ToString();
				return;
			}

			_title.text = itemView.GameId.IsInGroup(GameIdGroup.ProfilePicture) ? "" : itemView.DisplayName.ToUpperInvariant();
		}

		public new class UxmlFactory : UxmlFactory<BattlepassSegmentButtonElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			private readonly UxmlBoolAttributeDescription _debugMode = new ()
			{
				name = "debug-mode",
				defaultValue = false,
			};

			private readonly UxmlEnumAttributeDescription<RewardState> _state = new ()
			{
				name = "state",
				defaultValue = RewardState.Claimable,
			};

			private readonly UxmlBoolAttributeDescription _unlockedPremium = new ()
			{
				name = "unlocked-premium",
				defaultValue = false,
			};

			private readonly UxmlEnumAttributeDescription<GameId> _rewardId = new ()
			{
				name = "reward",
				defaultValue = GameId.MaleAssassin,
			};

			private readonly UxmlEnumAttributeDescription<PassType> _passType = new ()
			{
				name = "pass-type",
				defaultValue = PassType.Free,
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				var el = (BattlepassSegmentButtonElement) ve;
				el.RewardState = _state.GetValueFromBag(bag, cc);
				el.UnlockedPremium = _unlockedPremium.GetValueFromBag(bag, cc);
				if (_debugMode.GetValueFromBag(bag, cc))
					el.SetData(new BattlePassSegmentData()
					{
						PassType = _passType.GetValueFromBag(bag, cc),
						RewardConfig = new EquipmentRewardConfig()
						{
							GameId = _rewardId.GetValueFromBag(bag, cc),
							Amount = 1,
						},
						SegmentLevel = 1,
						LevelAfterClaiming = 2,
						PointsAfterClaiming = 2,
					}, RewardState.Claimable, false);
			}
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

	public enum RewardState
	{
		Claimed,
		Claimable,
		NotReached
	}
}