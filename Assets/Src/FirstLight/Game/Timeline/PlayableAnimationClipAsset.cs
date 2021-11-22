using UnityEngine;
using UnityEngine.Playables;

namespace FirstLight.Game.TimelinePlayables
{
	/// <summary>
	/// This playable asset that can be placed on a timeline.
	/// </summary>
	[System.Serializable]
	public class PlayableAnimationClipAsset : PlayableAsset
	{
		[SerializeField] private ExposedReference<Animation> _animation;
		[SerializeField] private AnimationClip _clip;

		/// <inheritdoc />
		public override double duration => _clip != null ? _clip.length : base.duration;

		/// <inheritdoc />
		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			var playable = ScriptPlayable<PlayableAnimationClip>.Create(graph);
			var animation = _animation.Resolve(graph.GetResolver());
			
			playable.GetBehaviour().SetAnimationData(animation, _clip);
			
			return playable;
		}
	}
}