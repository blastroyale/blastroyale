using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A single item in the summary part of the RewardsScreen
	/// </summary>
	public class RewardSummaryItemElement : VisualElement
	{
		private const string USS_BLOCK = "reward-summary-item";
		private const string USS_ICON = USS_BLOCK + "__icon";
		private const string USS_LABEL = USS_BLOCK + "__label";
		
		private const string USS_SPRITE_ICON_COIN = "sprite-shared__icon-currency-coin";

		private readonly Label _label;
		private readonly VisualElement _icon;

		public RewardSummaryItemElement()
		{
			AddToClassList(USS_BLOCK);
			Add(_icon = new VisualElement
			{
				name = "RewardIcon",
			});
			_icon.AddToClassList(USS_ICON);
			_icon.AddToClassList(USS_SPRITE_ICON_COIN);
			Add(_label = new Label("X 10000"));
			_label.AddToClassList(USS_LABEL);
		}

		public RewardSummaryItemElement(IReward reward) : this()
		{
			SetReward(reward);
		}

		/// <summary>
		/// Update the current displayed reward
		/// </summary>
		public void SetReward(IReward reward)
		{
			_icon.style.backgroundImage = StyleKeyword.Null;
			_icon.RemoveSpriteClasses();
			if (reward is UnlockReward ur)
			{
				switch (ur.UnlockSystem)
				{
					case UnlockSystem.ShopScreen:
						_icon.AddToClassList("sprite-home__icon-shop");
						break;
				}

				_label.text = ur.DisplayName;
			}
			else
			{
#pragma warning disable CS4014
				UIUtils.SetSprite(reward.GameId, _icon);
#pragma warning restore CS4014
				_label.text = "X " + reward.Amount;
			}
		}

		public new class UxmlFactory : UxmlFactory<RewardSummaryItemElement, UxmlTraits>
		{
		}
	}
}