using FirstLight.UiService;
namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles showing the login screen background. It's only purpose is to render the background image while user goes from login screen to register screen and vice versa.
	/// </summary>
	[LoadSynchronously]
	public class LoginScreenBackgroundPresenter : UiToolkitPresenterData<LoginScreenBackgroundPresenter.StateData>
	{
		public struct StateData
		{
		}
	}
}