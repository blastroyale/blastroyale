using System;

// ReSharper disable once CheckNamespace

namespace FirstLight.NotificationService
{
	/// <summary>
	/// Notification to serialize/deserialize to disk when the game goes foreground
	/// </summary>
	[Serializable]
	internal struct SerializableNotification
	{
		public int? Id;
		public string Title;
		public string Body;
		public string Subtitle;
		public string Channel;
		public int? BadgeNumber;
		public DateTime? DeliveryTime;
	}

	/// <summary>
	/// Converter serialization classes
	/// </summary>
	internal static class SerializableNotificationConverter
	{
		public static IGameNotification AsGameNotification(this SerializableNotification serializableNotification, 
			IGameNotificationsPlatform platform)
		{
			var notification = platform.CreateNotification();

			notification.Id = serializableNotification.Id;
			notification.Title = serializableNotification.Title;
			notification.Body = serializableNotification.Body;
			notification.Subtitle = serializableNotification.Subtitle;
			notification.Channel = serializableNotification.Channel;
			notification.BadgeNumber = serializableNotification.BadgeNumber;
			notification.DeliveryTime = serializableNotification.DeliveryTime;

			return notification;
		}
        
		public static SerializableNotification AsSerializableNotification(this PendingNotification pendingNotification)
		{
			return new SerializableNotification
			{
				Id = pendingNotification.Notification.Id,
				Title = pendingNotification.Notification.Title,
				Body = pendingNotification.Notification.Body,
				Subtitle = pendingNotification.Notification.Subtitle,
				Channel = pendingNotification.Notification.Channel,
				BadgeNumber = pendingNotification.Notification.BadgeNumber,
				DeliveryTime = pendingNotification.Notification.DeliveryTime,
			};
		}
	}

}