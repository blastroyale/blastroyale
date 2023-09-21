using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
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

			var evManager = _ctx.PluginEventManager!;
			evManager.RegisterEventListener<GameLogicMessageEvent<ClaimedRewardsMessage>>(OnClaimRewards);
			evManager.RegisterEventListener<GameLogicMessageEvent<IAPPurchaseCompletedMessage>>(OnPurchase);
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
			var coins = ev.Message.Rewards.Where(r => r.RewardId == GameId.COIN).Sum(r => r.Value);
			var cs = ev.Message.Rewards.Where(r => r.RewardId == GameId.CS).Sum(r => r.Value);
			if (coins > 0 || cs > 0)
			{
				_ctx.Statistics.UpdateStatistics(ev.PlayerId, (GameConstants.Stats.COINS_EARNED, coins),
					(GameConstants.Stats.CS_EARNED, cs));
			}
			
			
		}

		private async Task OnBattlePassLevel(GameLogicMessageEvent<BattlePassLevelUpMessage> ev)
		{
			var equipments = ev.Message.Rewards.Count();
			// TODO: Track other rewards after bpp rewards PR
			if (equipments > 0)
			{
				_ctx.Statistics.UpdateStatistics(ev.PlayerId, (GameConstants.Stats.ITEMS_OBTAINED, equipments));
			}
		}
		
		private async Task OnPurchase(GameLogicMessageEvent<IAPPurchaseCompletedMessage> ev)
		{
			_ctx.Statistics.UpdateStatistics(ev.PlayerId, (GameConstants.Stats.ITEMS_OBTAINED, 1));
		}

		private void OnEndGameCalculations(string userId, EndOfGameCalculationsCommand endGameCmd, ServerState state)
		{
			var toSend = new List<ValueTuple<string, int>>();
			var trophies = (int)state.DeserializeModel<PlayerData>().Trophies;
			var thisPlayerData = endGameCmd.PlayersMatchData[endGameCmd.QuantumValues.ExecutingPlayer];
			var ranked = endGameCmd.QuantumValues.MatchType == MatchType.Matchmaking;
			if (thisPlayerData.PlayerRank == 1)
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