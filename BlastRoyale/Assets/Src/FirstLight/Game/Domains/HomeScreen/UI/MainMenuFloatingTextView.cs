﻿using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Domains.HomeScreen.UI
{
	/// <summary>
	/// Creates Floating Text on the Main Menu which has an animation that auto plays spawned.
	/// </summary>
	public class MainMenuFloatingTextView : MonoBehaviour
	{
		[SerializeField, Required] private TextMeshProUGUI _floatingText;
		[SerializeField, Required] private Animation _animation;

		/// <summary>
		/// Requests the Floating text animation play lenght
		/// </summary>
		public float AnimationLength => _animation.clip.length;
		
		/// <summary>
		/// Sets this notification state when the Loot Screen Presenter is opened.
		/// </summary>
		public void SetText(string text)
		{
			_floatingText.text = text;

			_animation.Rewind();
			_animation.Play();
		}
	}
}