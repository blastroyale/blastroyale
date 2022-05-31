using System;
using System.IO;
using Backend;
using Backend.Db;
using Backend.Game;
using Backend.Game.Services;
using FirstLight;
using FirstLight.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
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

	private void SetupTestEnv()
	{
		Environment.SetEnvironmentVariable("SqlConnectionString", "Server=localhost;Database=localDatabase;Port=5432;User Id=postgres;Password=localPassword;Ssl Mode=Allow;");
		Environment.SetEnvironmentVariable("API_URL", "stub-api", EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("API_BLOCKCHAIN_SERVICE", "stub-service", EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("API_SECRET", "stub-key", EnvironmentVariableTarget.Process);
	}

	public void UpdateDependencies(Action<IServiceCollection> collectionAction)
	{
		var collection = SetupServices();
		collectionAction(collection);
		_services = collection.BuildServiceProvider();
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

	public T? GetService<T>()
	{
		return (T?)_services.GetService(typeof(T));
	}

	public IServerStateService ServerState => GetService<IServerStateService>()!;
	
	public string GetTestPlayerID()
	{
		if (_testPlayerId == null)
		{
			_testPlayerId = _services.GetService<ITestPlayerSetup>().GetTestPlayerId();
		}
		return _testPlayerId;
	}

	[NotNull]
	public IDataProvider Data
	{
		get => _data;
		set => _data = value ?? throw new ArgumentNullException(nameof(value));
	}
	
	[NotNull]
	public GameServerLogic Logic
	{
		get => _logic;
		set => _logic = value ?? throw new ArgumentNullException(nameof(value));
	}

	public IServiceProvider Services => _services;

}