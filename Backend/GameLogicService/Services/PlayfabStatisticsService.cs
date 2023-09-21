using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

		private async Task GetSeasonAsync(string name, Action<int> onGetSeason)
		{
			var result = await GetSeasonAsync(name);
			onGetSeason?.Invoke(result);
		}
		
		public async Task<int> GetSeasonAsync(string name)
		{
			var result = await PlayFabServerAPI.GetLeaderboardAsync(new()
			{
				StartPosition = 0,
				MaxResultsCount = 1,
				StatisticName = name
			});
			if (result.Error != null)
			{
				_log.LogError(result.Error.GenerateErrorReport());
				return 0;
			}
			return result.Result.Version;
		}
		
		public void GetSeason(string name, Action<int> onGetSeason, Action<string> onError)
		{
			_ = GetSeasonAsync(name, onGetSeason);
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