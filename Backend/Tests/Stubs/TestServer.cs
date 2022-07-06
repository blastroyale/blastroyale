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
using ServerSDK.Services;

namespace Tests.Stubs;

/// <summary>
/// Represents what is needed to run a isolated server testing environment.
/// </summary>
public class TestServer
{	
	private IServiceProvider _services;
	private IDataProvider _data;
	private GameServerLogic _logic;
	private string? _testPlayerId = null;

	public TestServer()
	{
		SetupTestEnv();
		_services = SetupServices().BuildServiceProvider();
		var cfg = GetService<IConfigsProvider>();
		var data = GetService<IDataProvider>();
		_logic = new GameServerLogic(cfg, data);
		_logic.Init();
	}
	
	public IServerStateService ServerState => GetService<IServerStateService>()!;

	public IServiceProvider Services => _services;

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
			services.RemoveAll(typeof(IServerStateService)); 
			services.RemoveAll(typeof(ITestPlayerSetup));
			services.AddSingleton<IServerStateService, InMemoryPlayerState>();
			services.AddSingleton<ITestPlayerSetup, InMemoryTestSetup>();
		});
	}
	
	/// <summary>
	/// Serializes and sends a test command to the server.
	/// </summary>
	public BackendLogicResult? SendTestCommand(IGameCommand cmd)
	{
		var commandData = new Dictionary<string, string>();
		commandData[CommandFields.Timestamp] = "1";
		commandData[CommandFields.ClientVersion] = ServerConfiguration.GetConfig().MinClientVersion;
		commandData[CommandFields.Command] = ModelSerializer.Serialize(cmd).Value;
		return GetService<GameServer>()?.RunLogic(GetTestPlayerID(), new LogicRequest()
		{
			Command = cmd.GetType().FullName,
			Data = commandData,
		});
	}

	private IServiceCollection SetupServices()
	{
		var services = new ServiceCollection();
		var logger = new LoggerFactory().CreateLogger("Log");
		var testAppPath = Path.GetDirectoryName(typeof(ServerConfiguration).Assembly.Location);
		ServerStartup.Setup(services, logger, testAppPath);
		services.AddSingleton<IDataProvider, ServerTestData>();
		services.AddSingleton<ITestPlayerSetup, TestPlayerSetup>();
		return services;
	}
	
	private void SetupTestEnv()
	{
		Environment.SetEnvironmentVariable("SqlConnectionString", "Server=localhost;Database=localDatabase;Port=5432;User Id=postgres;Password=localPassword;Ssl Mode=Allow;");
		Environment.SetEnvironmentVariable("API_URL", "stub-api", EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("API_BLOCKCHAIN_SERVICE", "stub-service", EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("API_SECRET", "stub-key", EnvironmentVariableTarget.Process);
	}

}