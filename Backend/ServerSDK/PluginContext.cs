using FirstLight.Game.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ServerSDK.Models;
using ServerSDK.Services;


namespace ServerSDK;


/// <summary>
/// Setup objects necessary for a plugin to initialize.
/// This object holds hard-typed references to what can be accessed by a external plugin.
/// </summary>
public class PluginContext
{
	public readonly IEventManager PluginEventManager;
	public readonly ILogger Log;
	public readonly IServerStateService ServerState;
	public readonly IServerMutex PlayerMutex;
	public readonly IMetricsService Metrics;
	public readonly IServerAnalytics Analytics;

	public PluginContext(IEventManager evManager, IServiceProvider services)
	{
		PluginEventManager = evManager;
		Log = services.GetService<ILogger>()!;
		ServerState = services.GetService<IServerStateService>()!;
		PlayerMutex = services.GetService<IServerMutex>()!;
		Metrics = services.GetService<IMetricsService>()!;
		Analytics = services.GetService<IServerAnalytics>()!;
	}

	/// <summary>
	/// Registers custom data converters for specific game objects.
	/// </summary>
	public void RegisterCustomConverter(ServerPlugin plugin, JsonConverter converter)
	{
		ModelSerializer.RegisterConverter(converter);
		Log.LogInformation($"Plugin {plugin.GetType()} registered converter {converter.GetType()}");
	}
}