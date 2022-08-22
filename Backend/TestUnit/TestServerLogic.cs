
using Backend.Game;
using Backend.Game.Services;
using FirstLight;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using Quantum;
using ServerSDK.Services;
using Assert = NUnit.Framework.Assert;

public class TestServerLogic
{
	private TestServer _server = null!;
	private IServerStateService? _stateService;
	private IServerCommahdHandler? _cmdHandler;
	
	[SetUp]
	public void Setup()
	{
		_server = new TestServer();
		// Running player states in-memory
		_server.SetupInMemoryServer();
		_stateService = _server.GetService<IServerStateService>();
		_cmdHandler = _server.GetService<IServerCommahdHandler>();
	}

	[Test]
	public void TestLoginInitializationWithInitialState()
	{
		var cfg = _server.GetService<IConfigsProvider>();
		var state = _server.GetService<IServerStateService>().GetPlayerState(_server.GetTestPlayerID()).Result;
		var logic = new GameServerLogic(cfg, new ServerPlayerDataProvider(state));
		logic.Init();
	}
	
	[Test]
	public void TestServerExecutingCommand()
	{
		var playerId = _server.GetTestPlayerID();
		var oldState = _stateService.GetPlayerState(playerId).Result;
		var oldPlayerData = oldState.DeserializeModel<PlayerData>();
		var currentSkin = oldPlayerData.PlayerSkinId;
		var newSkin = GameId.Female01Avatar;
		if (currentSkin == newSkin)
		{
			newSkin = GameId.Female02Avatar;
		}
		
		var command = new UpdatePlayerSkinCommand()
		{
			SkinId = newSkin
		};
		var newState = _cmdHandler.ExecuteCommand(command, oldState);
		var newPlayerData = newState.DeserializeModel<PlayerData>();
		
		Assert.AreEqual(newSkin, newPlayerData.PlayerSkinId);
	}

}