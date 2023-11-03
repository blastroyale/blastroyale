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
	public class UnlockRewardSummaryElement : RewardSummaryItemElement
	{
		/// <summary>
		/// Update the current displayed reward
		/// </summary>
		public override RewardSummaryItemElement SetReward(IItemViewModel itemViewModel)
		{
			_icon.style.backgroundImage = StyleKeyword.Null;
			_icon.RemoveSpriteClasses();
			itemViewModel.DrawIcon(_icon);
			_label.text = itemViewModel.DisplayName;
			return this;
		}

		public new class UxmlFactory : UxmlFactory<UnlockRewardSummaryElement, UxmlTraits>
		{
		}
	}
}