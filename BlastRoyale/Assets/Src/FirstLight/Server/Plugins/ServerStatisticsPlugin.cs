using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Models;
using Quantum;


namespace Src.FirstLight.Server
{
	/// <summary>
	/// Server plugin for Blast Royale that Implements server-sided statistics.
	/// Those statistics can be used to create leaderboards or segment players.
	/// execution.
	/// </summary>
	public class ServerStatisticsPlugin : ServerPlugin
	{
		private PluginContext _ctx;

		public override void OnEnable(PluginContext context)
		{
			_ctx = context;
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.GAMES_PLAYED_EVER, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.GAMES_PLAYED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.GAMES_WON, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.GAMES_WON_EVER, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_KILLS, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_KILLS_EVER, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_DEATHS_EVER, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.KILLS, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.KILLS_EVER, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_GAMES_WON, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_GAMES_WON_EVER, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_GAMES_PLAYED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_GAMES_PLAYED_EVER, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.DEATHS, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.NFT_ITEMS, false);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.NON_NFTS, false);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.BROKEN_ITEMS, false);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.CS_TOTAL, false);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.NOOB_TOTAL, false);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.COINS_TOTAL, false);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.CS_EARNED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.COINS_EARNED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.XP_EARNED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.BPP_EARNED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.BP_LEVEL, false);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.DAMAGE_DONE, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.DAMAGE_DONE_EVER, true);

			var evManager = _ctx.PluginEventManager!;
			evManager.RegisterEventListener<GameLogicMessageEvent<ClaimedRewardsMessage>>(OnClaimRewards);
			evManager.RegisterEventListener<GameLogicMessageEvent<RewardClaimedMessage>>(OnPurchase);
			evManager.RegisterEventListener<GameLogicMessageEvent<BattlePassLevelUpMessage>>(OnBattlePassLevel);
			evManager.RegisterEventListener<PlayerDataLoadEvent>(OnPlayerLoaded);
			evManager.RegisterCommandListener<EndOfGameCalculationsCommand>(OnEndGameCalculations);
			evManager.RegisterCommandListener<ScrapItemCommand>(OnScrap);
			evManager.RegisterCommandListener<UpgradeItemCommand>(OnUpgrade);
		}

		private async Task OnScrap(string userId, ScrapItemCommand endGameCmd, ServerState state)
		{
			await _ctx.Statistics.UpdateStatistics(userId, (GameConstants.Stats.ITEM_SCRAPS, 1), (GameConstants.Stats.ITEM_SCRAPS_EVER, 1));
		}

		private async Task OnUpgrade(string userId, UpgradeItemCommand endGameCmd, ServerState state)
		{
			await _ctx.Statistics.UpdateStatistics(userId, (GameConstants.Stats.ITEM_UPGRADES, 1), (GameConstants.Stats.ITEM_UPGRADES_EVER, 1));
		}

		private async Task OnClaimRewards(GameLogicMessageEvent<ClaimedRewardsMessage> ev)
		{
			await TrackRewards(ev.PlayerId, "ClaimedRewardsMessage", ev.Message.Rewards);
		}

		private async Task TrackRewards(string playerId, string source, IEnumerable<ItemData> rewards)
		{
			var coins = 0;
			var xp = 0;
			var bpp = 0;

			var eventData = new Dictionary<string, int>();
			foreach (var item in rewards)
			{
				if (item.Id == GameId.COIN) coins += item.GetMetadata<CurrencyMetadata>().Amount;
				if (item.Id == GameId.XP) xp += item.GetMetadata<CurrencyMetadata>().Amount;
				if (item.Id == GameId.BPP) bpp += item.GetMetadata<CurrencyMetadata>().Amount;

				eventData.TryGetValue(item.Id.ToString(), out var currentValue);
				int amount;
				if (item.HasMetadata<CurrencyMetadata>())
				{
					amount = item.GetMetadata<CurrencyMetadata>().Amount;
					_ctx.Metrics.EmitMetric($"Reward_Currency_{item.Id}", amount);
				}
				else
				{
					amount = 1;
					_ctx.Metrics.EmitMetric($"Reward_Generic_{item.Id}", 1);
				}

				eventData[item.Id.ToString()] = currentValue + amount;
			}


			if (eventData.Count > 0)
			{
				var joinedRewards = $"[{string.Join(",", eventData.Select(kv => $"\"{kv.Key}\":{kv.Value}"))}]";
				_ctx.Metrics.EmitEvent("Rewards", new Dictionary<string, string>()
				{
					{"source", source},
					{"rewards", joinedRewards}
				});
			}


			if (coins == 0 && xp == 0 && bpp == 0) return;

			await _ctx.Statistics.UpdateStatistics(playerId,
				(GameConstants.Stats.COINS_EARNED, coins),
				(GameConstants.Stats.XP_EARNED, xp),
				(GameConstants.Stats.BPP_EARNED, bpp));
		}

		private async Task OnBattlePassLevel(GameLogicMessageEvent<BattlePassLevelUpMessage> ev)
		{
			await _ctx.Statistics.UpdateStatistics(ev.PlayerId,
				(GameConstants.Stats.BP_LEVEL, (int) ev.Message.NewLevel));

			await TrackRewards(ev.PlayerId, "BattlePassLevelUpMessage", ev.Message.Rewards);
		}

		private async Task OnPurchase(GameLogicMessageEvent<RewardClaimedMessage> ev)
		{
			await _ctx.Statistics.UpdateStatistics(ev.PlayerId, (GameConstants.Stats.ITEMS_OBTAINED, 1));
		}

		private async Task OnEndGameCalculations(string userId, EndOfGameCalculationsCommand endGameCmd, ServerState state)
		{
			var toSend = new List<ValueTuple<string, int>>();
			var thisPlayerData = endGameCmd.PlayersMatchData[endGameCmd.QuantumValues.ExecutingPlayer];
			var firstPlayer = endGameCmd.PlayersMatchData.FirstOrDefault(p => p.PlayerRank == 1);
			var isWin = false;
			var ranked = endGameCmd.QuantumValues.AllowedRewards?.Contains(GameId.Trophies) ?? false;

			if (firstPlayer.Data.IsValid && thisPlayerData.TeamId == firstPlayer.TeamId && thisPlayerData.TeamId > Constants.TEAM_ID_NEUTRAL)
			{
				isWin = true;
			}
			else if (thisPlayerData.PlayerRank == 1)
			{
				isWin = true;
			}

			if (isWin)
			{
				toSend.Add((GameConstants.Stats.GAMES_WON, 1));
				toSend.Add((GameConstants.Stats.GAMES_WON_EVER, 1));
				if (ranked)
				{
					toSend.Add((GameConstants.Stats.RANKED_GAMES_WON, 1));
					toSend.Add((GameConstants.Stats.RANKED_GAMES_WON_EVER, 1));
				}
			}

			if (ranked)
			{
				toSend.Add((GameConstants.Stats.RANKED_GAMES_PLAYED_EVER, 1));
				toSend.Add((GameConstants.Stats.RANKED_KILLS_EVER, (int) thisPlayerData.Data.PlayersKilledCount));
				toSend.Add((GameConstants.Stats.RANKED_DEATHS_EVER, (int) thisPlayerData.Data.DeathCount));
				toSend.Add((GameConstants.Stats.RANKED_GAMES_PLAYED, 1));
				toSend.Add((GameConstants.Stats.RANKED_KILLS, (int) thisPlayerData.Data.PlayersKilledCount));
				toSend.Add((GameConstants.Stats.RANKED_DAMAGE_DONE_EVER, (int)thisPlayerData.Data.DamageDone));
				toSend.Add((GameConstants.Stats.RANKED_DAMAGE_DONE, (int)thisPlayerData.Data.DamageDone));
			}

			toSend.Add((GameConstants.Stats.DAMAGE_DONE_EVER, (int)thisPlayerData.Data.DamageDone));
			toSend.Add((GameConstants.Stats.DAMAGE_DONE, (int)thisPlayerData.Data.DamageDone));
			toSend.Add((GameConstants.Stats.GAMES_PLAYED_EVER, 1));
			toSend.Add((GameConstants.Stats.KILLS_EVER, (int) thisPlayerData.Data.PlayersKilledCount));
			toSend.Add((GameConstants.Stats.GAMES_PLAYED, 1));
			toSend.Add((GameConstants.Stats.KILLS, (int) thisPlayerData.Data.PlayersKilledCount));
			toSend.Add((GameConstants.Stats.DEATHS, (int) thisPlayerData.Data.DeathCount));

			var trophies = (int) await CheckUpdateTrophiesState(userId, state);
			toSend.Add((GameConstants.Stats.LEADERBOARD_LADDER_NAME, trophies));
			toSend.Add((GameConstants.Stats.NOOB_TOTAL, await UpdatePlayerDataCurrencySeasonAndReset(userId, state, GameConstants.Stats.NOOB_TOTAL, GameId.NOOB)));

			await _ctx.Statistics.UpdateStatistics(userId, toSend.ToArray());
		}

		public async Task OnPlayerLoaded(PlayerDataLoadEvent playerLoadEvent)
		{
			if (!playerLoadEvent.PlayerState.Has<EquipmentData>())
			{
				return;
			}

			var state = playerLoadEvent.PlayerState;
			var playerData = state.DeserializeModel<PlayerData>();
			playerData.Currencies.TryGetValue(GameId.CS, out var cs);
			playerData.Currencies.TryGetValue(GameId.COIN, out var coins);

			var newTrophies = await CheckUpdateTrophiesState(playerLoadEvent.PlayerId, state);
			// TODO: Check possible bug here, i think we are duplicating the trophies
			if (newTrophies != playerData.Trophies)
			{
				playerData.Trophies = newTrophies;
				state.UpdateModel(playerData);
			}

			var newNoobies = await UpdatePlayerDataCurrencySeasonAndReset(playerLoadEvent.PlayerId, state, GameConstants.Stats.NOOB_TOTAL, GameId.NOOB);
			await _ctx.Statistics.UpdateStatistics(playerLoadEvent.PlayerId,
				(GameConstants.Stats.CS_TOTAL, (int) cs),
				(GameConstants.Stats.COINS_TOTAL, (int) coins),
				(GameConstants.Stats.NOOB_TOTAL, newNoobies),
				(GameConstants.Stats.FAME, (int) playerData.Level)
			);
		}

		/// <summary>
		/// Update the player season if needs to and reset the PlayerData value,
		/// and returns how much currency the player have
		/// </summary>
		private async Task<int> UpdatePlayerDataCurrencySeasonAndReset(string userId, ServerState state, string metric, GameId currencyId)
		{
			var data = state.DeserializeModel<PlayerData>();
			var leaderboardSeason = await _ctx.Statistics.GetSeason(metric);
			data.CurrenciesSeasons.TryGetValue(currencyId, out var playerSeason);
			if (playerSeason != leaderboardSeason)
			{
				data.Currencies.TryGetValue(currencyId, out var oldCurrencyValue);
				_ctx.Metrics.EmitEvent("CurrencySeasonReset", new Dictionary<string, string>()
				{
					{"playerId", userId},
					{"oldSeason", playerSeason.ToString()},
					{"newSeason", leaderboardSeason.ToString()},
					{"currentCurrency", oldCurrencyValue.ToString()},
				});
				_ctx.Log.LogInformation($"Wiping Currency {currencyId} {userId} from s{playerSeason} to {leaderboardSeason}");
				data.CurrenciesSeasons[currencyId] = (uint) leaderboardSeason;
				data.Currencies[currencyId] = 0;
				state.UpdateModel(data);
				return 0;
			}

			var unclaimedCurrency = data.UncollectedRewards.Where(g => g.HasMetadata<CurrencyMetadata>() && g.Id == currencyId).Sum(g => g.GetMetadata<CurrencyMetadata>().Amount);
			data.Currencies.TryGetValue(currencyId, out var currentValue);
			int intValue = 0;
			try
			{
				checked
				{
					intValue = (int) currentValue;
				}
			}
			catch (OverflowException ex)
			{
				_ctx.Log.LogError(ex);
			}

			return unclaimedCurrency + intValue;
		}

		/// <summary>
		/// Runs a trophy season check in the given server state.
		/// Runs fully in-memory
		/// </summary>
		private async Task<uint> CheckUpdateTrophiesState(string userId, ServerState state)
		{
			var data = state.DeserializeModel<PlayerData>();
			var currentSeason = await _ctx.Statistics.GetSeason(GameConstants.Stats.LEADERBOARD_LADDER_NAME);
			if (currentSeason <= 0) return data.Trophies;
			if (data.TrophySeason == 0)
			{
				_ctx.Log.LogInformation($"Updating season for {userId}");
				data.TrophySeason = (uint) currentSeason;
				state.UpdateModel(data);
			}
			else if (currentSeason != data.TrophySeason)
			{
				_ctx.Metrics.EmitEvent("TrophySeasonReset", new Dictionary<string, string>()
				{
					{"playerId", userId},
					{"oldSeason", data.TrophySeason.ToString()},
					{"newSeason", currentSeason.ToString()},
					{"currentTrophies", data.Trophies.ToString()},
				});
				_ctx.Log.LogInformation($"Wiping Trophy {userId} s{data.TrophySeason} to s{currentSeason}");
				data.TrophySeason = (uint) currentSeason;
				data.Trophies = 0;
				state.UpdateModel(data);
				return 0;
			}


			var unclaimedTrophies = data.UncollectedRewards.Where(g => g.HasMetadata<CurrencyMetadata>() && g.Id == GameId.Trophies).Sum(g => g.GetMetadata<CurrencyMetadata>().Amount);
			return (uint) (data.Trophies + unclaimedTrophies);
		}
	}
}