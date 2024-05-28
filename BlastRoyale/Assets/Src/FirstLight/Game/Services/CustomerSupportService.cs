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
		private readonly IAuthenticationService _authenticationService;
		
		private const string ZENDESK_FORM_REQUESTER_EMAIL = "tf_anonymous_requester_email={0}";
		
		public CustomerSupportService(IAuthenticationService authenticationService)
		{
			_authenticationService = authenticationService;
		}

		public void OpenCustomerSupportTicketForm()
		{
			var userEmail = _authenticationService.IsGuest
				? "" : string.Format(ZENDESK_FORM_REQUESTER_EMAIL,_authenticationService.GetDeviceSavedAccountData().LastLoginEmail);	
		
			Application.OpenURL($"{GameConstants.Links.ZENDESK_SUPPORT_FORM}?{userEmail}");
		}
		
	}
}