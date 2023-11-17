using System;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Server.SDK.Services;
using FirstLightServerSDK.Modules;
using FirstLightServerSDK.Services;
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
		public readonly IBaseServiceConfiguration? ServerConfig;
		public readonly IConfigsProvider? GameConfig;
		public readonly IStatisticsService? Statistics;
		public readonly IDataSynchronizer? DataSyncs;
		public readonly IServerPlayerProfileService? PlayerProfile;
		public readonly IInventorySyncService? InventorySync;
		
		public PluginContext(IEventManager evManager, IServiceProvider services)
		{
			PluginEventManager = evManager;
			Log = services.GetService(typeof(IPluginLogger)) as IPluginLogger;
			ServerState = services.GetService(typeof(IServerStateService)) as IServerStateService;
			PlayerMutex = services.GetService(typeof(IServerMutex)) as IServerMutex;
			Metrics = services.GetService(typeof(IMetricsService)) as IMetricsService;
			Analytics = services.GetService(typeof(IServerAnalytics)) as IServerAnalytics;
			ServerConfig = services.GetService(typeof(IBaseServiceConfiguration)) as IBaseServiceConfiguration;
			GameConfig = services.GetService(typeof(IConfigsProvider)) as IConfigsProvider;
			Statistics = services.GetService(typeof(IStatisticsService)) as IStatisticsService;
			DataSyncs = services.GetService(typeof(IDataSynchronizer)) as IDataSynchronizer;
			PlayerProfile = services.GetService(typeof(IServerPlayerProfileService)) as IServerPlayerProfileService;
			InventorySync = services.GetService(typeof(IInventorySyncService)) as IInventorySyncService;
		}
	}
}


