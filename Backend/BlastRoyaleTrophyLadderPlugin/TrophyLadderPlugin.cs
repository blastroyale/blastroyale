using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using PlayFab;
using PlayFab.AdminModels;
using PlayFab.ServerModels;
using ServerSDK;
using ServerSDK.Events;
using UpdatePlayerStatisticsRequest = PlayFab.ServerModels.UpdatePlayerStatisticsRequest;

namespace BlastRoyaleNFTPlugin;

/// <summary>
/// Server plugin to handle leaderboards.
/// </summary>
public class TrophyLadderPlugin : ServerPlugin
{
	private const string RANK_NAME = "Trophies Ladder";
	
	public override void OnEnable(PluginContext context)
	{
		context.PluginEventManager.RegisterListener<CommandFinishedEvent>(OnCommandFinished);
		SetupLeaderboard();
	}

	private void SetupLeaderboard()
	{
		PlayFabAdminAPI.CreatePlayerStatisticDefinitionAsync(new CreatePlayerStatisticDefinitionRequest()
		{
			AggregationMethod = StatisticAggregationMethod.Last,
			StatisticName = RANK_NAME
		});
	}

	
	private void OnCommandFinished(CommandFinishedEvent ev)
	{
		if(!(ev.Command is EndOfGameCalculationsCommand))
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
					StatisticName = RANK_NAME
				}
			},
		});
	}
}