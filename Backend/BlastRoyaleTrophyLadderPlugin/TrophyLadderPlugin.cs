using System;
using System.Collections.Generic;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Utils;
using PlayFab;
using PlayFab.AdminModels;
using PlayFab.ServerModels;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using UpdatePlayerStatisticsRequest = PlayFab.ServerModels.UpdatePlayerStatisticsRequest;

namespace BlastRoyaleNFTPlugin
{
	/// <summary>
	/// Server plugin to handle leaderboards.
	/// </summary>
	public class TrophyLadderPlugin : ServerPlugin
	{
		private PluginContext _ctx;

		private static HashSet<Type> _commandTypesToUpdateLeaderboard = new HashSet<Type>()
		{
			typeof(EndOfGameCalculationsCommand),
			typeof(CollectUnclaimedRewardsCommand)
		};
		
		public override void OnEnable(PluginContext context)
		{
			_ctx = context;
			context.PluginEventManager.RegisterListener<CommandFinishedEvent>(OnCommandFinished);
			SetupLeaderboard();
		}

		private void SetupLeaderboard()
		{
			PlayFabAdminAPI.CreatePlayerStatisticDefinitionAsync(new CreatePlayerStatisticDefinitionRequest()
			{
				AggregationMethod = StatisticAggregationMethod.Last,
				StatisticName = GameConstants.Network.LEADERBOARD_LADDER_NAME
			});
		}

		private void OnCommandFinished(CommandFinishedEvent ev)
		{
			if (!_commandTypesToUpdateLeaderboard.Contains(ev.Command.GetType()))
			{
				return;
			}
		
			PlayFabServerAPI.UpdatePlayerStatisticsAsync(new UpdatePlayerStatisticsRequest()
			{
				PlayFabId = ev.PlayerId,
				Statistics = new List<StatisticUpdate>()
				{
					new StatisticUpdate()
					{
						Value = (int)ev.PlayerState.DeserializeModel<PlayerData>().Trophies,
						StatisticName = GameConstants.Network.LEADERBOARD_LADDER_NAME
					}
				},
			}).ContinueWith(response =>
			{
				if (response.Result.Error != null)
				{
					_ctx.Log.LogError(response.Result.Error.GenerateErrorReport());
				}
			});
		}
	}
}

