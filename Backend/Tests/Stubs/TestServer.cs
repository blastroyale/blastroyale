using System;
using FirstLight;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Services;
using JetBrains.Annotations;

namespace Tests.Stubs;

/// <summary>
/// Represents what is needed to run a isolated server testing environment.
/// </summary>
public class TestServer
{
	private IDataProvider _data;
	private GameLogic _logic;
	private GameCommandService _commandService;
	
	[NotNull]
	public IDataProvider Data
	{
		get => _data;
		set => _data = value ?? throw new ArgumentNullException(nameof(value));
	}

	[NotNull]
	public GameLogic Logic
	{
		get => _logic;
		set => _logic = value ?? throw new ArgumentNullException(nameof(value));
	}

	[NotNull]
	public GameCommandService CommandService
	{
		get => _commandService;
		set => _commandService = value ?? throw new ArgumentNullException(nameof(value));
	}
	
	public TestServer()
	{
		var cfg = new ConfigsProvider();
		cfg.AddSingletonConfig(new MapConfig());
		_data = new ServerTestData();
		_commandService = new GameCommandService(_logic, _data);
		_logic = new ServerTestLogic(cfg, _data);
		_logic.Init();
	}

}