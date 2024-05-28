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
			Add(_viewHideButton = new ImageButton(OnViewHideClicked) { name = "view-hide-button" });
			_viewHideButton.AddToClassList(UssViewHideButton);
		}
		
		private void OnViewHideClicked()
		{
			_viewHideButton.ToggleInClassList(UssViewHideButtonShow);
			isPasswordField = !_viewHideButton.ClassListContains(UssViewHideButtonShow);
		}

		public new class UxmlFactory : UxmlFactory<PasswordFieldElement, UxmlTraits>
		{
		}
		
		public new class UxmlTraits : LocalizedTextField.UxmlTraits
		{
			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				((PasswordFieldElement) ve).isPasswordField = true;
			}
		}
	}
}