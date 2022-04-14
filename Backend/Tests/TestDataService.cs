
using Backend.Game.Services;
using Backend.Models;
using FirstLight.Game.Logic;
using NUnit.Framework;
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
	[Test]
	public void TestSaveLoadPlayerData()
	{
		var playerId = _server.GetTestPlayerID();
		var data = new ServerState()
		{
			{"test_key", "test_value"}
		};
		_service?.UpdatePlayerState(playerId, data);
		
		var readData = _service.GetPlayerState(playerId);
		Assert.AreEqual(data["test_key"], readData["test_key"]);
	}

}