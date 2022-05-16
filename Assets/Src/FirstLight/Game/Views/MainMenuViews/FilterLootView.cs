using UnityEngine;
using UnityEngine.Events;
using Quantum;
using Sirenix.OdinInspector;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This script is used to filter items the player want to browse through when entering the Loot screen.
	/// </summary>
	public class FilterLootView : MonoBehaviour
	{
		[SerializeField] private GameIdGroup _slot;
		[SerializeField, Required] private Button _button;
		[SerializeField, Required] private NotificationGroupIdView _notificationGroupIdView;

		/// <summary>
		/// Triggered when the button is clicked and passing the <see cref="GameIdGroup"/> slot referencing the button
		/// </summary>
		public UnityEvent<GameIdGroup> OnClick = new UnityEvent<GameIdGroup>();

		protected void Awake()
		{
			_button.onClick.AddListener(OnButtonClick);
		}
		
		/// <summary>
		/// Sets this notification state when the Loot Screen Presenter is opened.
		/// </summary>
		public void SetNotificationState()
		{
			_notificationGroupIdView.UpdateState();
		}

		protected virtual void OnButtonClick()
		{
			OnClick.Invoke(_slot);
		}
	}
}