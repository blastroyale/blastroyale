using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.SDK.Services;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Execute battle pass initialization command after authentication
	/// </summary>
	public interface IBattlePassService
	{
		 
	}

	public class BattlePassService : IBattlePassService
	{
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private bool _hasUnseenSeason;

		public BattlePassService(IMessageBrokerService msgBroker, IGameDataProvider gameDataProvider, IGameServices services)
		{
			_gameDataProvider = gameDataProvider;
			_services = services;
			msgBroker.Subscribe<GameLogicInitialized>(InitializeBattlePass);
			msgBroker.Subscribe<MainMenuOpenedMessage>(OnMainMenuOpen);
		}

		private void OnMainMenuOpen(MainMenuOpenedMessage msg)
		{
			
			if (!_hasUnseenSeason) return;
			if (_services.TutorialService.IsTutorialRunning)
			{
				_hasUnseenSeason = true; // new users for now don't see the banner
				return;
			}
			_services.GameUiService.OpenUiAsync<BattlePassSeasonBannerPresenter>();
			_hasUnseenSeason = false;
		}
		
		private void InitializeBattlePass(GameLogicInitialized _)
		{
			var hasRewardsToClaim = _gameDataProvider.BattlePassDataProvider.HasUncollectedRewardsFromPreviousSeasons();
			var shouldInitializeSeason = _gameDataProvider.BattlePassDataProvider.ShouldInitializeSeason();
			if (hasRewardsToClaim || shouldInitializeSeason)
			{
				_services.CommandService.ExecuteCommand(new InitializeBattlepassSeasonCommand());
				_hasUnseenSeason = true;
			}
		}
	}
}