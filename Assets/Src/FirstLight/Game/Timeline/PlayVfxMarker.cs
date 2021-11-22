using System.ComponentModel;
using FirstLight.Game.Ids;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FirstLight.Game.TimelinePlayables
{
	/// <summary>
	/// PlayVfxMarker Markers are used to signal spawm of vfx assets
	/// </summary>
	/// </summary>
	[DisplayName("Vfx/PlayVfxMarker")]
	[CustomStyle("DestinationMarker")]
	public class PlayVfxMarker : Marker, INotification
	{
		[SerializeField] public bool createAtPlayerPosition;
		[SerializeField] public VfxId _vfxId;
		
		public PropertyName id { get; }

		private void Reset()
		{
			createAtPlayerPosition = true;
		}
	}
}