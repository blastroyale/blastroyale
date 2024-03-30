using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.UIService;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Handles the summary part of the rewards screen
	/// </summary>
	public class RewardsSummaryView : UIView2
	{
		private const float SKIP_ANIMATION_TIME = 0.2f;
		private const float FAME_MIDDLE_SKIP_TIME = 4.7f;
		private const float FAME_START_OFFSET = 1.7f;
		private const int MANY_REWARDS_AMOUNT = 5;
		private const string USS_REWARD_SUMMARY_CONTAINER_MANY_REWARDS_MODIFIER = "rewards-summary__rewards-container--manyrewards";
		private const string USS_FAME_REWARDS_SUMMARY = "rewards-summary--fame";

		private IGameDataProvider _dataProvider;

		private RewardsAnimationController _animationController;
		private AnimatedBackground _animatedBackground;
		private PlayableDirector _animationDirector;
		private bool _isFame;
		private AnimatedBackground.AnimatedBackgroundColor _summaryBgColor;

		private VisualElement _container;
		private PlayerAvatarElement _avatar;
		private Label _reachLevelLabel;

		public void Init(RewardsAnimationController animationController, AnimatedBackground animatedBackground, PlayableDirector animationDirector,
						 bool isFame, AnimatedBackground.AnimatedBackgroundColor summaryBgColor)
		{
			_animationController = animationController;
			_animatedBackground = animatedBackground;
			_animationDirector = animationDirector;
			_isFame = isFame;
			_summaryBgColor = summaryBgColor;
		}

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();

			_container = element.Q<VisualElement>("RewardsContainer").Required();
			_avatar = element.Q<PlayerAvatarElement>("Avatar").Required();
			_reachLevelLabel = Element.Q<Label>("ReachLevelToGetRewards").Required();

			SetupAvatarAndLevels();
		}

		private void SetupAvatarAndLevels()
		{
			var currentLevel = _dataProvider.PlayerDataProvider.Level.Value;
			_reachLevelLabel.text = string.Format("REACH LEVEL <color=#f8c72e>{0}</color> TO GET NEXT REWARDS",
				currentLevel + 1);
			_avatar.SetLevel(currentLevel);
			_avatar.SetAvatar(_dataProvider.AppDataProvider.AvatarUrl);
		}


		public void CreateSummaryElements(IEnumerable<ItemData> items, bool fameRewards)
		{
			// Clean up example elements
			for (var i = _container.childCount - 1; i >= 0; i--)
			{
				_container.RemoveAt(i);
			}

			foreach (var item in items)
			{
				var view = item.GetViewModel();
				_container.Add(view.ItemCard);
			}

			if (items.Count() >= MANY_REWARDS_AMOUNT)
			{
				_container.AddToClassList(USS_REWARD_SUMMARY_CONTAINER_MANY_REWARDS_MODIFIER);
			}

			if (fameRewards)
			{
				Element.AddToClassList(USS_FAME_REWARDS_SUMMARY);
			}
		}

		public void Show()
		{
			_animatedBackground.SetColor(_summaryBgColor);
			if (_isFame)
			{
				_animationController.StartAnimation(_animationDirector, FAME_MIDDLE_SKIP_TIME, FAME_MIDDLE_SKIP_TIME, FAME_START_OFFSET);
			}
			else
			{
				_animationController.StartAnimation(_animationDirector, SKIP_ANIMATION_TIME);
			}
		}

		public void SetPlayerLevel(uint level)
		{
			_avatar.SetLevel(level);
		}
	}
}