using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
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
		private IGameDataProvider _gameDataProvider;
		private IGameCommandService _commandService;

		public BattlePassService(IMessageBrokerService msgBroker, IGameDataProvider gameDataProvider, IGameCommandService commandService)
		{
			_gameDataProvider = gameDataProvider;
			_commandService = commandService;
			msgBroker.Subscribe<GameLogicInitialized>(InitializeBattlePass);
		}


		private void InitializeBattlePass(GameLogicInitialized _)
		{
			var hasRewardsToClaim = _gameDataProvider.BattlePassDataProvider.HasUncollectedRewardsFromPreviousSeasons();
			var shouldInitializeSeason = _gameDataProvider.BattlePassDataProvider.ShouldInitializeSeason();
			if (hasRewardsToClaim || shouldInitializeSeason)
			{
				_commandService.ExecuteCommand(new InitializeBattlepassSeasonCommand());
			}
		}
	}
}