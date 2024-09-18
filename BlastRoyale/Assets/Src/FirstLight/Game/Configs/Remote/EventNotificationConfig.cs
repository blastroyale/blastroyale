using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace FirstLight.Game.Configs.Remote
{
	[Serializable]
	public class EventMessage
	{
		public string Title;
		public string Description;
	}

	[Serializable]
	public class EventNotification
	{
		public int TimeBefore;
		public EventMessage[] Messages;
	}

	[Serializable]
	public class EventNotificationConfig
	{
		public bool ShowWhenAppIsOpen;
		public int ScheduleHoursBefore;
		public EventNotification[] Notifications;
	}
}