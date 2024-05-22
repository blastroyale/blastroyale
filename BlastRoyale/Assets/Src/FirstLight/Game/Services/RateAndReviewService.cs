using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.SDK.Services;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This service handles logic for determining showing review prompt 
	/// </summary>
	public interface IRateAndReviewService
	{
		bool ShouldShowPrompt { get; }
	}

	/// <inheritdoc />
	public class RateAndReviewService : IRateAndReviewService
	{
		private readonly IMessageBrokerService _messageBrokerService;
		private RateAndReview _rateAndReviewComponent;
		private LocalPrefsService _localPrefsService;
		
		public RateAndReviewService(IMessageBrokerService msgBroker, LocalPrefsService localPrefsService)
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
			_messageBrokerService.Subscribe<GameCompletedRewardsMessage>(OnGameCompletedRewardsMessage);
		}

		public bool ShouldShowPrompt => FeatureFlags.REVIEW_PROMPT_ENABLED && !_localPrefsService.RateAndReviewPromptShown && _localPrefsService.GamesPlayed.Value >= 4;

		private void OnOpenRateAndReviewPromptMessage(OpenRateAndReviewPromptMessage message)
		{
			if (!FeatureFlags.REVIEW_PROMPT_ENABLED)
			{
				return;
			}
			
			_rateAndReviewComponent.RateReview();
			_localPrefsService.RateAndReviewPromptShown.Value = true;
			_messageBrokerService.Unsubscribe<OpenRateAndReviewPromptMessage>(OnOpenRateAndReviewPromptMessage);
			_messageBrokerService.Unsubscribe<GameCompletedRewardsMessage>(OnGameCompletedRewardsMessage);
		}

		private void OnGameCompletedRewardsMessage(GameCompletedRewardsMessage message)
		{
			_localPrefsService.GamesPlayed.Value += 1;
			//Debug.Log($"LocalPrefsService.GamesPlayed.Value {_localPrefsService.GamesPlayed.Value}");
		}
	}
}