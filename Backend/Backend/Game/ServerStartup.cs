using System.IO;
using Backend.Db;
using Backend.Game.Services;
using FirstLight;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PlayFab;

namespace Backend.Game;

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
		services.AddSingleton<JsonConverter, StringEnumConverter>();
		services.AddSingleton<IServerCommahdHandler, ServerCommandHandler>();
		services.AddSingleton<GameServer>();
		services.AddSingleton<IConfigsProvider, ConfigsProvider>(p =>
		{
			var cfgSerializer = new ConfigsSerializer();
			var bakedConfigs = File.ReadAllText(Path.Combine(appPath, "gameConfig.json"));
			var cfg = cfgSerializer.Deserialize<ServerConfigsProvider>(bakedConfigs);
			return cfg;
		});
	}
	
}