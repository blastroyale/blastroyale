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

		internal void StartAnimation(PlayableDirector director, float allowSkipBefore)
		{
			if (_currentAnimation != null && _currentAnimation.state == PlayState.Playing)
			{
				_currentAnimation.Stop();
			}

			_skippedCurrentAnimation = false;
			_currentAnimationSkippableBefore = Time.time + allowSkipBefore;
			_currentAnimation = director;
			_currentAnimation!.time = 0;
			_currentAnimation.Play();
		}

		internal bool Skip()
		{
			if (_currentAnimation == null
				|| _currentAnimation.state != PlayState.Playing
				|| !(_currentAnimationSkippableBefore > Time.time)
				|| _skippedCurrentAnimation) return false;


			_skippedCurrentAnimation = true;
			_currentAnimation.time += _currentAnimation.duration / 2;
			_currentAnimation.Evaluate();
			return true;
		}
	}
}