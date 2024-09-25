using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using PlayFab;
using PlayFab.AdminModels;
using PlayFab.ServerModels;
using Quantum;

/// <summary>
/// Script sample for event rewards
/// </summary>
public class EventRewards : PlayfabScript
{
   public override Environment GetEnvironment() => Environment.PROD;
   
   public override void Execute(ScriptParameters parameters)
   {
      var task = RunAsync();
      task.Wait();
   }

   public List<List<string>> _csvData = new();
   private string _lastRewards = "";
   
   public async Task RunAsync()
   {
      var tasks = new List<Task<string>>();
      var leaderboard = await GetLeaderboard(100, 100, GameConstants.Stats.RANKED_GAMES_WON);

      await CreateSumMetric("Tournament Top1");
      await CreateSumMetric("Made Tournament Top3");
      await CreateSumMetric("Made Tournament Top10");
      await CreateSumMetric("Made Tournament Top100");
      _csvData.Add(new List<string>()
         {"Name", "Rank", "Trophies", "Rewards"}
      );
      
      foreach (var player in leaderboard)
      {
         tasks.Add(Proccess(player));
      }
      Task.WaitAll(tasks.ToArray());
      foreach (var t in tasks)
      {
         Console.WriteLine(await t);
      }
      ToCsv($"{AppDomain.CurrentDomain.BaseDirectory}/top100.csv", _csvData);
      Console.WriteLine("Done !");
   }

   private void AddReward(PlayerData data, GameId id, int amt)
   {
      _lastRewards += $"{amt}x {id}";
      data.UncollectedRewards.Add(ItemFactory.Currency(id, amt));
   }

   private async Task<string> Proccess(PlayerLeaderboardEntry entry)
   {
      var state = await ReadUserState(entry.PlayFabId);
      if (state == null || !state.Has<PlayerData>())
      {
         return null;
      }
      var playerData = state.DeserializeModel<PlayerData>();
      var rank = entry.Position + 1;
      var statistic = new List<StatisticUpdate>();
      if (rank == 1)
      {
         statistic.Add(new StatisticUpdate()
         {
            Value = 1,
            StatisticName = "Tournament Top1"
         });
      }
      if (rank <= 3)
      {
         statistic.Add(new StatisticUpdate()
         {
            Value = 1,
            StatisticName = "Made Tournament Top3"
         });
      }
      if (rank <= 10)
      {
         statistic.Add(new StatisticUpdate()
         {
            Value = 1,
            StatisticName = "Made Tournament Top10"
         });
      }
      if (rank <= 100)
      {
         statistic.Add(new StatisticUpdate()
         {
            Value = 1,
            StatisticName = "Made Tournament Top100"
         });
      }
      /*
      if (statistic.Count > 0)
      {
         await SumStatistic(entry.PlayFabId, statistic.ToArray());
      }
      if (rank == 1)
      {
         AddReward(playerData, GameId.COIN, 8000);
         AddReward(playerData, GameId.CS, 4000);
      } else if (rank == 2)
      {
         AddReward(playerData, GameId.COIN, 6000);
         AddReward(playerData, GameId.CS, 3000);
      } else if (rank == 3)
      {
         AddReward(playerData, GameId.COIN, 5000);
         AddReward(playerData, GameId.CS, 2500);
      } else if (rank >= 4 && entry.Position <= 10)
      {
         AddReward(playerData, GameId.COIN, 4000);
         AddReward(playerData, GameId.CS, 2000);
      } else if (rank >= 11 && entry.Position <= 20)
      {
         AddReward(playerData, GameId.COIN, 3000);
         AddReward(playerData, GameId.CS, 1500);
      } else if (rank >= 21 && entry.Position <= 50)
      {
         AddReward(playerData, GameId.COIN, 2000);
         AddReward(playerData, GameId.CS, 1000);
      }else if (rank >= 51 && rank <= 100)
      {
         AddReward(playerData, GameId.COIN, 800);
         AddReward(playerData, GameId.CS, 400);
      }
      state.UpdateModel(playerData);
      await SetUserState(entry.PlayFabId, state);
      */
      
      _csvData.Add(new List<string>()
         {entry.DisplayName, rank.ToString(), playerData.Trophies.ToString(), _lastRewards}
      );
      _lastRewards = "";
      return $"Player {entry.PlayFabId} {entry.DisplayName} Rank {rank} Trophies {playerData.Trophies} ";
   }
}