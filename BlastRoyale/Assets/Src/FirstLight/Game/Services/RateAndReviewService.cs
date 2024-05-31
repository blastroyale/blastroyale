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
		bool ShouldShowPrompt { get; }
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
			
			if (!RemoteConfigs.Instance.EnableReviewPrompt || localPrefsService.RateAndReviewPromptShown)
			{
				return;
			}
			
			_rateAndReviewComponent = new GameObject("Rate And Review").AddComponent<RateAndReview>();
			Object.DontDestroyOnLoad(_rateAndReviewComponent.gameObject);

			_messageBrokerService = msgBroker;
			_messageBrokerService.Subscribe<OpenRateAndReviewPromptMessage>(OnOpenRateAndReviewPromptMessage);
			_messageBrokerService.Subscribe<GameCompletedRewardsMessage>(OnGameCompletedRewardsMessage);
			_messageBrokerService.Subscribe<CollectionItemEquippedMessage>(OnCollectionItemEquippedMessage);
			_messageBrokerService.Subscribe<BattlePassLevelUpMessage>(OnBattlePassLevelUpMessage);
			
			FLog.Info($"RateAndReviewService->Setup");
		}

		public bool ShouldShowPrompt => RemoteConfigs.Instance.EnableReviewPrompt && !_localPrefsService.RateAndReviewPromptShown && _canShowPrompt;

		private void OnBattlePassLevelUpMessage(BattlePassLevelUpMessage message)
		{
			if (_localPrefsService.GamesPlayed.Value < 4)
			{
				return;
			}

			_canShowPrompt = true;
		}

		private void OnCollectionItemEquippedMessage(CollectionItemEquippedMessage message)
		{
			if (_localPrefsService.GamesPlayed.Value < 4)
			{
				return;
			}

			_canShowPrompt = true;
		}

		private void OnOpenRateAndReviewPromptMessage(OpenRateAndReviewPromptMessage message)
		{
			if (!RemoteConfigs.Instance.EnableReviewPrompt)
			{
				return;
			}
			
			_rateAndReviewComponent.RateReview();
			_localPrefsService.RateAndReviewPromptShown.Value = true;
			_messageBrokerService.Unsubscribe<OpenRateAndReviewPromptMessage>(OnOpenRateAndReviewPromptMessage);
			_messageBrokerService.Unsubscribe<GameCompletedRewardsMessage>(OnGameCompletedRewardsMessage);
			_messageBrokerService.Unsubscribe<CollectionItemEquippedMessage>(OnCollectionItemEquippedMessage);
			_messageBrokerService.Unsubscribe<BattlePassLevelUpMessage>(OnBattlePassLevelUpMessage);
			
			FLog.Info($"RateAndReviewService->OnOpenRateAndReviewPromptMessage");
		}

		private void OnGameCompletedRewardsMessage(GameCompletedRewardsMessage message)
		{
			_localPrefsService.GamesPlayed.Value += 1;
			FLog.Info($"RateAndReviewService->OnGameCompletedRewardsMessage LocalPrefsService.GamesPlayed.Value {_localPrefsService.GamesPlayed.Value}");
		}
	}
}