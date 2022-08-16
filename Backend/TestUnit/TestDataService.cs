
using Backend.Game.Services;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using NUnit.Framework;
using ServerSDK.Models;
using ServerSDK.Services;
using Tests.Stubs;

namespace Tests;

public class TestDataService
{
	private TestServer _server = null!;
	private IServerStateService? _service;

	[SetUp]
	public void Setup()
	{
		_server = new TestServer();
		_service = _server.GetService<IServerStateService>();
	}

	/// <summary>
	/// This test runs against a real playfab test account.
	/// Ensures we can save/read data from playfab using our DataService.
	/// </summary>
	/*[Test]
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
	}*/

	[Test]
	public void TestGettingOnlyUpdatedKeys()
	{
		var playerId = _server.GetTestPlayerID();
		var readData = _service.GetPlayerState(playerId).Result;

		readData.UpdateModel(new PlayerData());
		var onlyUpdated = readData.GetOnlyUpdatedState();

		Assert.AreEqual(1, readData.UpdatedTypes.Count);
		Assert.AreEqual(1, onlyUpdated.Count);
		Assert.IsTrue(onlyUpdated.ContainsKey(typeof(PlayerData).FullName));
	}
}