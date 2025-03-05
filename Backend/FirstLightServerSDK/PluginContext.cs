using System;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Server.SDK.Services;
using FirstLightServerSDK.Modules;
using FirstLightServerSDK.Services;


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
		public readonly IMetricsService? Metrics;
		public readonly IServerAnalytics? Analytics;
		public readonly IBaseServiceConfiguration? ServerConfig;
		public readonly IConfigsProvider? GameConfig;
		public readonly IStatisticsService? Statistics;
		public readonly IServerPlayerProfileService? PlayerProfile;
		
		public PluginContext(IEventManager evManager, IServiceProvider services)
		{
			PluginEventManager = evManager;
			Log = services.GetService(typeof(IPluginLogger)) as IPluginLogger;
			ServerState = services.GetService(typeof(IServerStateService)) as IServerStateService;
			Metrics = services.GetService(typeof(IMetricsService)) as IMetricsService;
			Analytics = services.GetService(typeof(IServerAnalytics)) as IServerAnalytics;
			ServerConfig = services.GetService(typeof(IBaseServiceConfiguration)) as IBaseServiceConfiguration;
			GameConfig = services.GetService(typeof(IConfigsProvider)) as IConfigsProvider;
			Statistics = services.GetService(typeof(IStatisticsService)) as IStatisticsService;
			PlayerProfile = services.GetService(typeof(IServerPlayerProfileService)) as IServerPlayerProfileService;
		}
	}
}


