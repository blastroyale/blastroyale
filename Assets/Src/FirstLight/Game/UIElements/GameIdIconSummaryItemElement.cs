using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class GameIdIconSummaryItemElement : RewardSummaryItemElement
	{
		public override RewardSummaryItemElement SetReward(IItemViewModel itemViewModel)
		{
			itemViewModel.DrawIcon(_icon);
			_label.text = itemViewModel.DisplayName;
			return this;
		}

		public new class UxmlFactory : UxmlFactory<CollectionRewardsSummaryElement, UxmlTraits>
		{
		}
	}
}