using UnityEngine;
using UnityEngine.Playables;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Controls animation skipping in the rewards screen
	/// </summary>
	public class RewardsAnimationController
	{
		private PlayableDirector _currentAnimation;
		private float _currentAnimationSkippableBefore;
		private bool _skippedCurrentAnimation;
		private float _skipToTime;

		internal void StartAnimation(PlayableDirector director, float allowSkipBefore, float skipToTime = 0, float startTime = 0)
		{
			if (_currentAnimation != null && _currentAnimation.state == PlayState.Playing)
			{
				_currentAnimation.Stop();
			}

			_skipToTime = skipToTime;
			_skippedCurrentAnimation = false;
			_currentAnimationSkippableBefore = Time.time + allowSkipBefore;
			_currentAnimation = director;
			_currentAnimation.time = startTime;
			_currentAnimation.Play();
		}

		internal bool Skip()
		{
			if (_currentAnimation == null
				|| _currentAnimation.state != PlayState.Playing
				|| !(_currentAnimationSkippableBefore > Time.time)
				|| _currentAnimation.time > _skipToTime
				|| _skippedCurrentAnimation) return false;

			_skippedCurrentAnimation = true;
			_currentAnimation.time = _skipToTime > 0 ? _skipToTime : _currentAnimation.duration * 0.8f;
			_currentAnimation.Evaluate();
			return true;
		}
	}
}