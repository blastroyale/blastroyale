using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent
{
	/// <summary>
	/// This Mono component is responsible for player a legacy animation using unity event hooks.
	/// </summary>
	public class AnimationBindMonoComponent : MonoBehaviour
	{
		[SerializeField, Required] private Animation _animation;

		/// <summary>
		/// Rewind and play legacy animation component
		/// </summary>
		public void Play()
		{
			_animation.Rewind();
			_animation.Play();
		}
	}
}