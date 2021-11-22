using Quantum;
using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Handles Upgrade Notifications for Equipment. We don't check for Weapon upgrades in this version of the game.
	/// </summary>
	public class NotificationGroupEquipmentUpgradeView : NotificationGroupUpgradeView
	{
		protected override bool CheckGroup()
		{
			var numInGroup = 0;
			var sc = DataProvider.CurrencyDataProvider.GetCurrencyAmount(GameId.SC);

			for (var i = 0; i < DataProvider.EquipmentDataProvider.Inventory.Count; i++)
			{
				var equipment = DataProvider.EquipmentDataProvider.Inventory[i];
				var info = DataProvider.EquipmentDataProvider.GetEquipmentInfo(equipment.Id);

				if (!info.IsMaxLevel && info.DataInfo.GameId.IsInGroup(_groupId) && (!info.DataInfo.GameId.IsInGroup(GameIdGroup.Weapon)) && sc >= info.UpgradeCost)
				{
					numInGroup++;
				}
			}

			NotificationText.SetText(numInGroup.ToString() );
			
			return numInGroup > 0;
		}
	}
}
