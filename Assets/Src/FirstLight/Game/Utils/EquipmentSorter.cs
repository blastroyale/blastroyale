using System.Collections.Generic;
using FirstLight.Game.Views.MainMenuViews;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// This View handles the Equipment / Loot Menu.
	/// </summary>
	public class EquipmentSorter: IComparer<EquipmentGridItemView.EquipmentGridItemData>
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
					compare = ((int) x.Info.DataInfo.Data.Rarity).CompareTo((int) y.Info.DataInfo.Data.Rarity);
					break;
				case EquipmentSortState.Type:
					compare = ((int) x.Info.DataInfo.GameId.GetSlot()).CompareTo((int) y.Info.DataInfo.GameId.GetSlot());
					break;
			}

			if (compare != 0)
			{
				return compare;
			}

			compare = ((int) x.Info.DataInfo.GameId).CompareTo((int) y.Info.DataInfo.GameId);

			if (compare != 0)
			{
				return compare;
			}

			return x.Info.DataInfo.Data.Id.CompareTo(y.Info.DataInfo.Data.Id);
		}
	}
}