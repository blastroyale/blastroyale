using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Utils;
using FirstLight.Services;
using UnityEngine;
using TMPro;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// A single entry for a Dynamic Message played after a Quantum Event is received.
	/// </summary>
	public class DynamicMessageEntryView : MonoBehaviour
	{
		[SerializeField] private Animation _animation;
		[SerializeField] private AnimationClip _introClip;
		[SerializeField] private AnimationClip _outroClip;
		[SerializeField] private TextMeshProUGUI _textTop;
		[SerializeField] private TextMeshProUGUI _textBottom;

		/// <summary>
		/// Sets the Text on this dynamic message.
		/// </summary>
		public Task DisplayMessage(string topText, string bottomText)
		{
			_textTop.text = topText;
			_textBottom.text = bottomText;

			PlayAnimation(_introClip);
			
			return this.LateCallAwaitable(_animation.clip.length, () => PlayAnimation(_outroClip));
		}

		private void PlayAnimation(AnimationClip clip)
		{
			_animation.clip = clip;
			_animation.Rewind();
			_animation.Play();
		}
	}
}