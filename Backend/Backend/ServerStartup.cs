using System.IO;
using Backend.Db;
using Backend.Game;
using Backend.Game.Services;
using Backend.Plugins;
using Backend.Models;
using FirstLight;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PlayFab;
using ServerSDK;
using ServerSDK.Models;
using ServerSDK.Services;
using Backend.Services;

namespace Backend;

/// <summary>
/// Setups up dependency injection context.
/// This is where we can declare specific implementations of the server like for instance, where we read data from.
/// Currently, its all setup for Playfab.
/// </summary>
public static class ServerStartup
{
	public static void Setup(IServiceCollection services, ILogger log, string appPath)
	{
		ServerConfiguration.LoadConfiguration(appPath);
		DbSetup.Setup(services);
		
		services.AddSingleton<IPlayerSetupService, PlayerSetupService>();
		services.AddSingleton<IErrorService<PlayFabError>, PlayfabErrorService>();
		services.AddSingleton<IServerStateService, PlayfabGameStateService>();
		services.AddSingleton<ILogger, ILogger>(l => log);
		services.AddSingleton<IPlayfabServer, PlayfabServerSettings>();
		services.AddSingleton<ILogicWebService, GameLogicWebWebService>();
		services.AddSingleton<JsonConverter, StringEnumConverter>();
		services.AddSingleton<IServerCommahdHandler, ServerCommandHandler>();
		services.AddSingleton<IEncryptionService, SimpleSha1Encryption>();
		services.AddSingleton<GameServer>();
		services.AddSingleton<IStateMigrator<ServerState>, StateMigrations>();
		services.AddSingleton<IEventManager, PluginEventManager>(p =>
		{
			var eventManager = new PluginEventManager(log);
			var pluginSetup = new PluginContext(eventManager, p);
			var pluginLoader = new PluginLoader(p);
			pluginLoader.LoadServerPlugins(pluginSetup, appPath);
			return eventManager;
		});
		services.AddSingleton<IConfigsProvider, ConfigsProvider>(p =>
		{
			var cfgSerializer = new ConfigsSerializer();
			var bakedConfigs = File.ReadAllText(Path.Combine(appPath, "gameConfig.json"));
			var cfg = cfgSerializer.Deserialize<ServerConfigsProvider>(bakedConfigs);
			return cfg;
		});
	}
}