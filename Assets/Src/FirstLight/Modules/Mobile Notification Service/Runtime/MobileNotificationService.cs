using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLight.NotificationService
{
	/// <summary>
	/// This service allows to schedule and handle notifications on the current platform
	/// </summary>
	public interface INotificationService
	{
		/// <summary>
		/// Event fired when a scheduled local notification is delivered while the app is in the foreground.
		/// </summary>
		event Action<PendingNotification> OnLocalNotificationDeliveredEvent;

		/// <summary>
		/// Event fired when a queued local notification is cancelled because the application is in the foreground
		/// when it was meant to be displayed.
		/// </summary>
		/// <seealso cref="OperatingMode.Queue"/>
		event Action<PendingNotification> OnLocalNotificationExpiredEvent;
		
		/// <summary>
		/// Requests the status of of the notification service and if the player accepted to receive it
		/// </summary>
		bool IsNotificationsOn { get; }
		
		/// <summary>
		/// Requests the status of of the notification service and if the player denied to receive it
		/// </summary>
		bool IsNotificationsDenied { get; }

		/// <summary>
		/// Gets a collection of notifications that are scheduled or queued.
		/// </summary>
		IReadOnlyList<PendingNotification> PendingNotifications { get; }
		
		/// <summary>
		/// Create a new instance of a <see cref="IGameNotification"/> for this platform.
		/// </summary>
		/// <returns>A new platform-appropriate notification object.</returns>
		IGameNotification CreateNotification();

		/// <summary>
		/// Schedules a notification to be delivered.
		/// </summary>
		/// <param name="gameNotification">The notification to deliver.</param>
		/// <exception cref="ArgumentNullException"><paramref name="gameNotification"/> is null.</exception>
		/// <exception cref="InvalidOperationException"><paramref name="gameNotification"/> isn't of the correct type.</exception>
		PendingNotification ScheduleNotification(IGameNotification gameNotification);

		/// <summary>
		/// Cancels a scheduled notification.
		/// </summary>
		/// <param name="notificationId">The ID of a previously scheduled notification.</param>
		void CancelNotification(int notificationId);

		/// <summary>
		/// Dismiss a displayed notification.
		/// </summary>
		/// <param name="notificationId">The ID of a previously scheduled notification that is being displayed to the user.</param>
		void DismissNotification(int notificationId);

		/// <summary>
		/// Cancels all scheduled notifications.
		/// </summary>
		void CancelAllScheduledNotifications();

		/// <summary>
		/// Dismisses all displayed notifications.
		/// </summary>
		void DismissAllDisplayedNotifications();

		/// <summary>
		/// Request the permission pop up to the user to allow to receive local and remote notifications.
		/// Only needed for iOS
		/// </summary>
		Task RequestPermissions();
	}
	
	/// <inheritdoc />
	public class MobileNotificationService : INotificationService
	{
		private readonly GameNotificationsMonoBehaviour _monoBehaviour;

		/// <inheritdoc />
		public event Action<PendingNotification> OnLocalNotificationDeliveredEvent;
		/// <inheritdoc />
		public event Action<PendingNotification> OnLocalNotificationExpiredEvent;

		/// <inheritdoc />
		public bool IsNotificationsOn
		{
#if UNITY_IOS
			get => Unity.Notifications.iOS.iOSNotificationCenter.GetNotificationSettings().AuthorizationStatus ==
			       Unity.Notifications.iOS.AuthorizationStatus.Authorized;
#else
			get => true;
#endif
		}

		/// <inheritdoc />
		public bool IsNotificationsDenied
		{
#if UNITY_IOS
			get => Unity.Notifications.iOS.iOSNotificationCenter.GetNotificationSettings().AuthorizationStatus ==
			       Unity.Notifications.iOS.AuthorizationStatus.Denied;
#else
			get => false;
#endif
		}

		/// <inheritdoc />
		public IReadOnlyList<PendingNotification> PendingNotifications => _monoBehaviour.PendingNotifications;
		
		public MobileNotificationService(params GameNotificationChannel[] channels)
		{
			_monoBehaviour = new GameObject("NotificationService").AddComponent<GameNotificationsMonoBehaviour>();
			_monoBehaviour.OnLocalNotificationDelivered = OnLocalNotificationDeliveredEvent;
			_monoBehaviour.OnLocalNotificationExpired = OnLocalNotificationExpiredEvent;

			_monoBehaviour.Initialize(channels);
			UnityEngine.Object.DontDestroyOnLoad(_monoBehaviour);
		}

		/// <inheritdoc />
		public IGameNotification CreateNotification()
		{
#if UNITY_EDITOR
			return new EditorGameNotification();
#else
			return _monoBehaviour.CreateNotification();
#endif
		}

		/// <inheritdoc />
		public PendingNotification ScheduleNotification(IGameNotification gameNotification)
		{
#if UNITY_EDITOR
			if (!gameNotification.Id.HasValue)
            {
                // Generate an ID for items that don't have one (just so they can be identified later)
                gameNotification.Id = Math.Abs(DateTime.Now.ToString("yyMMddHHmmssffffff").GetHashCode());
            }
			return new PendingNotification(gameNotification);
#else
			return _monoBehaviour.ScheduleNotification(gameNotification);
#endif
		}

		/// <inheritdoc />
		public void CancelNotification(int notificationId)
		{
			_monoBehaviour.CancelNotification(notificationId);
		}

		/// <inheritdoc />
		public void DismissNotification(int notificationId)
		{
			_monoBehaviour.DismissNotification(notificationId);
		}

		/// <inheritdoc />
		public void CancelAllScheduledNotifications()
		{
			_monoBehaviour.CancelAllNotifications();
		}

		/// <inheritdoc />
		public void DismissAllDisplayedNotifications()
		{
			_monoBehaviour.DismissAllNotifications();
		}

		/// <inheritdoc />
		public async Task RequestPermissions()
		{
#if UNITY_IOS
			if (Unity.Notifications.iOS.iOSNotificationCenter.GetNotificationSettings().AuthorizationStatus ==
			    Unity.Notifications.iOS.AuthorizationStatus.NotDetermined)
			{
				var request = new Unity.Notifications.iOS.AuthorizationRequest(Unity.Notifications.iOS.AuthorizationOption.Alert |
				                                                               Unity.Notifications.iOS.AuthorizationOption.Sound |
				                                                               Unity.Notifications.iOS.AuthorizationOption.Badge, true);
			
				while (!request.IsFinished)
				{
					await Task.Yield();
				}
			}
#endif
			await Task.Yield();
		}
	}
}