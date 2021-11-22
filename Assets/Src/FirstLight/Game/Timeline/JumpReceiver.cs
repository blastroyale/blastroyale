using UnityEngine;
using UnityEngine.Playables;

namespace FirstLight.Game.TimelinePlayables
{
	/// <summary>
	/// Jump Recievers are added to Timelines in conjunction with <cref="JumpMarker"/> objects.
	/// This class listens for a JumpMarker notification and sets the Timeline back to the specified point of that marker.
	/// </summary>
	public class JumpReceiver : MonoBehaviour, INotificationReceiver
	{
		/// <inheritdoc />
		public void OnNotify(Playable origin, INotification notification, object context)
		{
			var jumpMarker = notification as JumpMarker;
			if (jumpMarker == null) return;

			var destinationMarker = jumpMarker.destinationMarker;
			if (destinationMarker != null && destinationMarker.active)
			{
				var timelinePlayable = origin.GetGraph().GetRootPlayable(0);
				timelinePlayable.SetTime(destinationMarker.time);
			}
		}
	}
}


