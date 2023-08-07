using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
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

		private RewardsAnimationController _animationController;
		private AnimatedBackground _animatedBackground;
		private PlayableDirector _animationDirector;

		private VisualElement _container;

		public void Init(RewardsAnimationController animationController, AnimatedBackground animatedBackground, PlayableDirector animationDirector)
		{
			_animationController = animationController;
			_animatedBackground = animatedBackground;
			_animationDirector = animationDirector;
		}

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_container = element.Q<VisualElement>("RewardsContainer").Required();
		}


		public void CreateSummaryElements(IList<IReward> rewards)
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
		}

		public void Show()
		{
			_animatedBackground.SetDefault();
			_animationController.StartAnimation(_animationDirector, SKIP_ANIMATION_TIME);
		}
	}
}