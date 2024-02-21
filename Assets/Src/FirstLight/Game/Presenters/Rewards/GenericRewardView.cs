using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Handles the generic reward part of the RewardsScreen (ex Coins)
	/// </summary>
	public class GenericRewardView : UIView
	{
		private const float SKIP_ANIMATION_TIME = 0.2f;

		private RewardsAnimationController _animationController;
		private AnimatedBackground _animatedBackground;
		private PlayableDirector _animationDirector;
		private AnimatedBackground.AnimatedBackgroundColor _genericBgColor;

		private Label _name;
		private Label _amount;
		private VisualElement _icon;

		public void Init(RewardsAnimationController animationController, AnimatedBackground animatedBackground, PlayableDirector animationDirector, AnimatedBackground.AnimatedBackgroundColor genericBgColor)
		{
			_animationController = animationController;
			_animatedBackground = animatedBackground;
			_animationDirector = animationDirector;
			_genericBgColor = genericBgColor;
		}

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_name = element.Q<Label>("RewardName");
			_amount = element.Q<Label>("RewardAmount");
			_icon = element.Q<VisualElement>("RewardIcon");
		}

		public void ShowReward(IItemViewModel itemViewModel)
		{
			_animatedBackground.SetColor(_genericBgColor);
			_animationController.StartAnimation(_animationDirector, SKIP_ANIMATION_TIME);
			_icon.RemoveModifiers();
			itemViewModel.DrawIcon(_icon);
			_name.text = itemViewModel.GameId.IsInGroup(GameIdGroup.ProfilePicture) ? "AVATAR" : itemViewModel.DisplayName;

			if (itemViewModel.Description != null)
			{
				_amount.SetVisibility(true);
				_amount.text = itemViewModel.Description;
			}
			else
			{
				_amount.SetVisibility(false);
			}
		}
	}
}