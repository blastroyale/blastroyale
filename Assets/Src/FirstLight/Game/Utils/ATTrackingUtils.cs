using Cysharp.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// A wrapper to help with ATTracking on iOS because the Unity ATTrackingStatusBinding is not available on Android.
	/// </summary>
	public static class ATTrackingUtils
	{
		public static bool IsTrackingAllowed()
		{
#if UNITY_IOS && !UNITY_EDITOR
			return Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus() == Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED;
#else
			return true;
#endif
		}

		public static async UniTask RequestATTPermission()
		{
#if UNITY_IOS && !UNITY_EDITOR
			await UniTask.NextFrame(); // We wait for one frame because we had problems with the request in the past apparently
			Unity.Advertisement.IosSupport.ATTrackingStatusBinding.RequestAuthorizationTracking();

			await UniTask.WaitUntil(() => Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus() != Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED);
#endif
		}
	}
}