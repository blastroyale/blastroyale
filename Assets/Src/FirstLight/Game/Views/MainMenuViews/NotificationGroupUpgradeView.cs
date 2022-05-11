using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Handles Upgrade Notifications for a group. E.g. check all Weapons to see if any can be upgraded.
	/// </summary>
	public class NotificationGroupUpgradeView : NotificationUpgradeViewBase
	{
		[SerializeField] protected GameIdGroup _groupId;
		
		/// <inheritdoc />
		public override void UpdateState()
		{
			SetNotificationState(CheckGroup());
		}
		
		protected virtual bool CheckGroup()
		{
			var numInGroup = 0;
			var sc = DataProvider.CurrencyDataProvider.GetCurrencyAmount(GameId.SC);

			for (var i = 0; i < DataProvider.EquipmentDataProvider.Inventory.Count; i++)
			{
				var equipment = DataProvider.EquipmentDataProvider.Inventory[i];
				var upgradeCost = DataProvider.EquipmentDataProvider.GetUpgradeCost(equipment);

				if (!equipment.IsMaxLevel() && equipment.GameId.IsInGroup(_groupId) && sc >= upgradeCost)
				{
					numInGroup++;
				}
			}

			NotificationText.SetText(numInGroup.ToString() );
			
			return numInGroup > 0;
		}

		protected override void OnCurrencyChanged(GameId currency, ulong newAmount, ulong change, ObservableUpdateType updateType)
		{
			SetNotificationState(CheckGroup());
		}
		
	}
}
