
using Backend.Game;
using Backend.Game.Services;
using FirstLight;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using Quantum;
using FirstLight.Server.SDK.Services;
using FirstLight.Services;
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
		var logic = new GameServerLogic(cfg, new ServerPlayerDataProvider(state), new InMemoryMessageBrokerService());
		logic.Init();
	}
	
	[Test]
	public void TestServerExecutingCommand()
	{
		var playerId = _server.GetTestPlayerID();
		var oldState = _stateService.GetPlayerState(playerId).Result;
		var oldPlayerData = oldState.DeserializeModel<CollectionData>();
		var newSkin = GameId.FemalePunk;
		var cmd = new EquipCollectionItemCommand() { Item = new CollectionItem(newSkin) };
		var newState = _cmdHandler.ExecuteCommand(playerId, cmd, oldState).Result;
		var newPlayerData = newState.DeserializeModel<CollectionData>();
		
		Assert.AreEqual(newSkin, newPlayerData.Equipped[new (GameIdGroup.PlayerSkin)].Id);
	}

}