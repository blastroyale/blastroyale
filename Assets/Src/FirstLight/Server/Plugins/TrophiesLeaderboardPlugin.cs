using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Models;


namespace Src.FirstLight.Server
{
	/// <summary>
	/// Implementation for trophy (ranked) statistics.
	// It follows a different implementation than other statistics because it's saved on playerdata
	/// execution.
	/// </summary>
	public class TrophyLeaderboardPlugin : ServerPlugin
	{
		private PluginContext _ctx;

		public override void OnEnable(PluginContext context)
		{
			_ctx = context;
			_ctx.Statistics.SetupStatistic(GameConstants.Stats.LEADERBOARD_LADDER_NAME, false);

			var evManager = _ctx.PluginEventManager!;
			evManager.RegisterEventListener<PlayerDataLoadEvent>(OnPlayerLoaded);
			evManager.RegisterEventListener<GameLogicMessageEvent<TrophiesUpdatedMessage>>(OnTrophiesUpdate);
		}

		private async Task OnTrophiesUpdate(GameLogicMessageEvent<TrophiesUpdatedMessage> ev)
		{
			var currentSeason = await _ctx.Statistics.GetSeasonAsync(GameConstants.Stats.LEADERBOARD_LADDER_NAME);
			var finalTrophies = ev.Message.NewValue;
			
			if (currentSeason != ev.Message.Season)
			{
				if (finalTrophies < ev.Message.OldValue) finalTrophies = 0;
				else finalTrophies = ev.Message.NewValue - ev.Message.OldValue;
				await ResetTrophies(ev.PlayerId, (uint)finalTrophies, (uint)currentSeason);
			}
			_ctx.Statistics.UpdateStatistics(ev.PlayerId, (GameConstants.Stats.LEADERBOARD_LADDER_NAME, (int)finalTrophies));
		}
		
		private async Task ResetTrophies(string userId, uint trophies, uint season)
		{
			try
			{
				await _ctx.PlayerMutex.Lock(userId);
				var state = await _ctx.ServerState.GetPlayerState(userId);
				var data = state.DeserializeModel<PlayerData>();
				data.Trophies = trophies;
				data.TrophySeason = season;
				state.UpdateModel(data);
				await _ctx.ServerState.UpdatePlayerState(userId, state);
			}
			catch (Exception e) 	
			{
				_ctx.Log.LogError(e.StackTrace);
			}
			finally
			{
				_ctx.PlayerMutex.Unlock(userId);
			}
		}
	
		public async Task OnPlayerLoaded(PlayerDataLoadEvent playerLoadEvent)
		{
			await CheckSeasonReset(playerLoadEvent.PlayerId, playerLoadEvent.PlayerState);
		}

		private async Task CheckSeasonReset(string userId, ServerState state)
		{
			var playerData = state.DeserializeModel<PlayerData>();
			var currentSeason = await _ctx.Statistics.GetSeasonAsync(GameConstants.Stats.LEADERBOARD_LADDER_NAME);
			if (currentSeason != playerData.TrophySeason)
			{
				await ResetTrophies(userId, 0, (uint)currentSeason);
			}
		}
	}
}