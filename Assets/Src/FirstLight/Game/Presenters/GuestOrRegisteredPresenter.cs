using System;
using FirstLight.Game.Services;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.UIService;
using I2.Loc;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles showing the GuestOrRegistered screen
	/// </summary>
	public class GuestOrRegisteredPresenter : UIPresenterData2<GuestOrRegisteredPresenter.StateData>
	{
		public class StateData
		{
			public Action GoToLoginClicked;
			public Action PlayAsGuestClicked;
		}

		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements()
		{
			Root.Q<Button>("RegisterLoginButton").clicked += OnLoginRegisterClicked;
			Root.Q<Button>("GuestButton").clicked += OnPlayAsGuestButtonClicked;
			Root.SetupClicks(_services);
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