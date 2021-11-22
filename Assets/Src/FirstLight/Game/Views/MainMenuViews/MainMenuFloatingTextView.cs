using System;
using UnityEngine;
using FirstLight.Game.Services;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using I2.Loc;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using TMPro;
using UnityEngine.Events;
using FirstLight.Game.Messages;
using FirstLight.Services;
using Quantum;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Creates Floating Text on the Main Menu which has an animation that auto plays spawned.
	/// </summary>
	public class MainMenuFloatingTextView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _floatingText;
		[SerializeField] private Animation _animation;

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