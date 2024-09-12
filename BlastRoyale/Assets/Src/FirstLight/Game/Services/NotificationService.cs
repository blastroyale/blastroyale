using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Configs.Remote.FirstLight.Game.Configs.Remote;
using FirstLight.Game.Configs.Utils;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services.Analytics.Events;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using FirstLightServerSDK.Services;
using Unity.Notifications;
using Unity.Services.PushNotifications;
using UnityEngine;

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

		public NotificationService(IRemoteConfigProvider remoteConfigProvider, IMessageBrokerService msgBroker)
		{
			_remoteConfigProvider = remoteConfigProvider;
			msgBroker.Subscribe<SuccessAuthentication>((_) =>
			{
				RefreshEventNotifications();
			});
		}

		public void Init()
		{
			var args = NotificationCenterArgs.Default;
			args.AndroidChannelId = "default";
			args.AndroidChannelName = "Notifications";
			args.AndroidChannelDescription = "Main notifications";
			NotificationCenter.Initialize(args);
#if UNITY_ANDROID
			Unity.Notifications.Android.AndroidNotificationCenter.RegisterNotificationChannel(new Unity.Notifications.Android.AndroidNotificationChannel()
			{
				Id = "events",
				Name = "Events",
				Importance = Unity.Notifications.Android.Importance.Default,
				Description = "Upcoming events",
			});
#endif
		}

		public void RefreshEventNotifications()
		{
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
		}

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
				ScheduleNotification(timeOfNotification, "events", title, desc);
			}
		}

		private string ReplaceValue(string original, DurationConfig duration, EventGameModeEntry @event)
		{
			var startsAtDate = duration.GetStartsAtDateTime();
			var startsAt = startsAtDate.Minute == 0 ? startsAtDate.ToString("hh tt") : startsAtDate.ToString("hh:mm tt");

			return original
				.Replace("%event_title%", @event.Title.GetText())
				.Replace("%event_description%", @event.Description.GetText())
				.Replace("%event_long_description%", @event.LongDescription.GetText())
				.Replace("%starts_at%", startsAt);
		}

		public void ScheduleNotification(DateTime dateTime, string category, string title, string body)
		{
			FLog.Info("Scheduled notification " + title + " for " + dateTime.ToString(CultureInfo.InvariantCulture));
			var a = NotificationCenter.ScheduleNotification(new Notification()
			{
				Title = title,
				Text = body,
				ShowInForeground = true,
			}, category, new NotificationDateTimeSchedule(dateTime));
		}

		public async UniTask RegisterForNotifications()
		{
			Init();
			await InitRemotePushNotifications();
			var request = NotificationCenter.RequestPermission();

			while (request.Status == NotificationsPermissionStatus.RequestPending)
			{
				await UniTask.Yield();
			}

			Debug.Log("Permission result: " + request.Status);
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
	}
}