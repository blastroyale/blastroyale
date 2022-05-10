using System.Threading.Tasks;
using FirstLight.Game.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using TMPro;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// A single entry for a Dynamic Message played after a Quantum Event is received.
	/// </summary>
	public class DynamicMessageEntryView : MonoBehaviour
	{
		[SerializeField, Required] private Animation _animation;
		[SerializeField, Required] private AnimationClip _introClip;
		[SerializeField, Required] private AnimationClip _outroClip;
		[SerializeField, Required] private TextMeshProUGUI _textTop;
		[SerializeField, Required] private TextMeshProUGUI _textBottom;

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