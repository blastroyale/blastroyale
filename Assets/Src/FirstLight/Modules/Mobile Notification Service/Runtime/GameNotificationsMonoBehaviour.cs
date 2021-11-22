using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLight.NotificationService
{
    /// <summary>
    /// The operating modes for the notifications manager
    /// </summary>
    [Flags]
    public enum OperatingMode
    {
        /// <summary>
        /// Do not perform any queueing at all. All notifications are scheduled with the operating system
        /// immediately.
        /// </summary>
        NoQueue = 0x00,

        /// <summary>
        /// <para>
        /// Queue messages that are scheduled with this manager.
        /// No messages will be sent to the operating system until the application is backgrounded.
        /// </para>
        /// <para>
        /// If badge numbers are not set, will automatically increment them. This will only happen if NO badge numbers
        /// for pending notifications are ever set.
        /// </para>
        /// </summary>
        Queue = 0x01,

        /// <summary>
        /// When the application is foregrounded, clear all pending notifications.
        /// </summary>
        ClearOnForegrounding = 0x02,

        /// <summary>
        /// After clearing events, will put future ones back into the queue if they are marked with <see cref="PendingNotification.Reschedule"/>.
        /// </summary>
        /// <remarks>
        /// Only valid if <see cref="ClearOnForegrounding"/> is also set.
        /// </remarks>
        RescheduleAfterClearing = 0x04,

        /// <summary>
        /// Combines the behaviour of <see cref="Queue"/> and <see cref="ClearOnForegrounding"/>.
        /// </summary>
        QueueAndClear = Queue | ClearOnForegrounding,

        /// <summary>
        /// <para>
        /// Combines the behaviour of <see cref="Queue"/>, <see cref="ClearOnForegrounding"/> and
        /// <see cref="RescheduleAfterClearing"/>.
        /// </para>
        /// <para>
        /// Ensures that messages will never be displayed while the application is in the foreground.
        /// </para>
        /// </summary>
        QueueClearAndReschedule = Queue | ClearOnForegrounding | RescheduleAfterClearing,
    }
        
    /// <summary>
    /// Global notifications manager that serves as a wrapper for multiple platforms' notification systems.
    /// </summary>
    public sealed class GameNotificationsMonoBehaviour : MonoBehaviour
    {
        
        
        // Minimum amount of time that a notification should be into the future before it's queued when we background.
        private static readonly TimeSpan _minimumNotificationTime = new TimeSpan(0, 0, 2);

        /// <summary>
        /// The operating mode for the notifications manager
        /// </summary>
        public OperatingMode Mode = OperatingMode.NoQueue;

        /// <summary>
        /// Check to make the notifications manager automatically set badge numbers so that they increment.
        /// Schedule notifications with no numbers manually set to make use of this feature.
        /// </summary>
        public bool AutoBadging = true;

        /// <summary>
        /// Event fired when a scheduled local notification is delivered while the app is in the foreground.
        /// </summary>
        public Action<PendingNotification> OnLocalNotificationDelivered;

        /// <summary>
        /// Event fired when a queued local notification is cancelled because the application is in the foreground
        /// when it was meant to be displayed.
        /// </summary>
        /// <seealso cref="OperatingMode.Queue"/>
        public Action<PendingNotification> OnLocalNotificationExpired;

        private IGameNotificationsPlatform _platform;
        private bool _inForeground = true;

        /// <summary>
        /// Gets a collection of notifications that are scheduled or queued.
        /// </summary>
        public List<PendingNotification> PendingNotifications { get; private set; } = new List<PendingNotification>();

        /// <summary>
        /// Gets whether this manager has been initialized.
        /// </summary>
        public bool Initialized { get; private set; }

        /// <summary>
        /// Clean up platform object if necessary
        /// </summary>
        private void OnDestroy()
        {
            if (_platform == null)
            {
                return;
            }

            _platform.NotificationReceived -= OnNotificationReceived;
            if (_platform is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _inForeground = false;
        }

        /// <summary>
        /// Check pending list for expired notifications, when in queue mode.
        /// </summary>
        private void Update()
        {
            if ((Mode & OperatingMode.Queue) != OperatingMode.Queue)
            {
                return;
            }

            // Check each pending notification for expiry, then remove it
            for (int i = PendingNotifications.Count - 1; i >= 0; --i)
            {
                PendingNotification queuedNotification = PendingNotifications[i];
                DateTime? time = queuedNotification.Notification.DeliveryTime;
                if (time != null && time < DateTime.Now)
                {
                    PendingNotifications.RemoveAt(i);
                    OnLocalNotificationExpired?.Invoke(queuedNotification);
                }
            }
        }

        /// <summary>
        /// Respond to application foreground/background events.
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (_platform == null || !Initialized)
            {
                return;
            }

            _inForeground = hasFocus;

            if (hasFocus)
            {
                OnForegrounding();

                return;
            }

            _platform.OnBackground();

            // Backgrounding. Queue future dated notifications
            if ((Mode & OperatingMode.Queue) == OperatingMode.Queue)
            {
                // Filter out past events
                for (var i = PendingNotifications.Count - 1; i >= 0; i--)
                {
                    PendingNotification pendingNotification = PendingNotifications[i];
                    // Ignore already scheduled ones
                    if (pendingNotification.Notification.Scheduled)
                    {
                        continue;
                    }

                    // If a non-scheduled notification is in the past (or not within our threshold)
                    // just remove it immediately
                    if (pendingNotification.Notification.DeliveryTime != null &&
                        pendingNotification.Notification.DeliveryTime - DateTime.Now < _minimumNotificationTime)
                    {
                        PendingNotifications.RemoveAt(i);
                    }
                }

                // Sort notifications by delivery time, if no notifications have a badge number set
                bool noBadgeNumbersSet =
                    PendingNotifications.All(notification => notification.Notification.BadgeNumber == null);

                if (noBadgeNumbersSet && AutoBadging)
                {
                    PendingNotifications.Sort((a, b) =>
                    {
                        if (!a.Notification.DeliveryTime.HasValue)
                        {
                            return 1;
                        }

                        if (!b.Notification.DeliveryTime.HasValue)
                        {
                            return -1;
                        }

                        return a.Notification.DeliveryTime.Value.CompareTo(b.Notification.DeliveryTime.Value);
                    });

                    // Set badge numbers incrementally
                    var badgeNum = 1;
                    foreach (var pendingNotification in PendingNotifications)
                    {
                        if (pendingNotification.Notification.DeliveryTime.HasValue &&
                            !pendingNotification.Notification.Scheduled)
                        {
                            pendingNotification.Notification.BadgeNumber = badgeNum++;
                        }
                    }
                }

                for (int i = PendingNotifications.Count - 1; i >= 0; i--)
                {
                    var pendingNotification = PendingNotifications[i];
                    // Ignore already scheduled ones
                    if (pendingNotification.Notification.Scheduled)
                    {
                        continue;
                    }

                    // Schedule it now
                    _platform.ScheduleNotification(pendingNotification.Notification);
                }

                // Clear badge numbers again (for saving)
                if (noBadgeNumbersSet && AutoBadging)
                {
                    foreach (var pendingNotification in PendingNotifications)
                    {
                        if (pendingNotification.Notification.DeliveryTime.HasValue)
                        {
                            pendingNotification.Notification.BadgeNumber = null;
                        }
                    }
                }
            }

            // Calculate notifications to save
            var notificationsToSave = new List<SerializableNotification>(PendingNotifications.Count);
            foreach (var pendingNotification in PendingNotifications)
            {
                // If we're in clear mode, add nothing unless we're in rescheduling mode
                // Otherwise add everything
                if ((Mode & OperatingMode.ClearOnForegrounding) == OperatingMode.ClearOnForegrounding)
                {
                    if ((Mode & OperatingMode.RescheduleAfterClearing) != OperatingMode.RescheduleAfterClearing)
                    {
                        continue;
                    }

                    // In reschedule mode, add ones that have been scheduled, are marked for
                    // rescheduling, and that have a time
                    if (pendingNotification.Reschedule &&
                        pendingNotification.Notification.Scheduled &&
                        pendingNotification.Notification.DeliveryTime.HasValue)
                    {
                        notificationsToSave.Add(pendingNotification.AsSerializableNotification());
                    }
                }
                else
                {
                    // In non-clear mode, just add all scheduled notifications
                    if (pendingNotification.Notification.Scheduled)
                    {
                        notificationsToSave.Add(pendingNotification.AsSerializableNotification());
                    }
                }
            }

            // Save to disk
            PlayerPrefs.SetString("notifications", JsonUtility.ToJson(notificationsToSave));
        }

        /// <summary>
        /// Initialize the notifications manager.
        /// </summary>
        /// <param name="channels">An optional collection of channels to register, for Android</param>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/> has already been called.</exception>
        public void Initialize(params GameNotificationChannel[] channels)
        {
            if (Initialized)
            {
                throw new InvalidOperationException("NotificationsManager already initialized.");
            }

            Initialized = true;

#if UNITY_ANDROID
            _platform = new AndroidNotificationsPlatform();

            // Register the notification channels
            var doneDefault = false;
            foreach (var notificationChannel in channels)
            {
                var platform = _platform as AndroidNotificationsPlatform;
                
                if (!doneDefault)
                {
                    doneDefault = true;
                    platform.DefaultChannelId = notificationChannel.Id;
                }

                platform.RegisterChannel(notificationChannel);
            }
#elif UNITY_IOS
            _platform = new iOSNotificationsPlatform();
#endif

            if (_platform == null)
            {
                return;
            }

            _platform.NotificationReceived += OnNotificationReceived;

            OnForegrounding();
        }

        /// <summary>
        /// Creates a new notification object for the current platform.
        /// </summary>
        /// <returns>The new notification, ready to be scheduled, or null if there's no valid platform.</returns>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/> has not been called.</exception>
        public IGameNotification CreateNotification()
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            return _platform?.CreateNotification();
        }

        /// <summary>
        /// Schedules a notification to be delivered.
        /// </summary>
        /// <param name="notification">The notification to deliver.</param>
        public PendingNotification ScheduleNotification(IGameNotification notification)
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            if (notification == null)
            {
                return null;
            }

            // If we queue, don't schedule immediately.
            // Also immediately schedule non-time based deliveries (for iOS)
            if ((Mode & OperatingMode.Queue) != OperatingMode.Queue || notification.DeliveryTime == null)
            {
                _platform?.ScheduleNotification(notification);
            }
            else if (!notification.Id.HasValue)
            {
                // Generate an ID for items that don't have one (just so they can be identified later)
                notification.Id = Math.Abs(DateTime.Now.ToString("yyMMddHHmmssffffff").GetHashCode());
            }

            // Register pending notification
            var result = new PendingNotification(notification);
            PendingNotifications.Add(result);

            return result;
        }

        /// <summary>
        /// Cancels a scheduled notification.
        /// </summary>
        /// <param name="notificationId">The ID of the notification to cancel.</param>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/> has not been called.</exception>
        public void CancelNotification(int notificationId)
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            if (_platform == null)
            {
                return;
            }

            _platform.CancelNotification(notificationId);

            // Remove the cancelled notification from scheduled list
            var index = PendingNotifications.FindIndex(scheduledNotification =>
                scheduledNotification.Notification.Id == notificationId);

            if (index >= 0)
            {
                PendingNotifications.RemoveAt(index);
            }
        }

        /// <summary>
        /// Cancels all scheduled notifications.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/> has not been called.</exception>
        public void CancelAllNotifications()
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            if (_platform == null)
            {
                return;
            }

            _platform.CancelAllScheduledNotifications();

            PendingNotifications.Clear();
        }

        /// <summary>
        /// Dismisses a displayed notification.
        /// </summary>
        /// <param name="notificationId">The ID of the notification to dismiss.</param>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/> has not been called.</exception>
        public void DismissNotification(int notificationId)
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            _platform?.DismissNotification(notificationId);
        }

        /// <summary>
        /// Dismisses all displayed notifications.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/> has not been called.</exception>
        public void DismissAllNotifications()
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            _platform?.DismissAllDisplayedNotifications();
        }

        /// <summary>
        /// Event fired by <see cref="_platform"/> when a notification is received.
        /// </summary>
        private void OnNotificationReceived(IGameNotification deliveredNotification)
        {
            // Ignore for background messages (this happens on Android sometimes)
            if (!_inForeground)
            {
                return;
            }

            // Find in pending list
            int deliveredIndex = PendingNotifications.FindIndex(
                scheduledNotification => scheduledNotification.Notification.Id == deliveredNotification.Id);
            
            if (deliveredIndex >= 0)
            {
                OnLocalNotificationDelivered?.Invoke(PendingNotifications[deliveredIndex]);
                PendingNotifications.RemoveAt(deliveredIndex);
            }
        }

        // Clear foreground notifications and reschedule stuff from a file
        private void OnForegrounding()
        {
            PendingNotifications.Clear();
            _platform.OnForeground();

            // Deserialize saved items
            var notifications = JsonUtility.FromJson<List<SerializableNotification>>(PlayerPrefs.GetString("notifications"));

            // Foregrounding
            if ((Mode & OperatingMode.ClearOnForegrounding) == OperatingMode.ClearOnForegrounding)
            {
                // Clear on foregrounding
                _platform.CancelAllScheduledNotifications();

                // Only reschedule in reschedule mode, and if we loaded any items
                if (notifications == null || (Mode & OperatingMode.RescheduleAfterClearing) != OperatingMode.RescheduleAfterClearing)
                {
                    return;
                }

                // Reschedule notifications from deserialization
                foreach (var savedNotification in notifications)
                {
                    if (savedNotification.DeliveryTime > DateTime.Now)
                    {
                        var pendingNotification = ScheduleNotification(savedNotification.AsGameNotification(_platform));
                        
                        pendingNotification.Reschedule = true;
                    }
                }
            }
            else
            {
                // Just create PendingNotification wrappers for all deserialized items.
                // We're not rescheduling them because they were not cleared
                if (notifications == null)
                {
                    return;
                }

                foreach (var savedNotification in notifications)
                {
                    if (savedNotification.DeliveryTime > DateTime.Now)
                    {
                        PendingNotifications.Add(new PendingNotification(savedNotification.AsGameNotification(_platform)));
                    }
                }
            }
        }
    }
}
