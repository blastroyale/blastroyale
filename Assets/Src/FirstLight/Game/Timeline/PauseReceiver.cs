using UnityEngine;
using UnityEngine.Playables;

namespace FirstLight.Game.Timeline
{
	/// <summary>
	/// This class listens for a <see cref="PauseMarker"/> notification and pauses the Timeline back to the specified point of that marker.
	/// </summary>
	public class PauseReceiver : MonoBehaviour, INotificationReceiver
	{
		/// <inheritdoc />
		public void OnNotify(Playable origin, INotification notification, object context)
		{
			if (!(notification is PauseMarker))
			{
				return;
			}
			
			var graph = origin.GetGraph();
			
			graph.Stop();
			graph.GetRootPlayable(0).SetSpeed(0);
		}
	}
}