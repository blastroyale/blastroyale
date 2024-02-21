using Cysharp.Threading.Tasks;

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
#if UNITY_IOS && !UNITY_EDITOR
			return IsIOSPermissionAnswered();
#else
			return true;
#endif
		}
		
		/// <summary>
		/// Returns true if the user accepted tracking analytics
		/// </summary>
		public bool IsTrackingAccepted()
		{
#if UNITY_IOS && !UNITY_EDITOR
			return IsIOSTrackingAccepted();
#else
			return true;
#endif
		}

		public void RequestPermissions()
		{
			#if UNITY_IOS && !UNITY_EDITOR
				DisplayIOSPermissionsDialog();
			#endif
		}
		
		#if UNITY_IOS && !UNITY_EDITOR
		private bool IsIOSTrackingAccepted()
		{
			var currentStatus = Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
			return currentStatus != Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED;
		}

		private bool IsIOSPermissionAnswered()
		{
			var currentStatus = Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
			return currentStatus != Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED;
		}
		
		private void DisplayIOSPermissionsDialog()
		{
			Unity.Advertisement.IosSupport.SkAdNetworkBinding.SkAdNetworkRegisterAppForNetworkAttribution();
			var currentStatus = Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
			if (currentStatus ==
				Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
			{
				Unity.Advertisement.IosSupport.ATTrackingStatusBinding.RequestAuthorizationTracking();
			}
		}
		#endif

		public async UniTask PermissionResponseAwaitTask()
		{
			while (!IsPermissionsAnswered()) await UniTask.Delay(10);
		}
	}
}