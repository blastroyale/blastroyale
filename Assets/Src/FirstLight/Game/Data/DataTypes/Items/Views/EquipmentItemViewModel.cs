using System;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Equipment reward holder
	/// </summary>
	public class EquipmentItemViewModel : IItemViewModel
	{
		public ItemData Item { get; }
		public GameId GameId => Item.Id;
		public uint Amount => 1;
		public string Description => null;
		public string DisplayName => "Legacy Equipment";
		public string ItemTypeDisplayName => GameIdGroup.Equipment.GetGameIdGroupLocalization();
		public VisualElement ItemCard => new GameIdIconSummaryItemElement()
		{
			pickingMode = PickingMode.Ignore
		}.SetReward(this);

		public void DrawIcon(VisualElement icon)
		{
			icon.RemoveSpriteClasses();
			icon.style.backgroundImage = StyleKeyword.Null;
			icon.AddToClassList("sprite-home__icon-questionmark");
		}


		public EquipmentItemViewModel(ItemData item)
		{
			if (item.MetadataType != ItemMetadataType.Equipment)
			{
				throw new Exception($"Building View {GetType().Name} with wrong item type {item}");
			}

			Item = item;
		}
	}
}