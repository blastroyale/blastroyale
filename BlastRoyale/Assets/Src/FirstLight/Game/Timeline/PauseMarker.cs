using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FirstLight.Game.Timeline
{
	/// <summary>
	/// Pause Markers are used to signal director pause action
	/// </summary>
	[DisplayName("Marker/PauseMarker")]
	[CustomStyle("DestinationMarker")]
	public class PauseMarker : Marker, INotification
	{
		public PropertyName id { get; }
	}
}