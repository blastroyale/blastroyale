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
		public IEventManager PluginEventManager;
		public IPluginLogger Log;
		public IServerStateService ServerState;
		public IMetricsService Metrics;
		public IServerAnalytics Analytics;
		public IBaseServiceConfiguration ServerConfig;
		public IConfigsProvider GameConfig;
		public IStatisticsService Statistics;
		public IServerPlayerProfileService? PlayerProfile;
		
		public void SetupContext(IEventManager evManager, IServiceProvider services)
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


