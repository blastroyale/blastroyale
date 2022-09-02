using System;
using System.Collections.Generic;
using System.IO;
using Backend;
using Backend.Db;
using Backend.Game;
using Backend.Game.Services;
using FirstLight;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PlayFab;
using ServerSDK;
using ServerSDK.Models;
using ServerSDK.Modules;
using ServerSDK.Services;
using StackExchange.Redis;

/// <summary>
/// Represents what is needed to run a isolated server testing environment.
/// </summary>
public class TestServer
{	
	private IServiceProvider _services;
	private IDataProvider _data;
	private GameServerLogic _logic;
	private string? _testPlayerId = null;
	private PluginContext _pluginCtx;

	public TestServer()
	{
		SetupTestEnv();
		_services = SetupServices().BuildServiceProvider();
		var cfg = GetService<IConfigsProvider>();
		var data = GetService<IDataProvider>();
		var eventManager = GetService<IEventManager>();
		_logic = new GameServerLogic(cfg, data);
		_logic.Init();
		_pluginCtx = new PluginContext(eventManager, Services);
	}

	public void RegisterTestPlugin(ServerPlugin plugin)
	{
		plugin.OnEnable(_pluginCtx);
	}
	
	public IServerStateService ServerState => GetService<IServerStateService>()!;

	public IServiceProvider Services => _services;

	public PluginContext PluginContext => _pluginCtx;
	/// <summary>
	/// Obtains a test player id that is setup to be used in tests.
	/// The player should already exists and be ready to use.
	/// </summary>
	public string GetTestPlayerID()
	{
		if (_testPlayerId == null)
		{
			_testPlayerId = _services.GetService<ITestPlayerSetup>().GetTestPlayerId();
		}
		return _testPlayerId;
	}

	/// <summary>
	/// Gets current instance of game logic
	/// </summary>
	public IGameLogic Logic => _logic;

	/// <summary>
	/// Updates server dependencies to be stubbed or modified for specific unit testing.
	/// </summary>
	public void UpdateDependencies(Action<IServiceCollection> collectionAction)
	{
		var collection = SetupServices();
		collectionAction(collection);
		_services = collection.BuildServiceProvider();
	}

	/// <summary>
	/// Obtains the given service from server.
	/// Might return null if service is not present.
	/// </summary>
	public T? GetService<T>()
	{
		return (T?)_services.GetService(typeof(T));
	}

	/// <summary>
	/// Replaces all needed dependencies to run the server fully in-memory without any external requirements.
	/// </summary>
	public void SetupInMemoryServer()
	{
		UpdateDependencies(services =>
		{
			services.RemoveAll(typeof(IServerAnalytics)); 
			services.RemoveAll(typeof(IServerStateService)); 
			services.RemoveAll(typeof(ITestPlayerSetup));
			services.RemoveAll(typeof(IServerMutex));
			services.AddSingleton<IServerStateService, InMemoryPlayerState>();
			services.AddSingleton<ITestPlayerSetup, InMemoryTestSetup>();
			services.AddSingleton<IServerMutex, InMemoryMutex>();
			services.AddSingleton<IServerAnalytics, InMemoryAnalytics>();
		});
	}
	
	/// <summary>
	/// Serializes and sends a test command to the server.
	/// </summary>
	public BackendLogicResult? SendTestCommand(IGameCommand cmd)
	{
		var commandData = new Dictionary<string, string>();
		commandData[CommandFields.Timestamp] = "1";
		commandData[CommandFields.ClientVersion] = GetService<IServerConfiguration>().MinClientVersion.ToString();
		commandData[CommandFields.Command] = ModelSerializer.Serialize(cmd).Value;
		commandData["SecretKey"] = PlayFabSettings.staticSettings.DeveloperSecretKey;
		return GetService<GameServer>()?.RunLogic(GetTestPlayerID(), new LogicRequest()
		{
			Command = cmd.GetType().FullName,
			Data = commandData,
		}).Result;
	}

	private IServiceCollection SetupServices()
	{
		var services = new ServiceCollection();
		var testAppPath = Path.GetDirectoryName(typeof(GameLogicWebWebService).Assembly.Location);
		ServerStartup.Setup(services, testAppPath);
		services.AddSingleton<IDataProvider, ServerTestData>();
		services.AddSingleton<ITestPlayerSetup, TestPlayerSetup>();
		services.RemoveAll<ILogger>();
		services.AddSingleton<ILogger>(p => new LoggerFactory().CreateLogger("Log"));
		return services;
	}
	
	private void SetupTestEnv()
	{
		Environment.SetEnvironmentVariable("SqlConnectionString", "Server=localhost;Database=localDatabase;Port=5432;User Id=postgres;Password=localPassword;Ssl Mode=Allow;");
		Environment.SetEnvironmentVariable("API_URL", "stub-api", EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("API_BLOCKCHAIN_SERVICE", "stub-service", EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("API_SECRET", "stub-key", EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", "***REMOVED***", EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("PLAYFAB_TITLE", "DDD52", EnvironmentVariableTarget.Process);
	}

}