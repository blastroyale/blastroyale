using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Models;
using Newtonsoft.Json;

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
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.LEADERBOARD_LADDER_NAME, false);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.GAMES_PLAYED, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.GAMES_WON, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.KILLS, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.DEATHS, true);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.NFT_ITEMS, false);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.NON_NFTS, false);
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.BROKEN_ITEMS, false);
			
			var evManager = _ctx.PluginEventManager!;
			evManager.RegisterListener<PlayerDataLoadEvent>(OnPlayerLoaded);
			evManager.RegisterListener<CommandFinishedEvent>(OnCommandFinished);
		}

		private void OnCommandFinished(CommandFinishedEvent ev)
		{
			var toSend = new List<ValueTuple<string, int>>();
			if (ev.Command is EndOfGameCalculationsCommand endGameCmd)
			{
				var thisPlayerData = endGameCmd.PlayersMatchData[endGameCmd.QuantumValues.ExecutingPlayer];
				if (thisPlayerData.PlayerRank == 1)
				{
					toSend.Add((GameConstants.Stats.GAMES_WON, 1));
				}
				toSend.Add((GameConstants.Stats.GAMES_PLAYED, 1));
				toSend.Add((GameConstants.Stats.KILLS,  (int)thisPlayerData.Data.PlayersKilledCount));
				toSend.Add((GameConstants.Stats.DEATHS, (int)thisPlayerData.Data.DeathCount));
			} else if (ev.Command is CollectUnclaimedRewardsCommand claimCommand)
			{
				var trophies = (int)ev.PlayerState.DeserializeModel<PlayerData>().Trophies;
				toSend.Add((GameConstants.Stats.LEADERBOARD_LADDER_NAME, trophies));
			}

			if (toSend.Count > 0)
			{
				_ctx.Statistics.UpdateStatistics(ev.PlayerId, toSend.ToArray());
			}
		}

		public void OnPlayerLoaded(PlayerDataLoadEvent playerLoadEvent)
		{
			if (!playerLoadEvent.PlayerState.Has<EquipmentData>())
			{
				return;
			}
			var equipmentData = playerLoadEvent.PlayerState.DeserializeModel<EquipmentData>();
			_ctx.Statistics.UpdateStatistics(playerLoadEvent.PlayerId,
				(GameConstants.Stats.NFT_ITEMS, equipmentData.NftInventory.Count),
				(GameConstants.Stats.NON_NFTS, equipmentData.Inventory.Count - equipmentData.NftInventory.Count), 
				(GameConstants.Stats.BROKEN_ITEMS, equipmentData.Inventory.Values.Count(e => e.IsBroken())));
		}
	}
}