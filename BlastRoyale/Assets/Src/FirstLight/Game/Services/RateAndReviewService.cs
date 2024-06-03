using FirstLight.FLogger;
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
	}

	/// <inheritdoc />
	public class RateAndReviewService : IRateAndReviewService
	{
		private readonly IMessageBrokerService _messageBrokerService;
		private RateAndReview _rateAndReviewComponent;
		private LocalPrefsService _localPrefsService;
		private bool _canShowPrompt;

		
		public RateAndReviewService(IMessageBrokerService msgBroker, LocalPrefsService localPrefsService)
		{
			_localPrefsService = localPrefsService;
			
			if (!FeatureFlags.REVIEW_PROMPT_ENABLED || localPrefsService.RateAndReviewPromptShown)
			{
				return;
			}
			
			_rateAndReviewComponent = new GameObject("Rate And Review").AddComponent<RateAndReview>();
			Object.DontDestroyOnLoad(_rateAndReviewComponent.gameObject);

			_messageBrokerService = msgBroker;
			_messageBrokerService.Subscribe<PlayScreenOpenedMessage>(OnPlayScreenOpenedMessage);
			_messageBrokerService.Subscribe<GameCompletedRewardsMessage>(OnGameCompletedRewardsMessage);
			_messageBrokerService.Subscribe<CollectionItemEquippedMessage>(OnCollectionItemEquippedMessage);
			_messageBrokerService.Subscribe<BattlePassLevelUpMessage>(OnBattlePassLevelUpMessage);
			
			FLog.Info($"RateAndReviewService->ctr");
		}
		
		private void OnBattlePassLevelUpMessage(BattlePassLevelUpMessage message)
		{
			if (_localPrefsService.GamesPlayed.Value < RemoteConfigs.Instance.ReviewPromptGamesPlayedReq)
			{
				return;
			}

			_canShowPrompt = true;
		}

		private void OnCollectionItemEquippedMessage(CollectionItemEquippedMessage message)
		{
			if (_localPrefsService.GamesPlayed.Value < RemoteConfigs.Instance.ReviewPromptGamesPlayedReq)
			{
				return;
			}

			_canShowPrompt = true;
		}

		private void OpenRateAndReviewPrompt()
		{
			_rateAndReviewComponent.RateReview();
			_localPrefsService.RateAndReviewPromptShown.Value = true;
			_messageBrokerService.Unsubscribe<GameCompletedRewardsMessage>(OnGameCompletedRewardsMessage);
			_messageBrokerService.Unsubscribe<CollectionItemEquippedMessage>(OnCollectionItemEquippedMessage);
			_messageBrokerService.Unsubscribe<BattlePassLevelUpMessage>(OnBattlePassLevelUpMessage);
			_messageBrokerService.Unsubscribe<PlayScreenOpenedMessage>(OnPlayScreenOpenedMessage);
			
			FLog.Info($"RateAndReviewService->OpenRateAndReviewPrompt");
		}

		private void OnPlayScreenOpenedMessage(PlayScreenOpenedMessage message)
		{
			if (_localPrefsService.GamesPlayed.Value < RemoteConfigs.Instance.ReviewPromptGamesPlayedReq)
			{
				_canShowPrompt = false;
				
				return;
			}

			var shouldShowPrompt = _localPrefsService.RateAndReviewPromptShown && _canShowPrompt;

			if (!shouldShowPrompt)
			{
				return;
			}

			OpenRateAndReviewPrompt();
		}

		private void OnGameCompletedRewardsMessage(GameCompletedRewardsMessage message)
		{
			_localPrefsService.GamesPlayed.Value += 1;
			FLog.Info($"RateAndReviewService->OnGameCompletedRewardsMessage LocalPrefsService.GamesPlayed.Value {_localPrefsService.GamesPlayed.Value}");
		}
	}
}