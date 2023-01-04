using System;
using System.Collections.Generic;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Utils;
using PlayFab;
using PlayFab.AdminModels;
using PlayFab.ServerModels;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLightServerSDK.Services;
using Microsoft.Extensions.Logging;
using UpdatePlayerStatisticsRequest = PlayFab.ServerModels.UpdatePlayerStatisticsRequest;

namespace GameLogicService.Services
{
	public class PlayfabStatisticsService : IStatisticsService
	{
		private ILogger _log;
		
		public PlayfabStatisticsService(ILogger log)
		{
			_log = log;
		}
		
		public void SetupStatistic(string name, bool onlyDeltas)
		{
			PlayFabAdminAPI.CreatePlayerStatisticDefinitionAsync(new CreatePlayerStatisticDefinitionRequest()
			{
				AggregationMethod = onlyDeltas ? StatisticAggregationMethod.Sum : StatisticAggregationMethod.Last,
				StatisticName = name
			});
		}

		public void SetStatistics(string user, string name, int amount)
		{
			PlayFabServerAPI.UpdatePlayerStatisticsAsync(new UpdatePlayerStatisticsRequest()
			{
				PlayFabId = user,
				Statistics = new List<StatisticUpdate>()
				{
					new StatisticUpdate()
					{
						Value = amount,
						StatisticName = name
					}
				},
			}).ContinueWith(response =>
			{
				if (response.Result.Error != null)
				{
					_log.LogError(response.Result.Error.GenerateErrorReport());
				}
			});
		}
	}
}