using FirstLight.FLogger;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Messages;
using FirstLight.NativeUi;
using FirstLight.SDK.Services;
using FirstLightServerSDK.Services;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This service handles logic for determining showing review prompt 
	/// </summary>
	public class RateAndReviewService
	{
		private readonly IMessageBrokerService _messageBrokerService;
		private RateAndReview _rateAndReviewComponent;
		private LocalPrefsService _localPrefsService;
		private readonly IRemoteConfigProvider _remoteConfigProvider;
		private bool _canShowPrompt;

		private int RequiredMatches => _remoteConfigProvider.GetConfig<GeneralConfig>().ReviewPromptGamesPlayedReq;

		public RateAndReviewService()
		{
		}

		public RateAndReviewService(IMessageBrokerService msgBroker, LocalPrefsService localPrefsService, IRemoteConfigProvider remoteConfigProvider)
		{
			_localPrefsService = localPrefsService;
			_remoteConfigProvider = remoteConfigProvider;
			_messageBrokerService = msgBroker;
			msgBroker.Subscribe<SuccessfullyAuthenticated>(OnAuthenticated);
		}

		private void OnAuthenticated(SuccessfullyAuthenticated msg)
		{
			if (msg.PreviouslyLoggedIn) return;
			Init();
		}

		private void Init()
		{
			if (!_remoteConfigProvider.GetConfig<GeneralConfig>().EnableReviewPrompt || _localPrefsService.RateAndReviewPromptShown)
			{
				return;
			}

			_rateAndReviewComponent = new GameObject("Rate And Review").AddComponent<RateAndReview>();
			Object.DontDestroyOnLoad(_rateAndReviewComponent.gameObject);

			_messageBrokerService.Subscribe<MainMenuOpenedMessage>(OnPlayScreenOpenedMessage);
			_messageBrokerService.Subscribe<GameCompletedRewardsMessage>(OnGameCompletedRewardsMessage);
			_messageBrokerService.Subscribe<CollectionItemEquippedMessage>(OnCollectionItemEquippedMessage);
			_messageBrokerService.Subscribe<BattlePassLevelUpMessage>(OnBattlePassLevelUpMessage);
		}

		private void OnBattlePassLevelUpMessage(BattlePassLevelUpMessage message)
		{
			if (_localPrefsService.GamesPlayed.Value < RequiredMatches)
			{
				return;
			}

			_canShowPrompt = true;
		}

		private void OnCollectionItemEquippedMessage(CollectionItemEquippedMessage message)
		{
			if (_localPrefsService.GamesPlayed.Value < RequiredMatches)
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
			_messageBrokerService.Unsubscribe<MainMenuOpenedMessage>(OnPlayScreenOpenedMessage);

			FLog.Info($"RateAndReviewService->OpenRateAndReviewPrompt");
		}

		private void OnPlayScreenOpenedMessage(MainMenuOpenedMessage message)
		{
			if (_localPrefsService.GamesPlayed.Value < RequiredMatches)
			{
				_canShowPrompt = false;

				return;
			}

			var shouldShowPrompt = !_localPrefsService.RateAndReviewPromptShown && _canShowPrompt;

			if (!shouldShowPrompt)
			{
				return;
			}

			OpenRateAndReviewPrompt();
		}

		private void OnGameCompletedRewardsMessage(GameCompletedRewardsMessage message)
		{
			_localPrefsService.GamesPlayed.Value += 1;
			FLog.Info(
				$"RateAndReviewService->OnGameCompletedRewardsMessage LocalPrefsService.GamesPlayed.Value {_localPrefsService.GamesPlayed.Value}");
		}
	}
}