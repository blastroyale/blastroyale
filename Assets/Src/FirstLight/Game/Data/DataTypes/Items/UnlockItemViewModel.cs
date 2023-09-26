using System;
using FirstLight.Game.Configs;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Data.DataTypes
{
	public class UnlockItemViewModel : IItemViewModel
	{
		public UnlockSystem UnlockSystem { get; }
		public GameId GameId { get; }
		public uint Amount { get; }
		public string DisplayName => UnlockSystem.ToString().ToUpper(); // TODO: Move to localizations
		public VisualElement ItemCard => new UnlockRewardSummaryElement()
		{
			pickingMode = PickingMode.Ignore
		}.SetReward(this);

		public void LegacyRenderSprite(VisualElement icon, Label name, Label amount)
		{
			icon.RemoveSpriteClasses();
			icon.style.backgroundImage = StyleKeyword.Null;
			name.text = "NEW SCREEN UNLOCKED!";
			if(amount != null) amount.text = DisplayName;
			icon.AddToClassList("sprite-home__icon-shop");
		}

		public UnlockItemViewModel(ItemData item)
		{
			if (item.MetadataType != ItemMetadataType.Unlock)
			{
				throw new Exception($"Building View {GetType().Name} with wrong item type {item}");
			}
			UnlockSystem = item.GetMetadata<UnlockMetadata>().Unlock;
		}
	}
}