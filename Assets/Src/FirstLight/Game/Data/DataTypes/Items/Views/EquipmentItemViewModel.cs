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
		public Equipment Equipment => _equipment;
		public GameId GameId => _equipment.GameId;
		public uint Amount => 1;
		public string DisplayName => GameId.GetLocalization().ToUpper();
		public VisualElement ItemCard => new EquipmentCardElement(_equipment)
		{
			pickingMode = PickingMode.Ignore
		};

		public void LegacyRenderSprite(VisualElement icon, Label name, Label amount)
		{
			icon.RemoveSpriteClasses();
			icon.style.backgroundImage = StyleKeyword.Null;
			name.text = DisplayName;
#pragma warning disable CS4014
			UIUtils.SetSprite(GameId, icon);
#pragma warning restore CS4014
		}

		private Equipment _equipment;

		public EquipmentItemViewModel(ItemData item)
		{
			if (item.MetadataType != ItemMetadataType.Equipment)
			{
				throw new Exception($"Building View {GetType().Name} with wrong item type {item}");
			}
			_equipment = item.GetMetadata<EquipmentMetadata>().Equipment;
		}
	}
}