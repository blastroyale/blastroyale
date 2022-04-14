using System;
using Backend.Game;
using Backend.Game.Services;
using FirstLight;
using FirstLight.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
		_services = SetupServices().BuildServiceProvider();
		var cfg = GetService<IConfigsProvider>();
		var data = GetService<IDataProvider>();
		_logic = new GameServerLogic(cfg, data);
		_logic.Init();
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
		IOCSetup.Setup(services, logger);
		services.AddSingleton<IDataProvider, ServerTestData>();
		services.AddSingleton<ITestPlayerSetup, TestPlayerSetup>();
		return services;

	}

	public T? GetService<T>()
	{
		return (T?)_services.GetService(typeof(T));
	}
	
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