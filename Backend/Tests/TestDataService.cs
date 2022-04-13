
using Backend.Game.Services;
using FirstLight.Game.Logic;
using NUnit.Framework;
using Tests.Stubs;

namespace Tests;

public class TestDataService
{
	private TestServer _server = null!;
	private IServerDataService? _service;

	[SetUp]
	public void Setup()
	{
		_server = new TestServer();
		_service = _server.GetService<IServerDataService>();
	}

	/// <summary>
	/// This test runs against a real playfab test account.
	/// Ensures we can save/read data from playfab using our DataService.
	/// </summary>
	[Test]
	public void TestSaveLoadPlayerData()
	{
		var playerId = _server.GetTestPlayerID();
		var data = new ServerData()
		{
			{"test_key", "test_value"}
		};
		_service?.UpdatePlayerData(playerId, data);
		
		var readData = _service.GetPlayerData(playerId);
		Assert.AreEqual(data["test_key"], readData["test_key"]);
	}

}