
using System.IO;
using Backend;
using Backend.Game;
using FirstLight;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using NUnit.Framework;
using Tests.Stubs;

namespace Tests;

public class TestConfigsProvider
{
	private TestServer _server = null!;
	private string _playerId = null!;
	private PlayerData _playerData;

	[SetUp]
	public void Setup()
	{
		_server = new TestServer();
		_playerId = _server.GetTestPlayerID();
	}
	
	/// <summary>
	/// TestUnit the current data in server is serializable as configuration.
	/// </summary>
	[Test]
	public void TestCurrentDevData()
	{
		var testAppPath = Path.GetDirectoryName(typeof(ServerConfiguration).Assembly.Location);
		var cfgSerializer = new ConfigsSerializer();
		var bakedConfigs = File.ReadAllText(Path.Combine(testAppPath, "gameConfig.json"));
		var cfg = cfgSerializer.Deserialize<ServerConfigsProvider>(bakedConfigs);
		
		Assert.NotNull(cfg.GetConfigsDictionary<PlayerLevelConfig>());
	}

}