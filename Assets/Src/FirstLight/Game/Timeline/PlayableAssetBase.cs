using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FirstLight.Game.Timeline
{
	/// <summary>
	/// This defines the basic contract of a Playable asset with a clip as a property
	/// </summary>
	public interface IPlayableAssetBase
	{
		/// <summary>
		/// The clip ruling the playable to be played in the timeline
		/// </summary>
		public TimelineClip CustomClipReference { get; set; }
	}
	
	/// <summary>
	/// This creates a Playable in the timeline of <typeparamref name="T"/> behaviour type.
	/// Implement this class for your custom Playable implementation
	/// </summary>
	public abstract class PlayableAssetBase<T> : PlayableAsset, IPlayableAssetBase where T : PlayableBehaviourBase, new()
	{
		public T Template;
		
		/// <inheritdoc />
		public TimelineClip CustomClipReference { get; set; }

		/// <inheritdoc />
		public override Playable CreatePlayable (PlayableGraph graph, GameObject owner) 
		{
			var playable =  OnCreated(graph, owner);

			playable.GetBehaviour().CustomClipReference = CustomClipReference;

			return playable;
		}

		protected virtual ScriptPlayable<T> OnCreated(PlayableGraph graph, GameObject owner)
		{
			return  ScriptPlayable<T>.Create(graph, Template);
		}
	}
}