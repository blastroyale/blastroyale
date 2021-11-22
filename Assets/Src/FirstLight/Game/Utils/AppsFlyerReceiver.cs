using AppsFlyerSDK;
using PlayFab.SharedModels;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// This object handles the appsflyer message receiver
	/// </summary>
	public class AppsFlyerReceiver : MonoBehaviour, IAppsFlyerConversionData, IAppsFlyerUserInvite, IAppsFlyerValidateReceipt
	{
		/// <inheritdoc />
		public void onConversionDataSuccess(string conversionData)
		{
			AppsFlyer.AFLog("didReceiveConversionData", conversionData);
			// add deferred deeplink logic here
			//Dictionary<string, object> conversionDataDictionary = AppsFlyer.CallbackStringToDictionary(conversionData);
		}

		/// <inheritdoc />
		public void onConversionDataFail(string error)
		{
			AppsFlyer.AFLog("didReceiveConversionDataWithError", error);
		}

		/// <inheritdoc />
		public void onAppOpenAttribution(string attributionData)
		{
			AppsFlyer.AFLog("onAppOpenAttribution", attributionData);
			// add direct deeplink logic here
			//Dictionary<string, object> attributionDataDictionary = AppsFlyer.CallbackStringToDictionary(attributionData);
		}

		/// <inheritdoc />
		public void onAppOpenAttributionFailure(string error)
		{
			AppsFlyer.AFLog("onAppOpenAttributionFailure", error);
		}

		/// <inheritdoc />
		public void onInviteLinkGenerated(string link)
		{
			AppsFlyer.AFLog("onInviteLinkGenerated", link);
		}

		/// <inheritdoc />
		public void onInviteLinkGeneratedFailure(string error)
		{
			AppsFlyer.AFLog("onInviteLinkGeneratedFailure", error);
		}

		/// <inheritdoc />
		public void onOpenStoreLinkGenerated(string link)
		{
			AppsFlyer.AFLog("onOpenStoreLinkGenerated", link);
		}

		/// <inheritdoc />
		public void didFinishValidateReceipt(string result)
		{
			AppsFlyer.AFLog("didFinishValidateReceipt", result);
		}

		/// <inheritdoc />
		public void didFinishValidateReceiptWithError(string error)
		{
			AppsFlyer.AFLog("didFinishValidateReceiptWithError", error);
		}
	}
}