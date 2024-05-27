using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FirstLight.Game.TimelinePlayables
{
	/// <summary>
	/// Jump Markers are added to Timelines in conjunction with <cref="JumpReciever"/> objects.
	/// This class allows the timeline to jump to a specific point in the Timeline, in case we want to repeat specific sequences or skip past sections of it.
	/// </summary>
	[DisplayName("Jump/JumpMarker")]
	[CustomStyle("JumpMarker")]
	public class JumpMarker : Marker, INotification, INotificationOptionProvider
	{
		[SerializeField] public DestinationMarker destinationMarker;
		[SerializeField] public bool emitOnce;
		[SerializeField] public bool emitInEditor;

		public PropertyName id { get; }

		NotificationFlags INotificationOptionProvider.flags => (emitOnce ? NotificationFlags.TriggerOnce : default) | (emitInEditor ? NotificationFlags.TriggerInEditMode : default);
	}
}
