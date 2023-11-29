using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Utils;
using PlayFab;
using PlayFab.AdminModels;
using PlayFab.ServerModels;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Models;
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
				return -1;
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
		
		public async Task<PublicPlayerProfile> GetProfile(string user)
		{
			var response = await PlayFabServerAPI.GetPlayerProfileAsync(new()
			{
				PlayFabId = user,
				ProfileConstraints = new()
				{
					ShowStatistics = true,
					ShowDisplayName = true,
					ShowAvatarUrl = true
				}
			});
			if (response.Error != null)
			{
				_log.LogError(response.Error.GenerateErrorReport());
				return null!;
			}
			var profile = response.Result.PlayerProfile;
			return new PublicPlayerProfile()
			{
				Name = profile.DisplayName,
				Statistics = profile.Statistics.Select(s => new Statistic()
				{
					Name = s.Name, Version = s.Version, Value = s.Value
				}).ToList(),
				AvatarUrl = profile.AvatarUrl
			};
		}
	}
}