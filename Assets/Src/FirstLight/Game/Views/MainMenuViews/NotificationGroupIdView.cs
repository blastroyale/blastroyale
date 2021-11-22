using Quantum;
using UnityEngine;
using FirstLight.Game.Ids;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Handles Group based Notifications, e.g. a new Weapon or Shield was found, etc.
	/// </summary>
	public class NotificationGroupIdView : NotificationNewViewBase
	{
		[SerializeField] private GameIdGroup _groupId;

		/// <summary>
		/// Updates the state of this notification manually. To be called from a Presenter.
		/// </summary>
		public override void UpdateState()
		{
			SetNotificationState(CheckGroup());
		}

		protected override void OnUniqueIdChanged(int id, UniqueId uniqueId, UniqueId uniqueIdChange, ObservableUpdateType updateType)
		{
			if (State != CheckGroup())
			{
				SetNotificationState(!State);
			}
		}

		private bool CheckGroup()
		{
			var newIds = DataProvider.UniqueIdDataProvider.NewIds;
			var ids = DataProvider.UniqueIdDataProvider.Ids;
			var numInGroup = 0;
			
			for (var i = 0; i < newIds.Count; i++)
			{
				// This checks avoids legacy ids to crash the local data and corrupt the game for the player
				if (!ids.TryGetValue(newIds[i], out var id))
				{
					DataProvider.UniqueIdDataProvider.NewIds.Remove(newIds[i]);
					continue;
				}
				
				if (id.IsInGroup(_groupId))
				{
					numInGroup++;
				}
			}
			
			NotificationText.SetText(numInGroup.ToString() );

			return numInGroup > 0;
		}
	}
}
