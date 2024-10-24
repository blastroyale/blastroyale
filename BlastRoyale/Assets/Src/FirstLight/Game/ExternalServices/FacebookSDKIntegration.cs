using System.Collections.Generic;
using Facebook.Unity;
using FirstLight.FLogger;
using UnityEngine;

namespace FirstLight.Game.ExternalServices
{
	//Will keep this one in case we decide not to Go with KWALEE
	//Since Kwalee already initialize the Facebook SDK, we dont need to do anything here.
	public class FacebookSDKIntegration : MonoBehaviour
	{
		private void Awake()
		{
			InitFacebookSDK();
		}

		//Facebook SDK
		private void InitFacebookSDK()
		{
			if (!FB.IsInitialized) {
				Debug.Log("Initializing Facebook SDK");
				FB.Init(InitCallback, OnHideUnity);
			} else {
				Debug.Log("Facebook SDK is already initialized... Activating APP");
				FB.ActivateApp();
			}
		}
		
		private void InitCallback ()
		{
			Debug.Log("Facebook SDK Init Callback");
			if (FB.IsInitialized) {
				
				Debug.Log("Facebook SDK is already initialized... Activating APP");
				FB.ActivateApp();
				
			} else {
				Debug.Log("Failed to Initialize the Facebook SDK");
			}
		}
		
		private void OnHideUnity (bool isGameShown)
		{
			if (!isGameShown) {
				Debug.Log("Facebook SDK - Unity is HIDE... setting timeScale to 0");
				Time.timeScale = 0;
			} else {
				Debug.Log("Facebook SDK - Unity is FOCUS... setting timeScale to 1");
				Time.timeScale = 1;
			}
		}
	}
}