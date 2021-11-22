
using FirstLight.Game.Ids;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Handles UniqueId based Notifications, e.g. a specific new weapon was found. A player might already own one
	/// shotgun, but if they find another one, the new Shotgun should have a notification on it.
	/// </summary>
	public class NotificationUniqueIdView : NotificationNewViewBase
	{
		private UniqueId _uniqueId;
		
		/// <summary>
		/// Sets the Unique ID of the object this component is placed on.
		///
		/// UniqueId's are  assigned dynamically so this needs to be done at run time, e.g. opening the
		/// Equipment Screen to look at a new piece of equipment.
		/// </summary>
		public void SetUniqueId(UniqueId uniqueId, bool playAnimation = true)
		{
			_uniqueId = uniqueId;

			SetNotificationState(DataProvider.UniqueIdDataProvider.NewIds.Contains(uniqueId), playAnimation);
		}

		/// <inheritdoc />
		public override void UpdateState()
		{
			SetNotificationState(false);
		}

		protected override void OnUniqueIdChanged(int id, UniqueId uniqueId, UniqueId change, ObservableUpdateType updateType)
		{
			if (uniqueId == _uniqueId && updateType == ObservableUpdateType.Removed)
			{
				SetNotificationState(false);
			}
		}
	}
}
