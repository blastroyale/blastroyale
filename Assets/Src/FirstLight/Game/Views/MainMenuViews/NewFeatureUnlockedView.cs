using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Utils;
using I2.Loc;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Plays an animated message informing the player of new game features that have been unlocked on the
	/// Play Screen presenter.
	/// </summary>
	public class NewFeatureUnlockedView : MonoBehaviour
	{
		[SerializeField, Required] private TextMeshProUGUI _unlockedText;
		[SerializeField, Required] private Animation _animation;

		private readonly Queue<KeyValuePair<UnlockSystem, Action<UnlockSystem>>> _queue = 
			new Queue<KeyValuePair<UnlockSystem, Action<UnlockSystem>>>();

		/// <summary>
		/// Queue a new system pop up information. Will trigger <paramref name="callback"/> when the pop up is shown
		/// </summary>
		public void QueueNewSystemPopUp(UnlockSystem system, Action<UnlockSystem> callback)
		{
			_queue.Enqueue(new KeyValuePair<UnlockSystem, Action<UnlockSystem>>(system, callback));

			if (_queue.Count == 1)
			{
				gameObject.SetActive(true);
				PopUnlockedAnimation();
			}
		}
		
		private void PopUnlockedAnimation()
		{
			var pair = _queue.Peek();
			
			_unlockedText.text = string.Format(ScriptLocalization.MainMenu.NewFeatureUnlocked, pair.Key.ToString().ToUpper());

			_animation.Rewind();
			_animation.Play();
			pair.Value(pair.Key);
			
			this.LateCall(_animation.clip.length, PopNext);
		}

		private void PopNext()
		{
			_queue.Dequeue();
			
			if (_queue.Count > 0)
			{
				PopUnlockedAnimation();
			}
			else
			{
				gameObject.SetActive(false);
			}
		}
	}
}

