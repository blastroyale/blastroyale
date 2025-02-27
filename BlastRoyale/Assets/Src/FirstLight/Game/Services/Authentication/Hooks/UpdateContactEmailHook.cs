using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Utils;
using PlayFab.ClientModels;

namespace FirstLight.Game.Services.Authentication.Hooks
{
	public class UpdateContactEmailHook : IAuthenticationHook
	{
		public UniTask BeforeAuthentication(bool previouslyLoggedIn = false)
		{
			return UniTask.CompletedTask;
		}

		public UniTask AfterAuthentication(LoginResult result, bool previouslyLoggedIn = false)
		{
			var email = result.InfoResultPayload.AccountInfo.PrivateInfo.Email;
			var contactEmails = result.InfoResultPayload.PlayerProfile?.ContactEmailAddresses;
			var isMissingContactEmail = contactEmails == null || !contactEmails.Any(e => e != null && e.EmailAddress.Contains("@"));
			if (email != null && email.Contains("@") && isMissingContactEmail)
			{
				AsyncPlayfabAPI.ClientAPI.AddOrUpdateContactEmail(new AddOrUpdateContactEmailRequest()
				{
					EmailAddress = email
				}).Forget();
			}

			return UniTask.CompletedTask;
		}

		public UniTask AfterFetchedState(LoginResult result)
		{
			return UniTask.CompletedTask;
		}
	}
}