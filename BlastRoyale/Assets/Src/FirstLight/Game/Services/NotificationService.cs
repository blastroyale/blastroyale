#if UNITY_IOS || UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.Globalization;
using FirstLight.FLogger;
using FirstLight.Game.Configs.Utils;
using FirstLight.Game.Utils;
using Unity.Notifications;
using Unity.Services.PushNotifications;
using FirstLight.Game.Configs.Remote;
using UnityEngine;
#endif
using FirstLight.Game.Messages;
using FirstLightServerSDK.Services;
using FirstLight.SDK.Services;
using Cysharp.Threading.Tasks;

namespace FirstLight.Game.Services
{
	public interface INotificationService
	{
		public UniTask RegisterForNotifications();
		public void RefreshEventNotifications();
	}

	public class NotificationService : INotificationService
	{
		private IRemoteConfigProvider _remoteConfigProvider;
		private bool _initialized = false;

		public NotificationService(IRemoteConfigProvider remoteConfigProvider, IMessageBrokerService msgBroker)
		{
			_remoteConfigProvider = remoteConfigProvider;
			msgBroker.Subscribe<SuccessfullyAuthenticated>((msg) =>
			{
				if (msg.PreviouslyLoggedIn) return;
				RefreshEventNotifications();
			});
		}

		public void Init()
		{
#if UNITY_IOS || UNITY_ANDROID
			_initialized = true;
			var args = NotificationCenterArgs.Default;
			args.AndroidChannelId = "default";
			args.AndroidChannelName = "Notifications";
			args.AndroidChannelDescription = "Main notifications";
			NotificationCenter.Initialize(args);
#if UNITY_ANDROID
			Unity.Notifications.Android.AndroidNotificationCenter.RegisterNotificationChannel(
				new Unity.Notifications.Android.AndroidNotificationChannel()
				{
					Id = "events",
					Name = "Events",
					Importance = Unity.Notifications.Android.Importance.Default,
					Description = "Upcoming events",
				});
#endif
#endif
		}

		public void RefreshEventNotifications()
		{
			if (!_initialized) return;
#if UNITY_IOS || UNITY_ANDROID
			if (Application.isEditor) return;
			NotificationCenter.CancelAllScheduledNotifications();
			var eventConfig = _remoteConfigProvider.GetConfig<EventGameModesConfig>();
			var notificationConfig = _remoteConfigProvider.GetConfig<EventNotificationConfig>();

			var now = DateTime.UtcNow;
			foreach (var @event in eventConfig)
			{
				foreach (var duration in @event.Schedule)
				{
					if (duration.GetStartsAtDateTime() > now)
					{
						var dif = (duration.GetStartsAtDateTime() - now).TotalHours;
						if (dif > notificationConfig.ScheduleHoursBefore)
						{
							break;
						}

						AddEventNotifications(now, notificationConfig, @event, duration);
					}
				}
			}
#endif
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async UniTask RegisterForNotifications()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
#if UNITY_IOS || UNITY_ANDROID
			if (Application.isEditor) return;
			Init();
			await InitRemotePushNotifications();
			var request = NotificationCenter.RequestPermission();

			while (request.Status == NotificationsPermissionStatus.RequestPending)
			{
				await UniTask.Yield();
			}

			Debug.Log("Permission result: " + request.Status);
#endif
		}

#if UNITY_IOS || UNITY_ANDROID
		private void AddEventNotifications(DateTime now,
										   EventNotificationConfig notificationConfig,
										   EventGameModeEntry @event,
										   DurationConfig duration
		)
		{
			foreach (var notificationSchedule in notificationConfig.Notifications)
			{
				var timeOfNotification = duration.GetStartsAtDateTime() - TimeSpan.FromMinutes(notificationSchedule.TimeBefore);
				if (timeOfNotification < now)
				{
					continue;
				}

				var randomMessage = notificationSchedule.Messages.RandomElement();

				var title = ReplaceValue(randomMessage.Title, duration, @event);
				var desc = ReplaceValue(randomMessage.Description, duration, @event);
				ScheduleNotification(timeOfNotification, "events", title, desc, notificationConfig.ShowWhenAppIsOpen);
			}
		}

		private string ReplaceValue(string original, DurationConfig duration, EventGameModeEntry @event)
		{
			var startsAtDate = duration.GetStartsAtDateTime().ToLocalTime();
			var startsAt = startsAtDate.Minute == 0 ? startsAtDate.ToString("hh tt") : startsAtDate.ToString("hh:mm tt");

			return original
				.Replace("%event_title%", @event.Title.GetText())
				.Replace("%event_description%", @event.Description.GetText())
				.Replace("%event_long_description%", @event.LongDescription.GetText())
				.Replace("%starts_at%", startsAt);
		}

		public void ScheduleNotification(DateTime dateTime, string category, string title, string body, bool showInForeGround)
		{
			if (Application.isEditor) return;

			FLog.Info("Scheduled notification " + title + " for " + dateTime.ToString(CultureInfo.InvariantCulture));
			var a = NotificationCenter.ScheduleNotification(new Notification()
			{
				Title = title,
				Text = body,
				ShowInForeground = showInForeGround,
			}, category, new NotificationDateTimeSchedule(dateTime));
		}

		private async UniTask InitRemotePushNotifications()
		{
			if (Application.isEditor) return;

			PushNotificationsService.Instance.OnRemoteNotificationReceived += PushNotificationReceived;

			try
			{
				var token = await PushNotificationsService.Instance.RegisterForPushNotificationsAsync().AsUniTask();
				FLog.Info($"Registered for push notifications with token: {token}");
			}
			catch (Exception e)
			{
				FLog.Warn("Failed to register for push notifications: ", e);
			}

			return;

			// Only for testing for now
			void PushNotificationReceived(Dictionary<string, object> notificationData)
			{
				FLog.Info("Notification received!");
				foreach (var (key, value) in notificationData)
				{
					FLog.Info($"Notification data item: {key} - {value}");
				}
			}
		}
#endif
	}
}