using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays the password field
	/// </summary>
	public class PasswordFieldElement : LocalizedTextField
	{
		private const string UssViewHideButton = "view-hide-button";

		private readonly ImageButton _viewHideButton;

		public PasswordFieldElement()
		{
			Add(_viewHideButton = new ImageButton(OnViewHideClicked) {name = "view-hide-button"});
			_viewHideButton.AddToClassList(UssViewHideButton);
			_viewHideButton.clicked += OnViewHideClicked;
		}
		
		private void OnViewHideClicked()
		{
			_viewHideButton.ToggleInClassList("view-hide-button--show");
			isPasswordField = !_viewHideButton.ClassListContains("view-hide-button--show");
		}

		public new class UxmlFactory : UxmlFactory<PasswordFieldElement, UxmlTraits>
		{
		}
	}
}