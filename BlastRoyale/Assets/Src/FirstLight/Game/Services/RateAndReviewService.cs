using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.SDK.Services;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// 
	/// </summary>
	public interface IRateAndReviewService
	{
	}

	/// <inheritdoc />
	public class RateAndReviewService : IRateAndReviewService
	{
		private readonly IMessageBrokerService _messageBrokerService;
		private RateAndReview _rateAndReviewComponent;
		private LocalPrefsService _localPrefsService;
		
		public RateAndReviewService(IMessageBrokerService msgBroker,
									LocalPrefsService localPrefsService)
		{
			if (!FeatureFlags.REVIEW_PROMPT_ENABLED)
			{
				return;
			}
			
			_rateAndReviewComponent = new GameObject("Rate And Review").AddComponent<RateAndReview>();
			Object.DontDestroyOnLoad(_rateAndReviewComponent.gameObject);

			_messageBrokerService = msgBroker;
			_localPrefsService = localPrefsService;
			_messageBrokerService.Subscribe<OpenRateAndReviewPromptMessage>(OnOpenRateAndReviewPromptMessage);
		}
		
		private void OnOpenRateAndReviewPromptMessage(OpenRateAndReviewPromptMessage message)
		{
			if (!FeatureFlags.REVIEW_PROMPT_ENABLED)
			{
				return;
			}
			
			_rateAndReviewComponent.RateReview();
			_localPrefsService.RateAndReviewPromptShown.Value = true;
			_messageBrokerService.Unsubscribe<OpenRateAndReviewPromptMessage>(OnOpenRateAndReviewPromptMessage);
		}
	}
}