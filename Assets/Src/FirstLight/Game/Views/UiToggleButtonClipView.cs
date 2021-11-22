using System.Collections;
using DG.Tweening;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
			_gameService.AudioFxService.PlayClip2D(TapSoundFx);
			
			Animation.clip = isOn ? Clip : ToggleOnPressedClip;
			Animation.Rewind(); 
			Animation.Play();
		}
	}
}