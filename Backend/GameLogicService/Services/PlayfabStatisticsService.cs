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
		
		public void UpdateStatistics(string user, params ValueTuple<string, int> [] stats)
		{
			var toUpdate = new List<StatisticUpdate>();
			foreach(var stat in stats)
			{
				toUpdate.Add(new StatisticUpdate()
				{
					Value = stat.Item2,
					StatisticName = stat.Item1
				});
			}
			PlayFabServerAPI.UpdatePlayerStatisticsAsync(new UpdatePlayerStatisticsRequest()
			{
				PlayFabId = user,
				Statistics = toUpdate,
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