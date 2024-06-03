using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services.Analytics.Events;

namespace FirstLight.Game.Services.Analytics
{
	public class AnalyticsCallLeveling : AnalyticsCalls
	{
		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;

		public AnalyticsCallLeveling(IAnalyticsService analyticsService, IGameServices services, IGameDataProvider dataProvider) : base(
			analyticsService)
		{
			_analyticsService = analyticsService;
			_services = services;
			_dataProvider = dataProvider;
			_services?.MessageBrokerService.Subscribe<BattlePassLevelUpMessage>(OnBattlePassLevelUpMessage);
		}

		private void OnBattlePassLevelUpMessage(BattlePassLevelUpMessage msg)
		{
			LogEventBlastPassLevelUp(msg);
			LogEventBlastPassCompleted(msg);
		}

		private void LogEventBlastPassCompleted(BattlePassLevelUpMessage msg)
		{
			if (msg.Completed)
			{
				_analyticsService.LogEvent(new BlastPassCompletedEvent(_dataProvider.BattlePassDataProvider.CurrentSeason));
			}
		}

		private void LogEventBlastPassLevelUp(BattlePassLevelUpMessage msg)
		{
			for (var i = msg.PreviousLevel; i < msg.NewLevel; ++i)
			{
				_analyticsService.LogEvent(new BlastPassLevelUpEvent(_dataProvider.BattlePassDataProvider.CurrentSeason, (int) i + 1));
			}
		}
	}
}