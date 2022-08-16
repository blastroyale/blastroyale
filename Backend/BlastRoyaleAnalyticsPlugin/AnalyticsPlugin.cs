using FirstLight.Game.Commands;
using Newtonsoft.Json;
using ServerSDK;
using ServerSDK.Events;
using ServerSDK.Models;

namespace BlastRoyaleNFTPlugin;

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
		_ctx.Analytics.EmitUserEvent(ev.PlayerId, ev.Command.GetType().Name, new AnalyticsData()
		{
			{ "CommandData", ev.CommandData },
		});
		_ctx.Analytics.EmitUserEvent(ev.PlayerId, "StateUpdate", new AnalyticsData(ev.PlayerState));

	}
}