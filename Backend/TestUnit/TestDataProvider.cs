
using Backend.Game;
using Backend.Game.Services;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using NUnit.Framework;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Services;

public class TestDataProvider
{
	private TestServer _server = null!;
	private string _playerId = null!;
	private PlayerData _playerData;

	[SetUp]
	public void Setup()
	{
		_server = new TestServer();
		_server.SetupInMemoryServer();
		_playerId = _server.GetTestPlayerID();
		
		// Create player data and add to playfab
		_playerData = new PlayerData()
		{
			Level = 666,
			Trophies = 69
		};
		var serializedModel = ModelSerializer.Serialize(_playerData);
		var serverData = new ServerState();
		serverData.Add(serializedModel.Key, serializedModel.Value);
		var service = _server?.GetService<IServerStateService>();
		service?.UpdatePlayerState(_playerId, serverData).Wait();
	}
	
	/// <summary>
	/// Makes sure we are able to use the data provider from any read data.
	/// We will ServerPlayerDataProvider before game logic executes with necessary player data.
	/// </summary>
	[Test]
	public void TestPlayerDataProvider()
	{
		var readData = _server?.GetService<IServerStateService>()?.GetPlayerState(_playerId).Result;

		var dataProvider = new ServerPlayerDataProvider(readData);
		var readPlayerData = dataProvider.GetData<PlayerData>();
		
		Assert.AreEqual(readPlayerData.Level, _playerData.Level);
	}

}