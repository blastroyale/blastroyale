using FirstLight.Game.Services.Authentication;
using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Service that wraps all the features of our Customer Support system 
	/// </summary>
	public interface ICustomerSupportService
	{
		void OpenCustomerSupportTicketForm();
	}

	/// <inheritdoc />
	public class CustomerSupportService : ICustomerSupportService
	{
		private readonly IAuthService _authenticationService;

		private const string ZENDESK_FORM_REQUESTER_EMAIL = "tf_anonymous_requester_email={0}";

		public CustomerSupportService(IAuthService authService)
		{
			_authenticationService = authService;
		}

		public void OpenCustomerSupportTicketForm()
		{
			var userEmail = _authenticationService.SessionData.IsGuest
				? ""
				: string.Format(ZENDESK_FORM_REQUESTER_EMAIL, _authenticationService.SessionData.Email);

			Application.OpenURL($"{GameConstants.Links.ZENDESK_SUPPORT_FORM}?{userEmail}");
		}
	}
}