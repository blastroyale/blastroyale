using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.GridViews;
using FirstLight.Game.Views.MainMenuViews;
using TMPro;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Infos;
using FirstLight.Services;
using I2.Loc;
using FirstLight.Game.Messages;
using FirstLight.Game.Commands;
using Quantum;
using Button = UnityEngine.UI.Button;
using FirstLight.Game.Configs.AssetConfigs;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
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