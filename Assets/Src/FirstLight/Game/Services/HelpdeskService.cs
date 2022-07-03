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
			Helpshift.HelpshiftSdk.GetInstance().Install("blastroyale_platform_20220614135609109-850eeb708117baf", 
				"blastroyale.helpshift.com", new Dictionary<string, object>());
#elif UNITY_IOS
			Helpshift.HelpshiftSdk.GetInstance().Install("blastroyale_platform_20220614135609081-cb7c86c0c85e18c", 
				"blastroyale.helpshift.com", new Dictionary<string, object>());
#endif
		}

		public void Login(string id, string email, string username)
		{
#if UNITY_EDITOR
			Debug.Log("Helpdesk Login called. Doesn't work in Unity Editor");
#else
			var userDetails = new Dictionary<string, string>
			{
				{ "userId", id },
				{ "userEmail", email },
				{ "userName", username }
			};
			
			Helpshift.HelpshiftSdk.GetInstance().Login(userDetails);
#endif
		}


		public void Logout()
		{
#if UNITY_EDITOR
			Debug.Log("Helpdesk Logout called. Doesn't work in Unity Editor");
#else
			Helpshift.HelpshiftSdk.GetInstance().Logout();
#endif
		}

		public void StartConversation()
		{
#if UNITY_EDITOR
			Debug.Log("Helpdesk StartConversation called. Doesn't work in Unity Editor");
#else	
			Helpshift.HelpshiftSdk.GetInstance().ShowConversation();
#endif
		}

		public void ShowFaq()
		{
#if UNITY_EDITOR
			Debug.Log("Helpdesk ShowFaq called. Doesn't work in Unity Editor");
#else
			Helpshift.HelpshiftSdk.GetInstance().ShowFAQs();
#endif	
		}
	}
}