using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
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
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.COINS_TOTAL, false);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.CS_EARNED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.COINS_EARNED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.XP_EARNED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.BPP_EARNED, true);

			var evManager = _ctx.PluginEventManager!;
			evManager.RegisterEventListener<GameLogicMessageEvent<ClaimedRewardsMessage>>(OnClaimRewards);
			evManager.RegisterEventListener<GameLogicMessageEvent<RewardClaimedMessage>>(OnPurchase);
			evManager.RegisterEventListener<GameLogicMessageEvent<BattlePassLevelUpMessage>>(OnBattlePassLevel);
			evManager.RegisterEventListener<PlayerDataLoadEvent>(OnPlayerLoaded);
			evManager.RegisterCommandListener<EndOfGameCalculationsCommand>(OnEndGameCalculations);
			evManager.RegisterCommandListener<ScrapItemCommand>(OnScrap);
			evManager.RegisterCommandListener<UpgradeItemCommand>(OnUpgrade);
		}

		private void OnScrap(string userId, ScrapItemCommand endGameCmd, ServerState state)
		{
			_ctx.Statistics.UpdateStatistics(userId, (GameConstants.Stats.ITEM_SCRAPS, 1), (GameConstants.Stats.ITEM_SCRAPS_EVER, 1));
		}
		
		private void OnUpgrade(string userId, UpgradeItemCommand endGameCmd, ServerState state)
		{
			_ctx.Statistics.UpdateStatistics(userId, (GameConstants.Stats.ITEM_UPGRADES, 1), (GameConstants.Stats.ITEM_UPGRADES_EVER, 1));
		}

		private async Task OnClaimRewards(GameLogicMessageEvent<ClaimedRewardsMessage> ev)
		{
			TrackRewards(ev.PlayerId, ev.Message.Rewards);
		}

		private void TrackRewards(string playerId, IEnumerable<ItemData> rewards)
		{
			var coins = 0;
			var cs = 0;
			var equipments = 0;
			var xp = 0;
			var bpp = 0;

			foreach (var item in rewards)
			{
				if (item.Id == GameId.COIN) coins += item.GetMetadata<CurrencyMetadata>().Amount;
				if (item.Id == GameId.CS) cs += item.GetMetadata<CurrencyMetadata>().Amount;
				if (item.Id == GameId.XP) xp += item.GetMetadata<CurrencyMetadata>().Amount;
				if (item.Id.IsInGroup(GameIdGroup.Equipment)) equipments += 1;
				if (item.Id == GameId.BPP) bpp += item.GetMetadata<CurrencyMetadata>().Amount;
			}
			
			if (cs == 0 && coins == 0 && equipments == 0 && xp == 0 && bpp == 0) return;
			
			_ctx.Statistics.UpdateStatistics(playerId, 
				(GameConstants.Stats.COINS_EARNED, coins),
				(GameConstants.Stats.XP_EARNED, xp), 
				(GameConstants.Stats.BPP_EARNED, bpp), 
				(GameConstants.Stats.CS_EARNED, cs), 
				(GameConstants.Stats.ITEMS_OBTAINED, equipments));
		}

		private async Task OnBattlePassLevel(GameLogicMessageEvent<BattlePassLevelUpMessage> ev)
		{
			TrackRewards(ev.PlayerId, ev.Message.Rewards);
		}
		
		private async Task OnPurchase(GameLogicMessageEvent<RewardClaimedMessage> ev)
		{
			_ctx.Statistics.UpdateStatistics(ev.PlayerId, (GameConstants.Stats.ITEMS_OBTAINED, 1));
		}

		private void OnEndGameCalculations(string userId, EndOfGameCalculationsCommand endGameCmd, ServerState state)
		{
			var toSend = new List<ValueTuple<string, int>>();
			var trophies = (int)state.DeserializeModel<PlayerData>().Trophies;
			var thisPlayerData = endGameCmd.PlayersMatchData[endGameCmd.QuantumValues.ExecutingPlayer];
			var firstPlayer = endGameCmd.PlayersMatchData.FirstOrDefault(p => p.PlayerRank == 1);
			var isWin = false;
			var ranked = endGameCmd.QuantumValues.MatchType == MatchType.Matchmaking;
			
			if (firstPlayer.Data.IsValid && thisPlayerData.TeamId == firstPlayer.TeamId && thisPlayerData.TeamId > Constants.TEAM_ID_NEUTRAL)
			{
				isWin = true;
			}
			else if(thisPlayerData.PlayerRank == 1)
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
				toSend.Add((GameConstants.Stats.RANKED_KILLS_EVER,  (int)thisPlayerData.Data.PlayersKilledCount));
				toSend.Add((GameConstants.Stats.RANKED_GAMES_PLAYED, 1));
				toSend.Add((GameConstants.Stats.RANKED_KILLS,  (int)thisPlayerData.Data.PlayersKilledCount));
			}
			toSend.Add((GameConstants.Stats.GAMES_PLAYED_EVER, 1));
			toSend.Add((GameConstants.Stats.KILLS_EVER,  (int)thisPlayerData.Data.PlayersKilledCount));
			toSend.Add((GameConstants.Stats.GAMES_PLAYED, 1));
			toSend.Add((GameConstants.Stats.KILLS,  (int)thisPlayerData.Data.PlayersKilledCount));
			toSend.Add((GameConstants.Stats.DEATHS, (int)thisPlayerData.Data.DeathCount));
			toSend.Add((GameConstants.Stats.LEADERBOARD_LADDER_NAME, trophies));
			_ctx.Statistics.UpdateStatistics(userId, toSend.ToArray());
		}

		public async Task OnPlayerLoaded(PlayerDataLoadEvent playerLoadEvent)
		{
			if (!playerLoadEvent.PlayerState.Has<EquipmentData>())
			{
				return;
			}
			var equipmentData = playerLoadEvent.PlayerState.DeserializeModel<EquipmentData>();
			var playerData = playerLoadEvent.PlayerState.DeserializeModel<PlayerData>();
			playerData.Currencies.TryGetValue(GameId.CS, out var cs);
			playerData.Currencies.TryGetValue(GameId.COIN, out var coins);
			_ctx.Statistics.UpdateStatistics(playerLoadEvent.PlayerId,
				(GameConstants.Stats.NFT_ITEMS, equipmentData.NftInventory.Count),
				(GameConstants.Stats.NON_NFTS, equipmentData.Inventory.Count - equipmentData.NftInventory.Count), 
				(GameConstants.Stats.CS_TOTAL, (int)cs), 
				(GameConstants.Stats.COINS_TOTAL, (int)coins), 
				(GameConstants.Stats.FAME, (int)playerData.Level), 
				(GameConstants.Stats.BROKEN_ITEMS, equipmentData.Inventory.Values.Count(e => e.IsBroken())));
		}
	}
}