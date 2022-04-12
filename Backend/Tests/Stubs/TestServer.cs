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
	private static string? _testPlayerId = null;
	
	public TestServer()
	{
		SetupServices();
		var cfg = GetService<IConfigsProvider>();
		var data = GetService<IDataProvider>();
		_logic = new GameServerLogic(cfg, data);
		_logic.Init();
	}

	private void SetupServices()
	{
		var services = new ServiceCollection();
		var logger = new LoggerFactory().CreateLogger("Log");
		IOCSetup.Setup(services, logger);
		services.AddSingleton<IDataProvider, ServerTestData>();
		_services = services.BuildServiceProvider();
	}
	
	public T? GetService<T>()
	{
		return (T?)_services.GetService(typeof(T));
	}
	
	public string GetTestPlayerID()
	{
		if (_testPlayerId == null)
		{
			var server = _services.GetService<IPlayfabServer>();
			_testPlayerId = TestPlayerSetup.GetTestPlayerId(server as PlayfabServerSettings);
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

}