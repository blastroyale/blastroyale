using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// This view displays floating text and plays a legacy animation.
	/// </summary>
	public class FloatingTextView : MonoBehaviour
	{
		[SerializeField] private Animation _animation;
		[SerializeField] private TextMeshProUGUI _text;
		
		private void OnValidate()
		{
			_animation = _animation ? _animation : GetComponent<Animation>();
		}
		
		/// <summary>
		/// Plays the floating text animation with the given <paramref name="clip"/> and information
		/// </summary>
		public void Play(string text, Color color, AnimationClip clip)
		{
			_text.text = text;
			_text.color = color;
			_animation.clip = clip;
			
			_animation.Rewind();
			_animation.Play();
		}
	}
}