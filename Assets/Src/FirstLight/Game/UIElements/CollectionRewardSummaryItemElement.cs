using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class CollectionRewardsSummaryElement : RewardSummaryItemElement
	{
		public override RewardSummaryItemElement SetReward(IItemViewModel itemViewModel)
		{
			_icon.style.backgroundImage = StyleKeyword.Null;
			_icon.RemoveSpriteClasses();
			_icon.AddToClassList(itemViewModel.GameId.GetUSSSpriteClass());
			_label.text = itemViewModel.DisplayName;
			return this;
		}

		public new class UxmlFactory : UxmlFactory<CollectionRewardsSummaryElement, UxmlTraits>
		{
		}
	}
}