using System;
using System.Net.Http;
using Backend.Db;
using Backend.Game;
using Backend.Game.Services;
using Backend.Plugins;
using Backend.Models;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PlayFab;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Server.SDK.Services;
using FirstLightServerSDK.Services;
using GameLogicService.Game;
using GameLogicService.Services;
using GameLogicService.Services.Providers;
using ServerCommon;
using ServerCommon.CommonServices;
using PluginManager = Backend.Plugins.PluginManager;

namespace Backend
{
	/// <summary>
	/// Setups up dependency injection context.
	/// This is where we can declare specific implementations of the server like for instance, where we read data from.
	/// Currently, its all setup for Playfab.
	/// </summary>
	public static class ServerStartup
	{
		public static EnvironmentVariablesConfigurationService Setup(IMvcBuilder builder, string appPath)
		{
			var envConfig = new EnvironmentVariablesConfigurationService(appPath);
			var services = builder.Services;
			DbSetup.Setup(services, envConfig);
			var pluginManager = new PluginManager();

			services.AddSingleton<IPluginManager>(f => pluginManager);
			services.AddSingleton<ShopService>();
			services.AddSingleton<IServerAnalytics, AnalyticsService>();
			services.AddSingleton<IAnalyticsProvider, PlayfabPlayStreamAnalyticsServiceProvider>();
			services.AddSingleton<IAnalyticsProvider, UnityAnalyticsServiceProvider>();
			services.AddSingleton<IPlayerSetupService, DefaultPlayerSetupService>();
			services.AddSingleton<IServerPlayerProfileService, PlayfabProfileService>();
			services.AddSingleton<IPluginLogger, ServerPluginLogger>();
			services.AddSingleton<IGameLogicContextService, GameLogicContextService>();
			services.AddSingleton<IErrorService<PlayFabError>, PlayfabErrorService>();
			services.AddSingleton<IStatisticsService, PlayfabStatisticsService>();
			services.AddSingleton<IServerStateService, PlayfabGameStateService>();
			services.AddSingleton<IGameConfigurationService, GameConfigurationService>();
			services.AddSingleton<IConfigBackendService, PlayfabConfigurationBackendService>();
			services.AddSingleton<ServerEnvironmentService>();
			services.AddSingleton<IInventorySyncService, PlayfabInventorySyncService>();
			services.AddSingleton<IPlayfabServer, PlayfabServerSettings>();
			services.AddSingleton<IStoreService, PlayfabServerStoreService>();
			services.AddSingleton<IItemCatalog<ItemData>, PlayfabItemCatalogService>();
			services.AddSingleton<ILogicWebService, GameLogicWebWebService>();
			services.AddSingleton<JsonConverter, StringEnumConverter>();
			services.AddSingleton<IServerCommahdHandler, ServerCommandHandler>();
			services.AddSingleton<GameServer>();
			services.AddSingleton<IStateMigrator<ServerState>, StateMigrations>();
			services.AddSingleton<UnityAuthService>();
			services.AddSingleton<UnityCloudService>();
			services.AddSingleton<IRemoteConfigService, UnityRemoteConfigService>();
			services.AddHttpClient();
			services.AddSingleton<IEventManager, PluginEventManager>(p =>
			{
				var pluginLogger = p.GetService<IPluginLogger>();
				var eventManager = new PluginEventManager(pluginLogger);
				var pluginSetup = new PluginContext(eventManager, p);
				pluginManager.LoadPlugins(pluginSetup, appPath, services);
				return eventManager;
			});
			services.AddSingleton<IConfigsProvider>(SetupConfigsProvider);
			builder.SetupSharedServices(appPath);
			pluginManager.LoadServerSetup(services);
			return envConfig;
		}

		private static IConfigsProvider SetupConfigsProvider(IServiceProvider services)
		{
			var log = services.GetService<ILogger>();
			var env = services.GetService<IBaseServiceConfiguration>();
			log.LogInformation("Build commit number is " + env.BuildCommit);
			services.GetService<IPlayfabServer>();
			return new EmbeddedConfigProvider();
		}
	}
}