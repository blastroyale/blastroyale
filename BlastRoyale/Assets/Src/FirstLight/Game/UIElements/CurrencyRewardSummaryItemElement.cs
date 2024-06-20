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
			itemViewModel.DrawIcon(_icon);
			_label.text = itemViewModel.Description;
			return this;
		}

		public new class UxmlFactory : UxmlFactory<RewardSummaryItemElement, UxmlTraits>
		{
		}
	}
}