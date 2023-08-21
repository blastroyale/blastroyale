using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using FirstLight.UiService;
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

		private Label _name;
		private Label _amount;
		private VisualElement _icon;

		public void Init(RewardsAnimationController animationController, AnimatedBackground animatedBackground, PlayableDirector animationDirector)
		{
			_animationController = animationController;
			_animatedBackground = animatedBackground;
			_animationDirector = animationDirector;
		}

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_name = element.Q<Label>("RewardName");
			_amount = element.Q<Label>("RewardAmount");
			_icon = element.Q<VisualElement>("RewardIcon");
		}

		public void ShowReward(IReward reward)
		{
			_animatedBackground.SetDefault();
			_animationController.StartAnimation(_animationDirector, SKIP_ANIMATION_TIME);
			_icon.RemoveSpriteClasses();
			_icon.style.backgroundImage = StyleKeyword.Null;
			
			if (reward is UnlockReward ur)
			{
				_name.text = "NEW SCREEN UNLOCKED!";
				_amount.text = ur.DisplayName;
				_icon.AddToClassList("sprite-home__icon-shop");
			}
			else
			{
				_amount.text = $"X {reward.Amount}";
				_name.text = reward.DisplayName;
#pragma warning disable CS4014
				// Ignore task return because it only loads the sprite and we don't want to wait for it
				UIUtils.SetSprite(reward.GameId, _icon);
#pragma warning restore CS4014
			}
		}
	}
}