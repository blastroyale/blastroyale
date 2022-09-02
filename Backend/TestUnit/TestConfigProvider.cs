
using System.IO;
using Backend;
using Backend.Game;
using FirstLight;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using NUnit.Framework;

public class TestConfigsProvider
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
	}
	
	/// <summary>
	/// TestUnit the current data in server is serializable as configuration.
	/// </summary>
	[Test]
	public void TestCurrentDevData()
	{
		var testAppPath = Path.GetDirectoryName(typeof(GameLogicWebWebService).Assembly.Location);
		var cfgSerializer = new ConfigsSerializer();
		var bakedConfigs = File.ReadAllText(Path.Combine(testAppPath, "gameConfig.json"));
		var cfg = cfgSerializer.Deserialize<ServerConfigsProvider>(bakedConfigs);

		foreach (var cfgType in cfg.GetAllConfigs().Keys)
		{
			Assert.NotNull(cfg.GetConfigByType(cfgType));
		}
	}
}