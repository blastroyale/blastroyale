using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class CurrencyRewardSummaryItemElement : RewardSummaryItemElement
	{
		public override RewardSummaryItemElement SetReward(IItemViewModel itemViewModel)
		{
			_icon.style.backgroundImage = StyleKeyword.Null;
			_icon.RemoveSpriteClasses();
#pragma warning disable CS4014
			UIUtils.SetSprite(itemViewModel.GameId, _icon);
#pragma warning restore CS4014
			_label.text = "X " + itemViewModel.Amount;
			return this;
		}

		public new class UxmlFactory : UxmlFactory<RewardSummaryItemElement, UxmlTraits>
		{
		}
	}
}