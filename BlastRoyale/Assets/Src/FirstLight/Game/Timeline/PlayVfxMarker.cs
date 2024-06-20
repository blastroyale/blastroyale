using System.ComponentModel;
using FirstLight.Game.Ids;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;
using UnityEngine.Timeline;

namespace FirstLight.Game.Timeline
{
	/// <summary>
	/// PlayVfxMarker Markers are used to signal spawm of vfx assets
	/// </summary>
	/// </summary>
	[DisplayName("Vfx/PlayVfxMarker")]
	[CustomStyle("DestinationMarker")]
	public class PlayVfxMarker : Marker, INotification
	{
		[SerializeField] private EnumSelector<VfxId> _vfxId;
		
		public PropertyName id { get; }
		public VfxId Vfx => _vfxId;
	}
}