using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FirstLight.Game.Timeline
{
	/// <summary>
	/// This creates a track in the timeline of <typeparamref name="T"/> behaviour type.
	/// Implement this class for your custom track implementation
	/// </summary>
	[Serializable]
	public abstract class PlayableTrackAssetBase<T> : TrackAsset where T : PlayableMixerBehaviourBase, new()
	{
		public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) 
		{
			var mixer = OnCreated(graph, go, inputCount);
		
			foreach (var clip in GetClips())
			{
				ProcessClip(clip);
			}
			
			return mixer;
		}

		protected virtual Playable OnCreated(PlayableGraph graph, GameObject go, int inputCount)
		{
			return ScriptPlayable<T>.Create(graph, inputCount);
		}

		protected virtual void ProcessClip(TimelineClip clip)
		{
			if (clip.asset is IPlayableAssetBase behaviourBase)
			{
				behaviourBase.CustomClipReference = clip;
			}
		}
	}
}