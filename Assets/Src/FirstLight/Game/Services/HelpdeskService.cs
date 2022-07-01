using System.Collections.Generic;
using Helpshift;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Service that wraps all the features of our Help Desk system (HelpShift currently)
	/// </summary>
	public interface IHelpdeskService
	{
		/// <inheritdoc cref="HelpshiftSdk.Login"/>
		void Login(string id, string email, string username);
		/// <inheritdoc cref="HelpshiftSdk.Logout"/>
		void Logout();
		/// <inheritdoc cref="HelpshiftSdk.ShowConversation"/>
		void StartConversation();
		/// <inheritdoc cref="HelpshiftSdk.ShowFAQs"/>
		void ShowFaq();
	}
	
	/// <inheritdoc />
	public class HelpdeskService : IHelpdeskService
	{
		public HelpdeskService()
		{
#if UNITY_EDITOR
			//Do nothing
#elif UNITY_ANDROID
			HelpshiftSdk.GetInstance().Install("blastroyale_platform_20220614135609109-850eeb708117baf", 
				"blastroyale.helpshift.com", new Dictionary<string, object>());
#elif UNITY_IOS
			HelpshiftSdk.GetInstance().Install("blastroyale_platform_20220614135609081-cb7c86c0c85e18c", 
				"blastroyale.helpshift.com", new Dictionary<string, object>());
#endif
		}

		public void Login(string id, string email, string username)
		{
			var sdk = HelpshiftSdk.GetInstance();
			
			if (sdk == null)
			{
				Debug.Log("Helpdesk Login called. Doesn't work in Unity Editor");
				return;
			}
			
			var userDetails = new Dictionary<string, string>
			{
				{ "userId", id },
				{ "userEmail", email },
				{ "userName", username }
			};
			
			sdk.Login(userDetails);
		}


		public void Logout()
		{
			var sdk = HelpshiftSdk.GetInstance();
			
			if (sdk == null)
			{
				Debug.Log("Helpdesk Logout called. Doesn't work in Unity Editor");
				return;
			}
			
			sdk.Logout();
		}

		public void StartConversation()
		{
			var sdk = HelpshiftSdk.GetInstance();
			
			if (sdk == null)
			{
				Debug.Log("Helpdesk StartConversation called. Doesn't work in Unity Editor");
				return;
			}
			
			sdk.ShowConversation();
		}

		public void ShowFaq()
		{
			var sdk = HelpshiftSdk.GetInstance();
			
			if (sdk == null)
			{
				Debug.Log("Helpdesk ShowFaq called. Doesn't work in Unity Editor");
				return;
			}
			
			sdk.ShowFAQs();
		}
	}
}