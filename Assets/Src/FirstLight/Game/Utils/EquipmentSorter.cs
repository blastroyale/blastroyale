using System.Collections.Generic;
using FirstLight.Game.Views.MainMenuViews;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// This View handles the Equipment / Loot Menu.
	/// </summary>
	public class EquipmentSorter : IComparer<EquipmentGridItemView.EquipmentGridItemData>
	{
		public enum EquipmentSortState
		{
			All = 0,
			Type,
			Rarity,
			Size,
		}

		private readonly EquipmentSortState _equipmentSortState;

		public EquipmentSorter(EquipmentSortState equipmentSortState)
		{
			_equipmentSortState = equipmentSortState;
		}

		public int Compare(EquipmentGridItemView.EquipmentGridItemData x, EquipmentGridItemView.EquipmentGridItemData y)
		{
			var compare = 0;

			switch (_equipmentSortState)
			{
				case EquipmentSortState.Rarity:
					compare = ((int) x.Equipment.Rarity).CompareTo((int) y.Equipment.Rarity);
					break;
				case EquipmentSortState.Type:
					compare = ((int) x.Equipment.GameId.GetSlot()).CompareTo((int) y.Equipment.GameId.GetSlot());
					break;
			}

			if (compare != 0)
			{
				return compare;
			}

			compare = ((int) x.Equipment.GameId).CompareTo((int) y.Equipment.GameId);

			if (compare != 0)
			{
				return compare;
			}

			return x.Id.CompareTo(y.Id);
		}
	}
}