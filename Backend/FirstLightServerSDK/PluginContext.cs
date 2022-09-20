using System;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Services;
using Newtonsoft.Json;


namespace FirstLight.Server.SDK
{
	/// <summary>
	/// Setup objects necessary for a plugin to initialize.
	/// This object holds hard-typed references to what can be accessed by a external plugin.
	/// </summary>
	public class PluginContext
	{
		public readonly IEventManager? PluginEventManager;
		public readonly IPluginLogger? Log;
		public readonly IServerStateService? ServerState;
		public readonly IServerMutex? PlayerMutex;
		public readonly IMetricsService? Metrics;
		public readonly IServerAnalytics? Analytics;
		public readonly IServerConfiguration? ServerConfig;

		public PluginContext(IEventManager evManager, IServiceProvider services)
		{
			PluginEventManager = evManager;
			Log = services.GetService(typeof(IPluginLogger)) as IPluginLogger;
			ServerState = services.GetService(typeof(IServerStateService)) as IServerStateService;
			PlayerMutex = services.GetService(typeof(IServerMutex)) as IServerMutex;
			Metrics = services.GetService(typeof(IMetricsService)) as IMetricsService;
			Analytics = services.GetService(typeof(IServerAnalytics)) as IServerAnalytics;
			ServerConfig = services.GetService(typeof(IServerConfiguration)) as IServerConfiguration;
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
}


