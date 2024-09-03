using System;
using System.Collections.Generic;
using System.IO;
using Backend;
using Backend.Game;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Serializers;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PlayFab;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Server.SDK.Services;
using GameLogicService.Services.Providers;
using Quantum;
using Environment = System.Environment;

/// <summary>
/// Represents what is needed to run a isolated server testing environment.
/// </summary>
public class TestServer
{
	private IServiceProvider _services;
	private string? _testPlayerId = null;

	public TestServer(IBaseServiceConfiguration cfg)
	{
		SetupTestEnv();
		_services = SetupServices().BuildServiceProvider();
		UpdateDependencies(services =>
		{
			services.RemoveAll(typeof(IBaseServiceConfiguration));
			services.AddSingleton<IBaseServiceConfiguration>(p => cfg);
		});
	}

	public TestServer()
	{
		SetupTestEnv();
		_services = SetupServices().BuildServiceProvider();
		ModelSerializer.RegisterConverter(new QuantumVector2Converter());
		ModelSerializer.RegisterConverter(new QuantumVector3Converter());
		ModelSerializer.RegisterConverter(new FPConverter());
	}

	public void GiveDefaultSkins()
	{
		var collectionData = new CollectionData()
		{
			OwnedCollectibles =
			{
				{ CollectionCategories.PLAYER_SKINS, new List<ItemData> { ItemFactory.Collection(GameId.FemaleAssassin) } }
			}
		};
		var serializedModel = ModelSerializer.Serialize(collectionData);
		var serverData = new ServerState();
		serverData.Add(serializedModel.Key, serializedModel.Value);
		var service = GetService<IServerStateService>();
		service?.UpdatePlayerState(GetTestPlayerID(), serverData).Wait();
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
		return (T?) _services.GetService(typeof(T));
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
			services.AddSingleton<IServerStateService>(p => new InMemoryPlayerState());
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
		commandData[CommandFields.ClientVersion] = GetService<IBaseServiceConfiguration>().MinClientVersion.ToString();
		commandData[CommandFields.CommandData] = ModelSerializer.Serialize(cmd).Value;
		commandData[CommandFields.CommandType] = cmd.GetType().FullName;
		commandData["SecretKey"] = PlayFabSettings.staticSettings.DeveloperSecretKey;
		return GetService<GameServer>()?.RunLogic(GetTestPlayerID(), new LogicRequest()
		{
			Data = commandData,
		}).Result;
	}

	private IServiceCollection SetupServices()
	{
		var services = new ServiceCollection();
		var testAppPath = Path.GetDirectoryName(typeof(GameLogicWebWebService).Assembly.Location);
		ServerStartup.Setup(services.AddMvc(), testAppPath);
		services.AddSingleton<IDataProvider, ServerTestData>();
		services.AddSingleton<ITestPlayerSetup, TestPlayerSetup>();
		services.RemoveAll<IAnalyticsProvider>();
		services.RemoveAll<ILogger>();
		services.AddSingleton<ILogger>(p => new LoggerFactory().CreateLogger("Log"));
		return services;
	}

	private void SetupTestEnv()
	{
		Environment.SetEnvironmentVariable("SqlConnectionString", "Server=localhost;Database=localDatabase;Port=5432;User Id=postgres;Password=localPassword;Ssl Mode=Allow;");
		Environment.SetEnvironmentVariable("API_URL", "stub-api", EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("API_BLOCKCHAIN_SERVICE", "stub-service", EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("API_KEY", "stub-key", EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", "***REMOVED***", EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("PLAYFAB_TITLE", "***REMOVED***", EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("REMOTE_CONFIGURATION", "false", EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("APPLICATION_ENVIRONMENT", "dev", EnvironmentVariableTarget.Process);
	}
}