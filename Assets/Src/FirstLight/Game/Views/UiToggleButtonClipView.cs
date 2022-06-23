using FirstLight.Game.Ids;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace FirstLight.UiService
{
	/// <summary>
	/// Custom extension of UiToggleButtonView class that plays a click
	/// animation when selected.
	/// </summary>
	/// 
	public class UiToggleButtonClipView : UiToggleButtonView
	{
		// Animation clip to play when button is already selected
		public AnimationClip Clip;

		/// <inheritdoc />
		protected override void OnClick()
		{
			_gameService.AudioFxService.PlayClip2D(AudioId.ButtonClickForward);
			
			Animation.clip = isOn ? Clip : ToggleOnPressedClip;
			Animation.Rewind(); 
			Animation.Play();
		}
	}
}