using UnityEngine.Timeline;

namespace FirstLight.Game.Timeline
{
	/// <inheritdoc />
	/// <remarks>
	/// The track for the any behaviour to be used in the Ftue
	/// </remarks>
	[TrackClipType(typeof(TalkingHeadAsset)), TrackClipType(typeof(GenericVideoAsset)), TrackClipType(typeof(GameObjectAsset)),
	 TrackClipType(typeof(GuidElementAsset)), TrackClipType(typeof(UiPresenterAsset))]
	public class FtueTrack : PlayableTrackAssetBase<PlayableMixerBehaviourBase>
	{
	}
}