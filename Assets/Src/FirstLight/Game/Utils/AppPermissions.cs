using System.Threading.Tasks;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Represents mobile app permission handling
	/// </summary>
	public class AppPermissions
	{
		private static AppPermissions _singleton;

		public AppPermissions()
		{
			_singleton = this;
		}

		public static AppPermissions Get() => _singleton;

		/// <summary>
		/// Returns true if the user has answered already to either block or not permissions
		/// </summary>
		public bool IsPermissionsAnswered()
		{
			#if UNITY_IOS
				return IsIOSPermissionAnswered();
			#endif
			return true;
		}
		
		/// <summary>
		/// Returns true if the user accepted tracking analytics
		/// </summary>
		public bool IsTrackingAccepted()
		{
			#if UNITY_IOS
				return IsIOSTrackingAccepted();
			#endif
			return false;
		}

		public void RequestPermissions()
		{
			#if UNITY_IOS
				DisplayIOSPermissionsDialog();
			#endif
		}
		
		private bool IsIOSTrackingAccepted()
		{
			var currentStatus = Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
			Debug.Log("Current iOS permission status: " + currentStatus);
			return currentStatus != Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED;
		}

		private bool IsIOSPermissionAnswered()
		{
			var currentStatus = Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
			Debug.Log("Current iOS permission status: " + currentStatus);
			return currentStatus != Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED;
		}
		
		private void DisplayIOSPermissionsDialog()
		{
			Unity.Advertisement.IosSupport.SkAdNetworkBinding.SkAdNetworkRegisterAppForNetworkAttribution();
			var currentStatus = Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
			Debug.Log("Current iOS permission Status: " + currentStatus);
			if (currentStatus ==
				Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
			{
				Debug.Log("Requesting app permissions");
				Unity.Advertisement.IosSupport.ATTrackingStatusBinding.RequestAuthorizationTracking();
			}
		}

		public async Task PermissionResponseAwaitTask()
		{
			while (!IsPermissionsAnswered()) await Task.Delay(10);
		}
	}
}