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
		public ItemData Item { get; }
		public UnlockSystem UnlockSystem { get; }
		public GameId GameId { get; }
		public uint Amount { get; }
		public string Description => null;
		public string DisplayName => "NEW SCREEN UNLOCKED"; // TODO: Move to localizations
		public VisualElement ItemCard => new UnlockRewardSummaryElement()
		{
			pickingMode = PickingMode.Ignore
		}.SetReward(this);

		public void DrawIcon(VisualElement icon)
		{
			icon.RemoveSpriteClasses();
			icon.style.backgroundImage = StyleKeyword.Null;
			icon.AddToClassList("sprite-home__icon-shop");
		}

		public UnlockItemViewModel(ItemData item)
		{
			if (item.MetadataType != ItemMetadataType.Unlock)
			{
				throw new Exception($"Building View {GetType().Name} with wrong item type {item}");
			}

			Item = item;
			UnlockSystem = item.GetMetadata<UnlockMetadata>().Unlock;
		}
	}
}