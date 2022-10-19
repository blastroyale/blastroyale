using System;
using System.IO;
using Backend.Db;
using Backend.Game;
using Backend.Game.Services;
using Backend.Plugins;
using Backend.Models;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PlayFab;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Server.SDK.Services;
using GameLogicService.Services;
using ServerCommon;

namespace Backend
{
	/// <summary>
	/// Setups up dependency injection context.
	/// This is where we can declare specific implementations of the server like for instance, where we read data from.
	/// Currently, its all setup for Playfab.
	/// </summary>
	public static class ServerStartup
	{
		public static void Setup(IServiceCollection services, string appPath)
		{
			var envConfig = new EnvironmentVariablesConfigurationService(appPath);

			DbSetup.Setup(services, envConfig);
			var pluginLoader = new PluginLoader();
			var insightsConnection = envConfig.TelemetryConnectionString;
			if (insightsConnection != null)
			{
				services.AddApplicationInsightsTelemetry(o => o.ConnectionString = insightsConnection);
				services.AddSingleton<IMetricsService, AppInsightsMetrics>();
			}
			else
			{
				services.AddSingleton<IMetricsService, NoMetrics>();
			}
			services.AddSingleton<ShopService>();
			services.AddSingleton<IServerAnalytics, PlaystreamAnalyticsService>();
			services.AddSingleton<IPlayerSetupService, DefaultPlayerSetupService>();
			services.AddSingleton<IPluginLogger, ServerPluginLogger>();
			services.AddSingleton<IErrorService<PlayFabError>, PlayfabErrorService>();
			services.AddSingleton<IServerConfiguration>(p => envConfig);
			services.AddSingleton<IServerStateService, PlayfabGameStateService>();
			services.AddSingleton<IGameConfigurationService, GameConfigurationService>();
			services.AddSingleton<IConfigBackendService, PlayfabConfigurationBackendService>();
			services.AddSingleton<ILogger, ILogger>(l =>
			{
				return l.GetService<ILoggerFactory>().CreateLogger(LogCategories.CreateFunctionUserCategory("Common"));
			});
			services.AddSingleton<IPlayfabServer, PlayfabServerSettings>();
			services.AddSingleton<ILogicWebService, GameLogicWebWebService>();
			services.AddSingleton<JsonConverter, StringEnumConverter>();
			services.AddSingleton<IServerCommahdHandler, ServerCommandHandler>();
			services.AddSingleton<GameServer>();
			services.AddSingleton<IStateMigrator<ServerState>, StateMigrations>();
			services.AddSingleton<IEventManager, PluginEventManager>(p =>
			{
				var pluginLogger = p.GetService<IPluginLogger>();
				var eventManager = new PluginEventManager(pluginLogger);
				var pluginSetup = new PluginContext(eventManager, p);
				pluginLoader.LoadPlugins(pluginSetup, appPath, services);
				return eventManager;
			});
			services.AddSingleton<IConfigsProvider>(SetupConfigsProvider);
			pluginLoader.LoadServerSetup(services);
		}

		private static IConfigsProvider SetupConfigsProvider(IServiceProvider services)
		{
			var log = services.GetService<ILogger>();
			services.GetService<IPlayfabServer>(); 
			var cfgSerializer = new ConfigsSerializer();
			var env = services.GetService<IServerConfiguration>();
			if (!env.RemoteGameConfiguration)
			{
				log.Log(LogLevel.Information, "Starting server with baked configs");
				var bakedConfigs = File.ReadAllText(Path.Combine(env.AppPath, "gameConfig.json"));
				return cfgSerializer.Deserialize<ConfigsProvider>(bakedConfigs);
			}
			log.Log(LogLevel.Information, "Downloading remote configurations");
			var cfgBackend = services.GetService<IConfigBackendService>();
			var task = cfgBackend.GetRemoteVersion();
			task.Wait();
			var version = task.Result;
			var task2 = cfgBackend.FetchRemoteConfiguration(version);
			task2.Wait();
			return task2.Result;
		}
	}
}

