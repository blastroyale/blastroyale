using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class ProfilePictureRewardSummaryItemElement : RewardSummaryItemElement
	{
		private int _requestTextureHandle = -1;
		
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