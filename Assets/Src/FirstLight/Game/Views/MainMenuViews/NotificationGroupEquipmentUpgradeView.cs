using FirstLight.Game.Utils;
using Quantum;

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
				var upgradeCost = DataProvider.EquipmentDataProvider.GetUpgradeCost(equipment);

				if (!equipment.IsMaxLevel() && equipment.GameId.IsInGroup(_groupId) &&
				    !equipment.GameId.IsInGroup(GameIdGroup.Weapon) &&
				    sc >= upgradeCost)
				{
					numInGroup++;
				}
			}

			NotificationText.SetText(numInGroup.ToString());

			return numInGroup > 0;
		}
	}
}