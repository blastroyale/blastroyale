using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Collection items view model
	/// </summary>
	public class CollectionViewModel : IItemViewModel
	{
		public GameId GameId { get; }
		public uint Amount { get; }
		public string DisplayName { get; }
		public VisualElement ItemCard => new CollectionRewardsSummaryElement()
		{
			pickingMode = PickingMode.Ignore
		}.SetReward(this);

		public void LegacyRenderSprite(VisualElement icon, Label name, Label amount)
		{
			icon.style.backgroundImage = StyleKeyword.Null;
			icon.RemoveSpriteClasses();
			name.text = DisplayName;
			icon.AddToClassList(GameId.GetUSSSpriteClass());
		}

		public CollectionViewModel(ItemData item)
		{
			GameId = item.Id;
			Amount = 1;
			DisplayName = GameId.GetLocalization().ToUpper();
		}
	}
}