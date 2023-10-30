using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;


namespace Src.FirstLight.Server
{
	/// <summary>
	/// Plugin implementation to reset player battle pass points when the season changes
	/// </summary>
	public class SeasonResetPlugin : ServerPlugin
	{
		private PluginContext _ctx;

		public override void OnEnable(PluginContext context)
		{
			_ctx = context;
			_ctx.PluginEventManager.RegisterEventListener<PlayerDataLoadEvent>(OnPlayerLoad);
		}


		private async Task OnPlayerLoad(PlayerDataLoadEvent ev)
		{
			await CheckForSeasonUpdate(ev.PlayerId);
		}

		private async Task CheckForSeasonUpdate(string playfabId)
		{
			
			try
			{
				await _ctx.PlayerMutex.Lock(playfabId);

				var state = await _ctx.ServerState.GetPlayerState(playfabId);
				if (!state.Has<PlayerData>())
				{
					return;
				}
				var currentSeason = _ctx.GameConfig.GetConfig<BattlePassConfig>().CurrentSeason;
				var seasonData = state.DeserializeModel<SeasonData>();
				var playerSeason = seasonData.CurrentSeason;
				if (playerSeason < currentSeason)
				{
					var data = state.DeserializeModel<BattlePassData>();
					data.BPLevel = 0;
					data.BPPoints = 0;
					seasonData.CurrentSeason = currentSeason;
					state.UpdateModel(seasonData);
					_ctx.Log.LogInformation(
						$@"Resetting player {playfabId} battle pass, Player Season {playerSeason} & Current Season {currentSeason}");
					state.UpdateModel(data);
					await _ctx.ServerState.UpdatePlayerState(playfabId, state);
				}
			}
			finally
			{
				_ctx.PlayerMutex.Unlock(playfabId);
			}
		}
	}
}