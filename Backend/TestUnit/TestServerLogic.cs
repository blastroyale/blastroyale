using Backend.Game;
using Backend.Game.Services;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using NUnit.Framework;
using Quantum;
using FirstLight.Server.SDK.Services;
using GameLogicService.Services;
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
		_server.GiveDefaultSkins();
		_stateService = _server.GetService<IServerStateService>();
		_cmdHandler = _server.GetService<IServerCommahdHandler>();
	}

	[Test]
	public void TestLoginInitializationWithInitialState()
	{
		var cfg = _server.GetService<IConfigsProvider>();
		var state = _server.GetService<IServerStateService>().GetPlayerState(_server.GetTestPlayerID()).Result;
		var logic = new GameServerLogic(cfg, new UnityServerRemoteConfigProvider(null), new ServerPlayerDataProvider(state), new InMemoryMessageBrokerService());
		logic.Init();
	}

	[Test]
	public void TestServerExecutingCommand()
	{
		var playerId = _server.GetTestPlayerID();
		var oldState = _stateService.GetPlayerState(playerId).Result;
		var oldPlayerData = oldState.DeserializeModel<CollectionData>();
		var newSkin = GameId.FemaleAssassin;
		var cmd = new EquipCollectionItemCommand() { Item = ItemFactory.Collection(newSkin) };
		var remoteConfig = new UnityServerRemoteConfigProvider(null);
		var newState = _cmdHandler.ExecuteCommand(playerId, cmd, oldState, remoteConfig).Result;
		var newPlayerData = newState.DeserializeModel<CollectionData>();

		Assert.AreEqual(newSkin, newPlayerData.Equipped[new(GameIdGroup.PlayerSkin)].Id);
	}
}