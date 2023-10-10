
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Game;
using Backend.Game.Services;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using NUnit.Framework;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Services;
using FirstLight.Services;

using Quantum;
using Assert = NUnit.Framework.Assert;

public class TestDataService
{
	private TestServer _server = null!;
	private IServerStateService? _service;

	[SetUp]
	public void Setup()
	{
		_server = new TestServer();
		_server.SetupInMemoryServer();
		_service = _server.GetService<IServerStateService>();
		_server.GiveDefaultSkins();

	}

	/// <summary>
	/// This test runs against a real playfab test account.
	/// Ensures we can save/read data from playfab using our DataService.
	/// </summary>
	[Test]
	public void TestSaveLoadPlayerData()
	{
		var playerId = _server.GetTestPlayerID();
		var data = new ServerState()
		{
			{"test_key", "test_value"}
		};
		_service?.UpdatePlayerState(playerId, data).Wait();
		
		var readData = _service.GetPlayerState(playerId).Result;
		Assert.AreEqual(data["test_key"], readData["test_key"]);
	}

	[Test]
	public void TestGettingOnlyUpdatedKeys()
	{
		var playerId = _server.GetTestPlayerID();
		var readData = _service.GetPlayerState(playerId).Result;

		readData.UpdateModel(new PlayerData() { Level = 5 });
		var onlyUpdated = readData.GetOnlyUpdatedState();

		Assert.IsTrue(readData.HasDelta());
		Assert.AreEqual(1, readData.GetDeltas().GetModifiedTypes().Count());
		Assert.AreEqual(1, onlyUpdated.Count);
		Assert.IsTrue(onlyUpdated.ContainsKey(typeof(PlayerData).FullName));
	}
	
	[Test]
	public async Task TestCommandDelta()
	{
		var initialState = await _server.ServerState.GetPlayerState(_server.GetTestPlayerID());
		var modelBefore = initialState.DeserializeModel<CollectionData>();
		
		var cmd = new EquipCollectionItemCommand() { Item = ItemFactory.Collection(GameId.FemaleAssassin) };
		
		var result = _server.SendTestCommand(cmd).Data;

		var finalState = await _server.ServerState.GetPlayerState(_server.GetTestPlayerID());
		var modelAfter = finalState.DeserializeModel<CollectionData>();
		var resultDelta = ModelSerializer.DeserializeFromData<StateDelta>(result);

		Assert.AreNotEqual(modelAfter.GetHashCode(), modelBefore.GetHashCode());
		Assert.True(resultDelta.ModifiedTypes.ContainsKey(typeof(CollectionData)));
		Assert.IsTrue(resultDelta.ModifiedTypes.Values.Contains(modelAfter.GetHashCode()));
	}

	[Test]
	public async Task TestOnlyTriggerDeltaIfModified()
	{
		var initialState = await _server.ServerState.GetPlayerState(_server.GetTestPlayerID());
		var dataProvider = new ServerPlayerDataProvider(initialState);
		var playerData = dataProvider.GetData<PlayerData>();

		Assert.IsTrue(dataProvider.ModelsConsumed.Contains(playerData.GetType()));
		var newState = dataProvider.GetUpdatedState();
		Assert.IsFalse(newState.HasDelta());
		Assert.AreEqual(0, newState.GetOnlyUpdatedState().Keys.Count);
	}
	
	[Test]
	public async Task TestValidDeltas()
	{
		var initialState = await _server.ServerState.GetPlayerState(_server.GetTestPlayerID());
		var modelBefore = initialState.DeserializeModel<PlayerData>();
		
		var cmd = new EquipCollectionItemCommand() { Item = ItemFactory.Collection(GameId.FemaleAssassin) };
		
		var result = _server.SendTestCommand(cmd).Data;

		var finalState = await _server.ServerState.GetPlayerState(_server.GetTestPlayerID());
		var resultDelta = ModelSerializer.DeserializeFromData<StateDelta>(result);

		var dataProvider = new ServerPlayerDataProvider(finalState);
		var invalidModels = ServerCommandQueue.GetDesynchedDeltas(ServerCommandQueue.GetClientDelta(dataProvider), result);

		Assert.AreEqual(0, invalidModels.Count);
		Assert.True(resultDelta.ModifiedTypes.ContainsKey(typeof(CollectionData)));
	}
	
	[Test]
	public async Task TestInvalidDeltas()
	{
		var initialState = await _server.ServerState.GetPlayerState(_server.GetTestPlayerID());

		var cmd = new EquipCollectionItemCommand() { Item = ItemFactory.Collection(GameId.FemaleAssassin) };
		
		var result = _server.SendTestCommand(cmd).Data;
		var resultDelta = ModelSerializer.DeserializeFromData<StateDelta>(result);
		var dataProvider = new ServerPlayerDataProvider(initialState);
		
		var invalidModels = ServerCommandQueue.GetDesynchedDeltas(ServerCommandQueue.GetClientDelta(dataProvider), result);

		Assert.AreEqual(1, invalidModels.Count);
		Assert.True(resultDelta.ModifiedTypes.ContainsKey(typeof(CollectionData)));
	}
}