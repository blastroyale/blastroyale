using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.NotificationService;

namespace FirstLight.Tests.EditorMode
{
	public class StubNotificationService : INotificationService
	{
		private bool _isNotificationsOn;
		private bool _isNotificationsDenied;
		private IReadOnlyList<PendingNotification> _pendingNotifications;
		public event Action<PendingNotification> OnLocalNotificationDeliveredEvent;
		public event Action<PendingNotification> OnLocalNotificationExpiredEvent;

		public bool IsNotificationsOn => _isNotificationsOn;

		public bool IsNotificationsDenied => _isNotificationsDenied;

		public IReadOnlyList<PendingNotification> PendingNotifications => _pendingNotifications;

		public IGameNotification CreateNotification()
		{
			return null;
		}

		public PendingNotification ScheduleNotification(IGameNotification gameNotification)
		{
			return null;
		}

		public void CancelNotification(int notificationId)
		{
			
		}

		public void DismissNotification(int notificationId)
		{
			
		}

		public void CancelAllScheduledNotifications()
		{
		
		}

		public void DismissAllDisplayedNotifications()
		{
			
		}

		public async Task RequestPermissions()
		{
			await Task.Delay(1);
		}
	}
}