using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Handles Notifications for unclaimed Trophy Road Rewards. If a player has a Trophy Road reward that hasn't been claimed yet,
	/// the notification will show.
	/// </summary>
	public class NotificationTrophyRoadView : NotificationViewBase
	{
		/// <summary>
		/// Updates the state of this notification manually. To be called from a Presenter.
		/// </summary>
		public override void UpdateState()
		{
			SetNotificationState(CheckRewards());
		}
		
		/// <summary>
		/// Check for unclaimed Trophy Road rewards.
		/// </summary>
		private bool CheckRewards()
		{
			var infos = DataProvider.TrophyRoadDataProvider.GetAllInfos();

			foreach (var rewardInfo in infos)
			{
				if (DataProvider.TrophyRoadDataProvider.GetInfo(rewardInfo.Level).IsReadyToCollect) 
				{
					return true;
				}
			}

			return false;
		}
	}
}
