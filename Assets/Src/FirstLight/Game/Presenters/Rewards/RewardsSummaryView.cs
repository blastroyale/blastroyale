using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Handles the summary part of the rewards screen
	/// </summary>
	public class RewardsSummaryView : UIView
	{
		private const float SKIP_ANIMATION_TIME = 0.2f;
		private const int MANY_REWARDS_AMOUNT = 5;
		private const string USS_REWARD_SUMMARY_CONTAINER_MANY_REWARDS_MODIFIER = "rewards-summary__rewards-container--manyrewards";
		private const string USS_FAME_REWARDS_SUMMARY = "rewards-summary--fame";

		private IGameDataProvider _dataProvider;
		private IGameServices _services;

		private RewardsAnimationController _animationController;
		private AnimatedBackground _animatedBackground;
		private PlayableDirector _animationDirector;

		private VisualElement _container;
		private PlayerAvatarElement _avatar;
		private Label _reachLevelLabel;

		private int _avatarRequestHandle = -1;

		public void Init(RewardsAnimationController animationController, AnimatedBackground animatedBackground, PlayableDirector animationDirector)
		{
			_animationController = animationController;
			_animatedBackground = animatedBackground;
			_animationDirector = animationDirector;
		}

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.ResolveServices();

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
			_avatar.SetLevel(currentLevel - 1);
			_avatar.SetAvatar(_dataProvider.AppDataProvider.AvatarUrl);
		}

		public override void UnsubscribeFromEvents()
		{
			_services.RemoteTextureService.CancelRequest(_avatarRequestHandle);
		}

		public void CreateSummaryElements(IList<IReward> rewards, bool fameRewards)
		{
			// Clean up example elements
			for (var i = _container.childCount - 1; i >= 0; i--)
			{
				_container.RemoveAt(i);
			}

			foreach (var dataReward in rewards)
			{
				if (dataReward is EquipmentReward er)
				{
					var eq = new EquipmentCardElement(er.Equipment, new UniqueId())
					{
						pickingMode = PickingMode.Ignore
					};
					_container.Add(eq);
					continue;
				}

				var el = new RewardSummaryItemElement(dataReward)
				{
					pickingMode = PickingMode.Ignore
				};
				_container.Add(el);
			}

			if (rewards.Count >= MANY_REWARDS_AMOUNT)
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
			_animatedBackground.SetDefault();
			_animationController.StartAnimation(_animationDirector, SKIP_ANIMATION_TIME);
		}

		public void SetPlayerLevel(uint level)
		{
			_avatar.SetLevel(level);
		}
	}
}