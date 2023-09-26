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

		protected readonly Label _label;
		protected readonly VisualElement _icon;
		
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

		/// <summary>
		/// Update the current displayed reward
		/// </summary>
		public virtual RewardSummaryItemElement SetReward(IItemViewModel itemViewModel)
		{
			return this;
		}

		public new class UxmlFactory : UxmlFactory<RewardSummaryItemElement, UxmlTraits>
		{
		}
	}
}