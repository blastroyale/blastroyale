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
		private const string UssSpriteRarityModifier = "--sprite-home__pattern-rewardglow-";
		private const string UssSpriteRarityCommon = UssSpriteRarityModifier + "common";
		private const string UssSpriteRarityUncommon = UssSpriteRarityModifier + "uncommon";
		private const string UssSpriteRarityRare = UssSpriteRarityModifier + "rare";
		private const string UssSpriteRarityEpic = UssSpriteRarityModifier + "epic";
		private const string UssSpriteRarityLegendary = UssSpriteRarityModifier + "legendary";
		private const string UssSpriteRarityRainbow = UssSpriteRarityModifier + "rainbow";

		private const string UssOutlineClaimed = "reward__button-outline--claimed";
		private const string UssLevelBgComplete = "progress-bar__level-bg--complete";
		public event Action<BattlePassSegmentView> Clicked;

		private VisualElement _root;
		private VisualElement _rewardRoot;
		private VisualElement _blocker;
		private VisualElement _claimBubble;
		private VisualElement _rarityImage;
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

		public void Attached(VisualElement element)
		{
			_root = element;
			_rewardRoot = _root.Q("Reward").Required();
			_progressBackground = _root.Q("ProgressBackground").Required();
			_progressBarFill = _root.Q("ProgressFill").Required();
			_blocker = _root.Q("Blocker").Required();
			_claimStatusOutline = _root.Q("Outline").Required();
			_readyToClaimShine = _root.Q("ReadyToClaimShine").Required();
			_readyToClaimOutline = _root.Q("ReadyToClaimOutline").Required();
			_claimBubble = _root.Q("ClaimBubble").Required();
			_claimStatusCheckmark = _root.Q("Checkmark").Required();
			_levelBg = _root.Q("LevelBg").Required();
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
		public void InitWithData(BattlePassSegmentData data)
		{
			_data = data;

			var levelForUi = _data.SegmentLevelForRewards + 1;
			var isRewardClaimed = _data.CurrentLevel >= data.SegmentLevelForRewards;

			_title.text = GetRewardName(_data.RewardConfig.GameId);
			_levelNumber.text = levelForUi.ToString();

			_rarityImage.RemoveSpriteClasses();
			var rarityStyle = GetRarityStyle(_data.RewardConfig.GameId);
			if (!_rarityImage.ClassListContains(rarityStyle))
			{
				_rarityImage.AddToClassList(rarityStyle);
			}

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
				SetProgressFill((float) data.PredictedCurrentProgress / data.MaxProgress);
			}
			else
			{
				SetProgressFill(0);
			}
		}

		private void SetProgressFill(float percent)
		{
			_progressBarFill.style.flexGrow = percent;
		}

		private string GetRarityStyle(GameId id)
		{
			return id switch
			{
				GameId.CoreCommon    => UssSpriteRarityCommon,
				GameId.CoreUncommon  => UssSpriteRarityUncommon,
				GameId.CoreRare      => UssSpriteRarityRare,
				GameId.CoreEpic      => UssSpriteRarityEpic,
				GameId.CoreLegendary => UssSpriteRarityLegendary,
				_                    => UssSpriteRarityRainbow
			};
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