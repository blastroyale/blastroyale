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

		private async Task OnClaimRewards(GameLogicMessageEvent<ClaimedRewardsMessage> ev)
		{
			await TrackRewards(ev.PlayerId, "ClaimedRewardsMessage", ev.Message.Rewards);
		}

		public override void OnEnable(PluginContext context)
		{
			_ctx = context;
			
			//Setup Statistics in Playfab
			//Player Current Currency Statistics
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.NOOB_TOTAL, false);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.COINS_TOTAL, false);
			
			//Player Persistent General Statistics
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.ITEMS_OBTAINED, false);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.KD_RATIO, false);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.WL_RATIO, false);
			
			
			//Player Season General Statistics
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.COINS_EARNED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.SEASON_XP_EARNED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.SEASON_BPP_EARNED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.SEASON_BP_LEVEL, false);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.SEASON_WL_RATIO, false);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.SEASON_KD_RATIO, false);
			
			//Player InGame Persistent Statistics
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_DAMAGE_DONE_EVER, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_DEATHS_EVER, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_GAMES_PLAYED_EVER, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_GAMES_WON_EVER, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_KILLS_EVER, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_AIRDROP_OPENED_EVER, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_SUPPLY_CRATES_OPENED_EVER, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_GUNS_COLLECTED_EVER, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_PICKUPS_COLLECTED_EVER, true);

			//Player InGame Season Statistics
			//General
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_DAMAGE_DONE, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_DEATHS, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_GAMES_PLAYED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_GAMES_WON, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_KILLS, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_AIRDROP_OPENED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_SUPPLY_CRATES_OPENED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_GUNS_COLLECTED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.RANKED_PICKUPS_COLLECTED, true);
			
			//Solo Queue
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.SOLO_RANKED_DAMAGE_DONE, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.SOLO_RANKED_DEATHS, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.SOLO_RANKED_GAMES_PLAYED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.SOLO_RANKED_GAMES_WON, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.SOLO_RANKED_KILLS, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.SOLO_RANKED_AIRDROP_OPENED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.SOLO_RANKED_SUPPLY_CRATES_OPENED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.SOLO_RANKED_GUNS_COLLECTED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.SOLO_RANKED_PICKUPS_COLLECTED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.SOLO_LEADERBOARD_LADDER_NAME, true);

			//Duo Queue
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.DUO_RANKED_DAMAGE_DONE, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.DUO_RANKED_DEATHS, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.DUO_RANKED_GAMES_PLAYED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.DUO_RANKED_GAMES_WON, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.DUO_RANKED_KILLS, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.DUO_RANKED_AIRDROP_OPENED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.DUO_RANKED_SUPPLY_CRATES_OPENED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.DUO_RANKED_GUNS_COLLECTED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.DUO_RANKED_PICKUPS_COLLECTED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.DUO_LEADERBOARD_LADDER_NAME, true);

			//Quad Queue
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.QUAD_RANKED_DAMAGE_DONE, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.QUAD_RANKED_DEATHS, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.QUAD_RANKED_GAMES_PLAYED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.QUAD_RANKED_GAMES_WON, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.QUAD_RANKED_KILLS, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.QUAD_RANKED_AIRDROP_OPENED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.QUAD_RANKED_SUPPLY_CRATES_OPENED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.QUAD_RANKED_GUNS_COLLECTED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.QUAD_RANKED_PICKUPS_COLLECTED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.QUAD_LEADERBOARD_LADDER_NAME, true);
			

			var evManager = _ctx.PluginEventManager!;
			evManager.RegisterEventListener<GameLogicMessageEvent<ClaimedRewardsMessage>>(OnClaimRewards);
			evManager.RegisterEventListener<GameLogicMessageEvent<PurchaseClaimedMessage>>(OnPurchase);
			evManager.RegisterEventListener<GameLogicMessageEvent<BattlePassLevelUpMessage>>(OnBattlePassLevel);
			evManager.RegisterEventListener<PlayerDataLoadEvent>(OnPlayerLoaded);
			evManager.RegisterCommandListener<EndOfGameCalculationsCommand>(OnEndGameCalculations);
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
				(GameConstants.Stats.SEASON_XP_EARNED, xp),
				(GameConstants.Stats.SEASON_BPP_EARNED, bpp));
		}

		private async Task OnBattlePassLevel(GameLogicMessageEvent<BattlePassLevelUpMessage> ev)
		{
			await _ctx.Statistics.UpdateStatistics(ev.PlayerId,
				(GameConstants.Stats.SEASON_BP_LEVEL, (int) ev.Message.NewLevel));

			await TrackRewards(ev.PlayerId, "BattlePassLevelUpMessage", ev.Message.Rewards);
		}

		private async Task OnPurchase(GameLogicMessageEvent<PurchaseClaimedMessage> ev)
		{
			await _ctx.Statistics.UpdateStatistics(ev.PlayerId, (GameConstants.Stats.ITEMS_OBTAINED, 1));
		}

		// private async Task OnEndGameCalculations(string userId, EndOfGameCalculationsCommand endGameCmd, ServerState state)
		private async Task OnEndGameCalculations(string userId, EndOfGameCalculationsCommand endGameCmd, ServerState state)
		{

			var isRankedMatch = SimulationMatchConfig.FromByteArray(endGameCmd.SerializedSimulationConfig).MatchType == MatchType.Matchmaking;

			//Non-Ranked Matches (Custom Games since we don't have Casual Matches) are not used for player's statistics calculation.
			if (!isRankedMatch)
			{
				return;
			}

			var toSend = new List<ValueTuple<string, int>>();
			var firstPlacePlayerData = endGameCmd.PlayersMatchData.FirstOrDefault(p => p.PlayerRank == 1);
			var thisPlayerData = endGameCmd.PlayersMatchData[endGameCmd.QuantumValues.ExecutingPlayer];
			var simulationConfig = SimulationMatchConfig.FromByteArray(endGameCmd.SerializedSimulationConfig);

			CalculateMatchPlayedAndWinStatistics(toSend, firstPlacePlayerData, thisPlayerData, simulationConfig.TeamSize);
			CalculatePersistentStatistics(toSend, thisPlayerData, simulationConfig.TeamSize);
			await CalculateSeasonStatistics(toSend, userId, state,  thisPlayerData, simulationConfig.TeamSize);
			await _ctx.Statistics.UpdateStatistics(userId, toSend.ToArray());
		}

		private async Task CalculateSeasonStatistics(List<(string, int)> toSend, string userId, ServerState state, QuantumPlayerMatchData thisPlayerData, uint teamSize)
		{
			
			toSend.Add((GameConstants.Stats.RANKED_DAMAGE_DONE, (int) thisPlayerData.Data.DamageDone));
			toSend.Add((GameConstants.Stats.RANKED_DEATHS, (int) thisPlayerData.Data.DeathCount));
			toSend.Add((GameConstants.Stats.RANKED_KILLS, (int) thisPlayerData.Data.PlayersKilledCount));
			toSend.Add((GameConstants.Stats.RANKED_AIRDROP_OPENED, (int) thisPlayerData.Data.AirdropOpenedCount));
			toSend.Add((GameConstants.Stats.RANKED_SUPPLY_CRATES_OPENED, (int) thisPlayerData.Data.SupplyCrateOpenedCount));
			toSend.Add((GameConstants.Stats.RANKED_GUNS_COLLECTED, (int) thisPlayerData.Data.GunsCollectedCount));
			toSend.Add((GameConstants.Stats.RANKED_PICKUPS_COLLECTED, (int) thisPlayerData.Data.PickupCollectedCount));
			
			var trophies = (int) await CheckUpdateTrophiesState(userId, state);
			toSend.Add((GameConstants.Stats.RANKED_LEADERBOARD_LADDER_NAME, trophies));

			switch (teamSize)
			{
				case 1:
					toSend.Add((GameConstants.Stats.SOLO_RANKED_DAMAGE_DONE, (int) thisPlayerData.Data.DamageDone));
					toSend.Add((GameConstants.Stats.SOLO_RANKED_DEATHS, (int) thisPlayerData.Data.DeathCount));
					toSend.Add((GameConstants.Stats.SOLO_RANKED_KILLS, (int) thisPlayerData.Data.PlayersKilledCount));
					toSend.Add((GameConstants.Stats.SOLO_RANKED_AIRDROP_OPENED, (int) thisPlayerData.Data.AirdropOpenedCount));
					toSend.Add((GameConstants.Stats.SOLO_RANKED_SUPPLY_CRATES_OPENED, (int) thisPlayerData.Data.SupplyCrateOpenedCount));
					toSend.Add((GameConstants.Stats.SOLO_RANKED_GUNS_COLLECTED, (int) thisPlayerData.Data.GunsCollectedCount));
					toSend.Add((GameConstants.Stats.SOLO_RANKED_PICKUPS_COLLECTED, (int) thisPlayerData.Data.PickupCollectedCount));
					toSend.Add((GameConstants.Stats.SOLO_LEADERBOARD_LADDER_NAME, trophies));
					break;
				case 2:
					toSend.Add((GameConstants.Stats.DUO_RANKED_DAMAGE_DONE, (int) thisPlayerData.Data.DamageDone));
					toSend.Add((GameConstants.Stats.DUO_RANKED_DEATHS, (int) thisPlayerData.Data.DeathCount));
					toSend.Add((GameConstants.Stats.DUO_RANKED_KILLS, (int) thisPlayerData.Data.PlayersKilledCount));
					toSend.Add((GameConstants.Stats.DUO_RANKED_AIRDROP_OPENED, (int) thisPlayerData.Data.AirdropOpenedCount));
					toSend.Add((GameConstants.Stats.DUO_RANKED_SUPPLY_CRATES_OPENED, (int) thisPlayerData.Data.SupplyCrateOpenedCount));
					toSend.Add((GameConstants.Stats.DUO_RANKED_GUNS_COLLECTED, (int) thisPlayerData.Data.GunsCollectedCount));
					toSend.Add((GameConstants.Stats.DUO_RANKED_PICKUPS_COLLECTED, (int) thisPlayerData.Data.PickupCollectedCount));
					toSend.Add((GameConstants.Stats.DUO_LEADERBOARD_LADDER_NAME, trophies));
					break;
				case 4:
					toSend.Add((GameConstants.Stats.QUAD_RANKED_DAMAGE_DONE, (int) thisPlayerData.Data.DamageDone));
					toSend.Add((GameConstants.Stats.QUAD_RANKED_DEATHS, (int) thisPlayerData.Data.DeathCount));
					toSend.Add((GameConstants.Stats.QUAD_RANKED_KILLS, (int) thisPlayerData.Data.PlayersKilledCount));
					toSend.Add((GameConstants.Stats.QUAD_RANKED_AIRDROP_OPENED, (int) thisPlayerData.Data.AirdropOpenedCount));
					toSend.Add((GameConstants.Stats.QUAD_RANKED_SUPPLY_CRATES_OPENED, (int) thisPlayerData.Data.SupplyCrateOpenedCount));
					toSend.Add((GameConstants.Stats.QUAD_RANKED_GUNS_COLLECTED, (int) thisPlayerData.Data.GunsCollectedCount));
					toSend.Add((GameConstants.Stats.QUAD_RANKED_PICKUPS_COLLECTED, (int) thisPlayerData.Data.PickupCollectedCount));
					toSend.Add((GameConstants.Stats.QUAD_LEADERBOARD_LADDER_NAME, trophies));
					break;
			}
		}

		private static void CalculatePersistentStatistics(List<(string, int)> toSend, QuantumPlayerMatchData thisPlayerData, uint teamSize)
		{
			toSend.Add((GameConstants.Stats.RANKED_DAMAGE_DONE_EVER, (int) thisPlayerData.Data.DamageDone));
			toSend.Add((GameConstants.Stats.RANKED_DEATHS_EVER, thisPlayerData.Data.DeathCount));
			toSend.Add((GameConstants.Stats.RANKED_KILLS_EVER, thisPlayerData.Data.PlayersKilledCount));
			toSend.Add((GameConstants.Stats.RANKED_AIRDROP_OPENED_EVER, thisPlayerData.Data.AirdropOpenedCount));
			toSend.Add((GameConstants.Stats.RANKED_SUPPLY_CRATES_OPENED_EVER, thisPlayerData.Data.SupplyCrateOpenedCount));
			toSend.Add((GameConstants.Stats.RANKED_GUNS_COLLECTED_EVER, thisPlayerData.Data.GunsCollectedCount));
			toSend.Add((GameConstants.Stats.RANKED_PICKUPS_COLLECTED_EVER, thisPlayerData.Data.PickupCollectedCount));
		}

		private static void CalculateMatchPlayedAndWinStatistics(List<(string, int)> toSend, QuantumPlayerMatchData firstPlacePlayerData, QuantumPlayerMatchData thisPlayerData, uint teamSize)
		{
			if (HasPlayerWin(firstPlacePlayerData, thisPlayerData))
			{
				toSend.Add((GameConstants.Stats.RANKED_GAMES_WON_EVER, 1));
				toSend.Add((GameConstants.Stats.RANKED_GAMES_WON, 1));

				switch (teamSize)
				{
					case 1: 
						toSend.Add((GameConstants.Stats.SOLO_RANKED_GAMES_WON, 1));
						break;
					case 2: 
						toSend.Add((GameConstants.Stats.DUO_RANKED_GAMES_WON, 1));
						break;
					case 4: 
						toSend.Add((GameConstants.Stats.QUAD_RANKED_GAMES_WON, 1));
						break;
				}
			}
			
			toSend.Add((GameConstants.Stats.RANKED_GAMES_PLAYED_EVER, 1));
			toSend.Add((GameConstants.Stats.RANKED_GAMES_PLAYED, 1));
			switch (teamSize)
			{
				case 1: 
					toSend.Add((GameConstants.Stats.SOLO_RANKED_GAMES_PLAYED, 1));
					break;
				case 2: 
					toSend.Add((GameConstants.Stats.DUO_RANKED_GAMES_PLAYED, 1));
					break;
				case 4: 
					toSend.Add((GameConstants.Stats.QUAD_RANKED_GAMES_PLAYED, 1));
					break;
			}
		}

		private static bool HasPlayerWin(QuantumPlayerMatchData firstPlayer, QuantumPlayerMatchData thisPlayerData)
		{
			return (firstPlayer.Data.IsValid && thisPlayerData.TeamId == firstPlayer.TeamId && thisPlayerData.TeamId > Constants.TEAM_ID_NEUTRAL) ||
				    thisPlayerData.PlayerRank == 1;
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
			playerData.Currencies.TryGetValue(GameId.NOOB, out var noobs);

			var newTrophies = await CheckUpdateTrophiesState(playerLoadEvent.PlayerId, state);
			// TODO: Check possible bug here, i think we are duplicating the trophies
			if (newTrophies != playerData.Trophies)
			{
				playerData.Trophies = newTrophies;
				state.UpdateModel(playerData);
			}
			
			await _ctx.Statistics.UpdateStatistics(playerLoadEvent.PlayerId,
				(GameConstants.Stats.COINS_TOTAL, (int) coins),
				(GameConstants.Stats.NOOB_TOTAL, (int) noobs),
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
			var currentSeason = await _ctx.Statistics.GetSeason(GameConstants.Stats.RANKED_LEADERBOARD_LADDER_NAME);
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