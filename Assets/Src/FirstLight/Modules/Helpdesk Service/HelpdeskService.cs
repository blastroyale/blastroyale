using System.Collections.Generic;
using Helpshift;
using UnityEngine;

namespace FirstLight.HelpdeskService
{
	
	public interface IHelpdeskService
	{
		void Login(string id, string email, string username);
		void Logout();
		void StartConversation();
		void ShowFaq();
	}
	public class HelpdeskService : IHelpdeskService
	{
		private HelpshiftSdk _help;

		public HelpdeskService()
		{
			Debug.Log("Installing helpshift");
			var configMap = new Dictionary<string, object>();
			
#if UNITY_EDITOR
			//Do nothing
#elif UNITY_ANDROID
			_help = HelpshiftSdk.GetInstance();
			_help.Install("blastroyale_platform_20220614135609109-850eeb708117baf", "blastroyale.helpshift.com", configMap);
#elif UNITY_IOS
			_help = HelpshiftSdk.GetInstance();
			_help.Install("blastroyale_platform_20220614135609081-cb7c86c0c85e18c", "blastroyale.helpshift.com", configMap);
#endif
		}

		//Helpdesk login
		public void Login(string id, string email, string username)
		{
			if (_help == null)
			{
				Debug.Log("Helpdesk Login called. Doesn't work in Unity Editor");
				return;
			}
			
			Dictionary<string, string> userDetails = new Dictionary<string, string>
			{
				{ "userId", id },
				{ "userEmail", email },
				{ "userName", username }
			};
			_help.Login(userDetails);
		}


		public void Logout()
		{
			if (_help == null)
			{
				Debug.Log("Helpdesk Logout called. Doesn't work in Unity Editor");
				return;
			}
			_help.Logout();
			
		}

		public void StartConversation()
		{
			if (_help == null)
			{
				Debug.Log("Helpdesk StartConversation called. Doesn't work in Unity Editor");
				return;
			}
			_help.ShowConversation();
		}

		public void ShowFaq()
		{
			if (_help == null)
			{
				Debug.Log("Helpdesk ShowFaq called. Doesn't work in Unity Editor");
				return;
			}
			_help.ShowFAQs();
		}
	}
}