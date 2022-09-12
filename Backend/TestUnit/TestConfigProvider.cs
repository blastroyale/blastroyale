
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Backend;
using Backend.Game;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Server.SDK.Modules.GameConfiguration;
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

	[Test]
	public void TestUpdatingConfigs()
	{
		var config = new ConfigsProvider();
		var oldValue = new Dictionary<Type, IEnumerable>()
		{
			{
				typeof(PlayerLevelConfig), new Dictionary<int, PlayerLevelConfig>()
				{
					{0, new PlayerLevelConfig()
					{
						Level = 1,
						LevelUpXP = 10
					}}
				}
			}
		};

		var newValue = new Dictionary<Type, IEnumerable>()
		{
			{
				typeof(PlayerLevelConfig), new Dictionary<int, PlayerLevelConfig>()
				{
					{0, new PlayerLevelConfig()
					{
						Level = 1,
						LevelUpXP = 50
					}}
				}
			}
		};
		
		config.AddAllConfigs(oldValue);

		var cfg = config.GetConfigsDictionary<PlayerLevelConfig>().Values.First();
		Assert.AreEqual(0, config.Version);
		Assert.AreEqual(10, cfg.LevelUpXP);

		config.UpdateTo(1, newValue);
		
		cfg = config.GetConfigsDictionary<PlayerLevelConfig>().Values.First();
		Assert.AreEqual(1, config.Version);
		Assert.AreEqual(50, cfg.LevelUpXP);
	}
}