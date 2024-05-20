using System.Collections;
using System.Collections.Generic;
using UnityEngine.Android;
using UnityEngine;
#if UNITY_IOS
using UnityEngine.iOS;

#elif UNITY_ANDROID
using Google.Play.Review;
#endif

namespace FirstLight.NativeUi
{
	public class RateAndReview : MonoBehaviour
	{
#if UNITY_ANDROID
private ReviewManager _reviewManager;
private PlayReviewInfo _playReviewInfo;
#endif
		public static RateAndReview Instance;

		private void Start()
		{
			Instance = this;
		}

		public void RateReview()
		{
#if UNITY_IOS
			Device.RequestStoreReview();
#elif UNITY_ANDROID
			_reviewManager = new ReviewManager();
			StartCoroutine(ReviewCoroutine());
#endif
		}

#if UNITY_ANDROID
private IEnumerator ReviewCoroutine()
{
	var requestFlowOperation = _reviewManager.RequestReviewFlow();
	yield return requestFlowOperation;
	if (requestFlowOperation.Error != ReviewErrorCode.NoError)
	{
		// Log error. For example, using requestFlowOperation.Error.ToString().
		yield break;
	}
	_playReviewInfo = requestFlowOperation.GetResult();
	var launchFlowOperation = _reviewManager.LaunchReviewFlow(_playReviewInfo);
	yield return launchFlowOperation;
	_playReviewInfo = null;
	if (launchFlowOperation.Error != ReviewErrorCode.NoError)
	{
		// Log error. For example, using requestFlowOperation.Error.ToString().
		yield break;
	}
}
#endif
	}
}