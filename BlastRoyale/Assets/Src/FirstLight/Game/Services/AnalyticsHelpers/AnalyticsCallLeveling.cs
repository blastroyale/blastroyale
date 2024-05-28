using System.Collections.Generic;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using UnityEngine;

namespace FirstLight.Game.Services.AnalyticsHelpers
{
	public class AnalyticsCallLeveling : AnalyticsCalls
	{
		private IGameServices _services;
		private IGameDataProvider _dataProvider;

		private const uint BP_RELEVANT_LEVEL_AMOUNT = 5;
		
		public AnalyticsCallLeveling(IAnalyticsService analyticsService, IGameServices services, IGameDataProvider dataProvider) : base(analyticsService)
		{
			_analyticsService = analyticsService;
			_services = services;
			_dataProvider = dataProvider;
			_services?.MessageBrokerService.Subscribe<BattlePassLevelUpMessage>(OnBattlePassLevelUpMessage);
		}

		private void OnBattlePassLevelUpMessage(BattlePassLevelUpMessage msg)
		{
			LogEventBlastPassLevelUp(msg);
			LogEventRelevantBlastPassLevelUp(msg);
			LogEventBlastPassCompleted(msg);
		}

		private void LogEventBlastPassCompleted(BattlePassLevelUpMessage msg)
		{
			if (msg.Completed)
			{
				_analyticsService.LogEvent(AnalyticsEvents.BlastPassCompleted, new Dictionary<string, object>()
				{
					{"bp_season", _dataProvider.BattlePassDataProvider.CurrentSeason}
				});	
			}
		}

		private void LogEventBlastPassLevelUp(BattlePassLevelUpMessage msg)
		{
			for (uint i = msg.PreviousLevel; i < msg.NewLevel; ++i)
			{
				_analyticsService.LogEvent(AnalyticsEvents.BlastPassLevelUp, new Dictionary<string, object>(){
					{"bpp_level", (int)i+1}
				});
			}
		}

		private void LogEventRelevantBlastPassLevelUp(BattlePassLevelUpMessage msg)
		{
			
			var lastRelevantBPLevelAchieved = (uint) Mathf.FloorToInt(msg.PreviousLevel / BP_RELEVANT_LEVEL_AMOUNT) * BP_RELEVANT_LEVEL_AMOUNT;
			var currentRelevantBPLevelAchieved = (uint) Mathf.FloorToInt((msg.NewLevel - lastRelevantBPLevelAchieved) / BP_RELEVANT_LEVEL_AMOUNT);
			
			for (var i = 1; i <= currentRelevantBPLevelAchieved; ++i)
			{
				_analyticsService.LogEvent($"{AnalyticsEvents.BlastPassLevelUp}_{lastRelevantBPLevelAchieved + i * BP_RELEVANT_LEVEL_AMOUNT}");
			}
		}
	}
}