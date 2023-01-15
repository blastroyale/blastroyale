using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays the password field
	/// </summary>
	public class PasswordFieldElement : LocalizedTextField
	{
		private const string UssViewHideButton = "view-hide-button";
		private const string UssViewHideButtonShow = UssViewHideButton + "--show";

		private readonly ImageButton _viewHideButton;

		public PasswordFieldElement()
		{
			RegisterCallback(new EventCallback<AttachToPanelEvent>(OnAttachToPanel));
			Add(_viewHideButton = new ImageButton(OnViewHideClicked) { name = "view-hide-button" });
			_viewHideButton.AddToClassList(UssViewHideButton);
		}

		private void OnAttachToPanel(AttachToPanelEvent evt)
		{
			isPasswordField = true;
		}

		private void OnViewHideClicked()
		{
			_viewHideButton.ToggleInClassList(UssViewHideButtonShow);
			isPasswordField = !_viewHideButton.ClassListContains(UssViewHideButtonShow);
		}

		public new class UxmlFactory : UxmlFactory<PasswordFieldElement, UxmlTraits>
		{
		}
	}
}