
using Backend.Game;
using Backend.Game.Services;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using NUnit.Framework;
using Tests.Stubs;

namespace Tests;

public class TestDataProvider
{
	private TestServer _server = null!;
	private string _playerId = null!;
	private PlayerData _playerData;

	[SetUp]
	public void Setup()
	{
		_server = new TestServer();
		_playerId = _server.GetTestPlayerID();
		
		// Create player data and add to playfab
		_playerData = new PlayerData()
		{
			Level = 666,
			Trophies = 69
		};
		var serializedModel = ModelSerializer.Serialize(_playerData);
		var serverData = new ServerData();
		serverData.Add(serializedModel.Key, serializedModel.Value);
		var service = _server?.GetService<IServerDataService>();
		service?.UpdatePlayerData(_playerId, serverData);
	}
	
	
	/// <summary>
	/// Makes sure we are able to use the data provider from any read data.
	/// We will ServerPlayerDataProvider before game logic executes with necessary player data.
	/// </summary>
	[Test]
	public void TestPlayerDataProvider()
	{
		var readData = _server?.GetService<IServerDataService>()?.GetPlayerData(_playerId);

		var dataProvider = new ServerPlayerDataProvider(readData);
		var readPlayerData = dataProvider.GetData<PlayerData>();
		
		Assert.AreEqual(readPlayerData.Level, _playerData.Level);
	}

}