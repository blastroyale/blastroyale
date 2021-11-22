using System.Linq;
using Coffee.UIEffects;
using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This View is used on buttons that have different visuals to possible visible states 
	/// </summary>
	public class VisualStateButtonView : MonoBehaviour
	{
		[SerializeField] private Image _image;
		[SerializeField] private UiButtonView _button;
		[SerializeField] private Sprite _notificationOnSprite;
		[SerializeField] private Sprite _notificationOffSprite;
		[SerializeField] private NotificationViewBase[] _notifications;
		[SerializeField] private UIShiny _shiny;
		[SerializeField] private Animation _stateAnimation;
		
		/// <summary>
		/// Requests the <see cref="Button"/> referencing this view on the PlayScreen
		/// </summary>
		public Button Button => _button;
		
		/// <summary>
		/// Checks if this button is currently emphasized in the UI
		/// </summary>
		public bool IsEmphasized => _image.sprite == _notificationOnSprite;

		/// <summary>
		/// Checks if the button is supposed to be in a shiny state
		/// </summary>
		public bool IsShinyState => _image.color == _button.colors.normalColor &&
		                            _notifications.Any(notification => notification.State);

		private void Awake()
		{
			_shiny.enabled = false;
			
			foreach (var notification in _notifications)
			{
				notification.SetNotificationState(false, false);
			}

			OnAwake();
		}

		/// <summary>
		/// Plays the unlocked state animation of the button
		/// </summary>
		public void PlayUnlockedStateAnimation()
		{
			_stateAnimation.Play();

			if (_notifications.Length > 0)
			{
				_notifications[0].SetNotificationState(true);
			}
		}

		/// <summary>
		/// Sets the shiny buttons to the given shiny state
		/// </summary>
		public void UpdateShinyState()
		{
			_shiny.Stop();
			_shiny.enabled = IsShinyState;
		}
		
		/// <summary>
		/// Updates the current visual state of this button
		/// </summary>
		public void UpdateState(bool isUnlocked, bool isNew, bool emphasize)
		{
			_image.color = isUnlocked ? _button.colors.normalColor : _button.colors.disabledColor;
			
			if (!isUnlocked)
			{
				return;
			}

			foreach (var notification in _notifications)
			{
				notification.UpdateState();
			}
			
			if (_notifications.Length > 0)
			{
				if (isNew && !_notifications[0].State)
				{
					_notifications[0].SetNotificationState(true);
				}

				if (_notifications[0].NotificationText.text == "0")
				{
					_notifications[0].NotificationText.SetText("!");
				}
			}
			
			_image.sprite = emphasize ? _notificationOnSprite : _notificationOffSprite;
		}

		protected virtual void OnAwake() { }
	}
}

