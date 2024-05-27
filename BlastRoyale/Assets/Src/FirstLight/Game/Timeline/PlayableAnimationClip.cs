using System;
using UnityEngine;
using UnityEngine.Playables;

namespace FirstLight.Game.TimelinePlayables
{
	/// <summary>
	/// This Mono Component that is attached to a playable tp play an animation clip.
	/// </summary>
	[Serializable]
	public class PlayableAnimationClip : PlayableBehaviour
	{
		[SerializeField] private Animation _animation;
		[SerializeField] private AnimationClip _clip;

#if UNITY_EDITOR
		/// <inheritdoc />
		public override void ProcessFrame(Playable playable, FrameData info, object playerData)
		{
			if (!Application.isPlaying && _animation != null && _clip != null)
			{
				_clip.SampleAnimation(_animation.gameObject, (float) playable.GetTime());
			}
		}
#endif
		
		/// <inheritdoc />
		public override void OnBehaviourPlay(Playable playable, FrameData info)
		{
			_animation.clip = _clip;
			_animation.Play();
		}

		/// <summary>
		/// Set's this <see cref="PlayableBehaviour"/> <paramref name="animation"/> and <paramref name="clip"/> data
		/// </summary>
		public void SetAnimationData(Animation animation, AnimationClip clip)
		{
			_animation = animation;
			_clip = clip;
		}
	}
}