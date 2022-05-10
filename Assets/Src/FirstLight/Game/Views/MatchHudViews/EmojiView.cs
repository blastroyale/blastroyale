using System;
using FirstLight.Game.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// This view displays the player sent emoji
	/// </summary>
	public class EmojiView : MonoBehaviour
	{
		[SerializeField, Required] private Animation _animation;
		[SerializeField, Required] private Image _image;
		
		private Action _despawner;
		
		private void OnValidate()
		{
			_animation = _animation ? _animation : GetComponent<Animation>();
			_image = _image ? _image : GetComponent<Image>();
		}

		/// <summary>
		/// Initializes this view state.
		/// To set the data call <see cref="SetInfo"/> instead
		/// </summary>
		public void Init(Action despawner)
		{
			_despawner = despawner;
		}
		
		/// <summary>
		/// Setup the display emoji with the given <paramref name="sprite"/>
		/// </summary>
		public void SetInfo(Sprite sprite)
		{
			_image.sprite = sprite;
			
			_animation.Rewind();
			_animation.Play();
			
			this.LateCall(_animation.clip.length, _despawner.Invoke);
		}
	}
}