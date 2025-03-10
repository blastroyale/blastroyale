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
		public readonly IEventManager PluginEventManager;
		public readonly IPluginLogger Log;
		public readonly IServerStateService ServerState;
		public readonly IMetricsService Metrics;
		public readonly IServerAnalytics Analytics;
		public readonly IBaseServiceConfiguration ServerConfig;
		public readonly IConfigsProvider GameConfig;
		public readonly IStatisticsService Statistics;
		public readonly IServerPlayerProfileService? PlayerProfile;
		
		public PluginContext(IEventManager evManager, IServiceProvider services)
		{
			PluginEventManager = evManager;
			Log = (IPluginLogger)services.GetService(typeof(IPluginLogger));
			ServerState = (IServerStateService)services.GetService(typeof(IServerStateService));
			Metrics = (IMetricsService)services.GetService(typeof(IMetricsService));
			Analytics = (IServerAnalytics)services.GetService(typeof(IServerAnalytics));
			ServerConfig = (IBaseServiceConfiguration)services.GetService(typeof(IBaseServiceConfiguration));
			GameConfig = (IConfigsProvider)services.GetService(typeof(IConfigsProvider));
			Statistics = (IStatisticsService)services.GetService(typeof(IStatisticsService));
			PlayerProfile = (IServerPlayerProfileService)services.GetService(typeof(IServerPlayerProfileService));
		}
	}
}


