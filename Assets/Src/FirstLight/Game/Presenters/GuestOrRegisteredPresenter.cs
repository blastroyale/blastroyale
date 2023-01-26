using System;
using FirstLight.Game.Services;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles showing the GuestOrRegistered screen
	/// </summary>
	[LoadSynchronously]
	public class GuestOrRegisteredPresenter : UiToolkitPresenterData<GuestOrRegisteredPresenter.StateData>
	{
		public struct StateData
		{
			public Action GoToLoginClicked;
			public Action PlayAsGuestClicked;
		}

		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements(VisualElement root)
		{
			root.Q<Button>("RegisterLoginButton").clicked += OnLoginRegisterClicked;
			root.Q<Button>("GuestButton").clicked += OnPlayAsGuestButtonClicked;
			root.SetupClicks(_services);
		}
		
		private void OnLoginRegisterClicked()
		{
			Data.GoToLoginClicked();
		}

		private void OnPlayAsGuestButtonClicked()
		{
			_services.AnalyticsService.UiCalls.ButtonAction(UIAnalyticsButtonsNames.PlayAsGuest);
			Data.PlayAsGuestClicked();
		}
	}
}