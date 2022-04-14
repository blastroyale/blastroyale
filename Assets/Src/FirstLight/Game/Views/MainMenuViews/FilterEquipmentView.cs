using Quantum;
using UnityEngine;
using UnityEngine.Events;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This script is used to filter items the player want to browse through when entering a screen with equipment.
	/// </summary>
	[RequireComponent(typeof(Button))]
	public class FilterEquipmentView : MonoBehaviour
	{
		[SerializeField] private GameIdGroup _slot;
		[SerializeField] private Button _button;

		/// <summary>
		/// Triggered when the button is clicked and passing the <see cref="GameIdGroup"/> slot referencing the button
		/// </summary>
		public UnityEvent<GameIdGroup> OnClick = new UnityEvent<GameIdGroup>();

		private void OnValidate()
		{
			_button ??= GetComponent<Button>();
		}

		protected void Start()
		{
			_button.onClick.AddListener(OnButtonClick);
		}

		/// <summary>
		/// Set's the button to the given selection <paramref name="slotSelected"/>
		/// </summary>
		public void SetSelectedState(GameIdGroup slotSelected)
		{
			_button.image.sprite = slotSelected == _slot ? _button.spriteState.selectedSprite : _button.spriteState.pressedSprite;
		}

		protected virtual void OnButtonClick()
		{
			OnClick.Invoke(_slot);
		}
	}
}