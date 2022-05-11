using System;
using FirstLight.Game.Ids;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Handles UniqueId based Upgrade Notifications, e.g. a specific Shotgun can be upgraded.
	/// </summary>
	public class NotificationUniqueIdUpgradeView : NotificationUpgradeViewBase
	{
		[SerializeField] private GameId _currency = GameId.SC;

		private UniqueId _uniqueId;

		/// <summary>
		/// Sets the <paramref name="uniqueId"/> of this Notification and sets it's Notification State.
		/// Called by a Presenter when the screen containing this element is opened.
		/// </summary>
		public void SetUniqueId(UniqueId uniqueId)
		{
			_uniqueId = uniqueId;
			NotificationText.SetText("");
			SetState(DataProvider.CurrencyDataProvider.GetCurrencyAmount(_currency));
		}

		/// <inheritdoc />
		public override void UpdateState()
		{
			throw new InvalidOperationException($"Call {nameof(SetUniqueId)} instead");
		}

		protected override void OnCurrencyChanged(GameId currency, ulong newAmount, ulong change,
		                                          ObservableUpdateType updateType)
		{
			if (currency != _currency)
			{
				return;
			}

			SetState(newAmount);
		}

		private void SetState(ulong currencyAmount)
		{
			if (!DataProvider.UniqueIdDataProvider.Ids.ContainsKey(_uniqueId))
			{
				SetNotificationState(false);
			}
			else
			{
				var equipment = DataProvider.EquipmentDataProvider.Inventory[_uniqueId];
				var upgradeCost = DataProvider.EquipmentDataProvider.GetUpgradeCost(equipment);

				SetNotificationState(equipment.Level < equipment.MaxLevel && currencyAmount >= upgradeCost);
			}
		}
	}
}