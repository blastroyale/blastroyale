using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.AdminModels;
using PlayFab.ServerModels;
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
		
		public async Task<int> GetSeason(string name)
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
		
		public async Task UpdateStatistics(string user, params ValueTuple<string, int> [] stats)
		{
			const int STATISTICS_CHUNK_SIZE = 20;
			var chunkedStatistics = SplitList(stats, STATISTICS_CHUNK_SIZE);

			foreach (var chunk in chunkedStatistics)
			{
				var toUpdate = new List<StatisticUpdate>();
				foreach (var stat in chunk)
				{
					toUpdate.Add(new StatisticUpdate()
					{
						Value = stat.Item2,
						StatisticName = stat.Item1
					});
				}

				var resp = await PlayFabServerAPI.UpdatePlayerStatisticsAsync(new UpdatePlayerStatisticsRequest()
				{
					PlayFabId = user,
					Statistics = toUpdate,
				});

				if (resp.Error != null)
				{
					_log.LogError(resp.Error.GenerateErrorReport());
				}
			}
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
		
		public static IEnumerable<IEnumerable<T>> SplitList<T>(IEnumerable<T> list, int size)
		{
			for (int i = 0; i < list.Count(); i += size)
			{
				yield return list.Skip(i).Take(size);
			}
		}
	}
}