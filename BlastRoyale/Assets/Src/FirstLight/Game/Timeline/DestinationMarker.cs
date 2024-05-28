using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FirstLight.Game.TimelinePlayables
{
	/// <summary>
	/// Destination Markers are added to Timelines in conjunction with <cref="JumpMarker"/> objects.
	/// This class allows the timeline to jump to a specific point in the Timeline, in case we want to repeat specific sequences or skip past sections of it.
	/// </summary>
	/// </summary>
	[DisplayName("Jump/DestinationMarker")]
	[CustomStyle("DestinationMarker")]
	public class DestinationMarker : Marker, INotification
	{
		[SerializeField] public bool active;

		public PropertyName id { get; }

		private void Reset()
		{
			active = true;
		}
	}
}

