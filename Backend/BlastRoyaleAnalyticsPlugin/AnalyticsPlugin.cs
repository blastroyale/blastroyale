using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Models;

namespace BlastRoyaleNFTPlugin
{
	/// <summary>
	/// Listen to server events and send analytics
	/// </summary>
	public class AnalyticsPlugin : ServerPlugin
	{
		private PluginContext _ctx;
	
		public override void OnEnable(PluginContext context)
		{
			_ctx = context;
			context.PluginEventManager.RegisterListener<CommandFinishedEvent>(OnCommandFinished);
		}

		public void OnCommandFinished(CommandFinishedEvent ev)
		{
			_ctx.Analytics.EmitUserEvent(ev.PlayerId, "server_player_gamestate_change", new AnalyticsData()
			{
				{ "old_state", ev.PlayerStateBeforeCommand },
				{ "current_state", ev.PlayerState }
			});
		}
	}
}

